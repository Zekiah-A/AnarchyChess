using System.Buffers.Binary;
using AnarchyChess.Server.Packets;
using AnarchyChess.Server.Virtual;
using UnbloatDB;
using WatsonWebsocket;
namespace AnarchyChess.Server;

public sealed class ServerInstance
{
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
        app.ClientConnected += (sender, args) =>
        {
            var buffer = new byte[0];
            buffer[0] = 1;

            app.SendAsync(args.Client, buffer);
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
                    if (Clients.TryGetValue(args.Client, out var clientPiece))
                    {
                        // Delete piece from that board
                        var pieceCoordinates = clientPiece.Board.Pieces.CoordinatesOf(clientPiece);
                        clientPiece.Board.Pieces[pieceCoordinates.Row, pieceCoordinates.Column] = null;
                        
                        // We send the Map boards Row, Column Board Row, Column and finally the PieceType (not used)
                        var buffer = new byte[5];
                        buffer[0] = (byte) ServerPackets.RemovePiece;
                        SerialisePiecePacket(clientPiece).CopyTo(buffer, 1);

                        foreach (var client in app.Clients)
                        {
                            app.SendAsync(client, buffer.ToArray());
                        }
                    }
                    
                    // Packet is boardRow = data[1], boardColumn = data[2], row = data[3],
                    // column = data[4], pieceType = data[5], client spawn packet.
                    var board = VirtualMap.Boards[data[1], data[2]];
                    if (board is null)
                    {
                        app.SendAsync(args.Client, new [] { (byte) ServerPackets.Reject });
                        return;
                    }

                    // Add piece to board
                    var piece = new Piece(Guid.NewGuid().ToString(), (PieceType) data[5], board);
                    board.Pieces[data[3], data[4]] = piece;
                    Clients.Add(args.Client, piece);
                    
                    // Send to all connected clients
                    var sendBuffer = new byte[6];
                    sendBuffer[0] = (byte) ServerPackets.AddPiece;
                    SerialisePiecePacket(piece).CopyTo(sendBuffer, 1);
                    
                    foreach (var client in app.Clients)
                    {
                        app.SendAsync(client, sendBuffer.ToArray());
                    }
                    break;
                }
                case ClientPackets.Move:
                    // Check if move is legal
                    // Move vector
                    var type = (PieceType) data[4];
                    var moveColumns = 0;
                    var moveRows = 0;

                    bool valid;
                    /*switch (type)
                    {
                        case PieceType.Bishop:
                            if (moveColumns == )
                            break;
                        case PieceType.King:
                            break;
                        case PieceType.Knight:
                            break;
                        case PieceType.Pawn:
                            break;
                        case PieceType.Queen:
                            break;
                        case PieceType.Rook:
                            break;
                    }*/
                        
                    break;
                case ClientPackets.Chat:
                    
                    break;
                /*
                case ClientPackets.RequestFriend:
                    break;
                case ClientPackets.AcceptFriend:
                    break;
                */
            }
        };

        app.ClientDisconnected += (sender, args) =>
        {
            
        };
    }
    
    private byte[] SerialisePiecePacket(Piece piece)
    {
        var buffer = new byte[5];
        buffer[0] = (byte) VirtualMap.Boards.CoordinatesOf(piece.Board).Row;
        buffer[1] = (byte) VirtualMap.Boards.CoordinatesOf(piece.Board).Column;
        buffer[2] = (byte) piece.Board.Pieces.CoordinatesOf(piece).Row;
        buffer[3] = (byte) piece.Board.Pieces.CoordinatesOf(piece).Column;
        buffer[4] = (byte) piece.Type;

        return buffer;
    }
}