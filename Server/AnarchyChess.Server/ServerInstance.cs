using System.Buffers.Binary;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AnarchyChess.Server.Packets;
using AnarchyChess.Server.Virtual;
using UnbloatDB;
using WatsonWebsocket;
namespace AnarchyChess.Server;

public sealed class ServerInstance
{
    // ReSharper disable MemberCanBePrivate.Global
    public Map VirtualMap { get; }
    public GameData GameData { get; }
    public Dictionary<ClientMetadata, Piece> Clients { get; set; } = new();
    
    private WatsonWsServer app;

    public ServerInstance(GameData data, Map map, string certPath, string keyPath, string origin, bool ssl, int port)
    {
        VirtualMap = map;
        GameData = data;
        app = new WatsonWsServer(port, "localhost");
    }

    public async Task Start()
    {
        // Encode entire virtual map to JSON to get client up to date
        app.ClientConnected += async (sender, args) =>
        {
            using var stream = new MemoryStream(); 
            await JsonSerializer.SerializeAsync(stream, VirtualMap);
            await stream.FlushAsync();
            
            await app.SendAsync(args.Client, stream.ToArray());
        };

        app.MessageReceived += (sender, args) =>
        {
            if (args.Data.Array is null)
            {
                return;
            }

            var data = new Span<byte>(args.Data.Array);
            var code = data[0];

            switch ((ClientPackets) code)
            {
                case ClientPackets.Spawn:
                {
                    //TODO: Implement token re-authentication
                    if (Clients.TryGetValue(args.Client, out var clientPiece))
                    {
                        RemoveClient(clientPiece);
                    }
                    
                    // Packet is boardRow = data[1], boardColumn = data[2], row = data[3],
                    // column = data[4], pieceType = data[5], client spawn packet.
                    var board = VirtualMap.Boards[data[1], data[2]];
                    if (board is null)
                    {
                        app.SendAsync(args.Client, new [] { (byte) ServerPackets.RejectSpawn });
                        return;
                    }
                    
                    // Add piece to board
                    var token = Guid.NewGuid().ToString();
                    var piece = new Piece(token, (PieceType) data[5], (PieceColour) data[6], board);
                    if (!board.TrySpawnPiece(piece, data[3], data[4]))
                    {
                        app.SendAsync(args.Client, new [] { (byte) ServerPackets.RejectSpawn });
                    }
                    
                    Clients.Add(args.Client, piece);
                    
                    // Send the client back their token so that they can reconnect to the same piece
                    var guidBuffer = Encoding.UTF8.GetBytes(token);
                    var tokenBuffer = new byte[guidBuffer.Length + 1];
                    tokenBuffer[0] = (byte) ServerPackets.Token;
                    guidBuffer.CopyTo(tokenBuffer, 1);

                    app.SendAsync(args.Client, tokenBuffer);
                    
                    // Send to all connected clients
                    var sendBuffer = new byte[6];
                    sendBuffer[0] = (byte) ServerPackets.Spawn;
                    SerialisePiecePacket(piece).CopyTo(sendBuffer, 1);
                    
                    foreach (var client in app.Clients)
                    {
                        app.SendAsync(client, sendBuffer.ToArray());
                    }
                    break;
                }
                case ClientPackets.Move:
                {
                    if (!Clients.TryGetValue(args.Client, out var clientPiece))
                    {
                        return;
                    }
                    
                    // Record previous move packet, board row and board column, before manipulating piece position.
                    var previousMove = SerialisePiecePacket(clientPiece);
                    var boardRow = data[1];
                    var boardColumn = data[2];
                    
                    // Attempt move piece, reject if move is invalid 
                    if (!clientPiece.Board.TryMovePiece(clientPiece, boardRow, boardColumn))
                    {
                        app.SendAsync(args.Client, new[] {(byte) ServerPackets.RejectMove});
                        return;
                    }

                    // Send to all connected clients
                    var sendBuffer = new byte[9];
                    sendBuffer[0] = (byte) ServerPackets.Spawn;
                    SerialiseMovePacket(clientPiece, previousMove).CopyTo(sendBuffer, 1);
                    
                    foreach (var client in app.Clients)
                    {
                        app.SendAsync(client, sendBuffer.ToArray());
                    }
                    break;
                }
                case ClientPackets.Chat:
                {
                    if (data.Length > 250)
                    {
                        app.SendAsync(args.Client, new[] {(byte) ServerPackets.RejectChat});
                        return;
                    }

                    foreach (var client in app.Clients)
                    {
                        app.SendAsync(client, data.ToArray());
                    }
                    break;
                }
            }
        };

        app.ClientDisconnected += (sender, args) =>
        {
            // TODO: We do not necessarily have to kill players off on disconnect, for example, on server shutdown,
            // TODO: players should be able to reauthenticate into their piece via the token saved in localstorage.
            if (Clients.TryGetValue(args.Client, out var clientPiece))
            {
                RemoveClient(clientPiece);
            }
        };
    }

    private void RemoveClient(Piece clientPiece)
    {
        // Delete piece from that board
        var pieceCoordinates = clientPiece.Board.Pieces.CoordinatesOf(clientPiece);
        clientPiece.Board.Pieces[pieceCoordinates.Row, pieceCoordinates.Column] = null;
                        
        // We send the Map boards Row, Column Board Row, Column and finally the PieceType (not used)
        var buffer = new byte[5];
        buffer[0] = (byte) ServerPackets.Death;
        SerialisePiecePacket(clientPiece).CopyTo(buffer, 1);

        foreach (var client in app.Clients)
        {
            app.SendAsync(client, buffer.ToArray());
        }
    }
    
    private byte[] SerialisePiecePacket(Piece piece)
    {
        var buffer = new []
        {
            (byte) VirtualMap.Boards.CoordinatesOf(piece.Board).Row,
            (byte) VirtualMap.Boards.CoordinatesOf(piece.Board).Column,
            (byte) piece.Board.Pieces.CoordinatesOf(piece).Row,
            (byte) piece.Board.Pieces.CoordinatesOf(piece).Column,
            (byte) piece.Type,
            (byte) piece.Colour,
        };
        
        return buffer;
    }

    private byte[] SerialiseMovePacket(Piece piece, IReadOnlyList<byte> previousMove)
    {
        var buffer = new[]
        {
            previousMove[0],
            previousMove[1],
            previousMove[2],
            previousMove[3],
            (byte) VirtualMap.Boards.CoordinatesOf(piece.Board).Row,
            (byte) VirtualMap.Boards.CoordinatesOf(piece.Board).Column,
            (byte) piece.Board.Pieces.CoordinatesOf(piece).Row,
            (byte) piece.Board.Pieces.CoordinatesOf(piece).Column,
        };
        
        return buffer;
    }
}