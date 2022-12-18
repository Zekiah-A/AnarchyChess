using System.Buffers.Binary;
using System.Text;
using System.Text.Encodings.Web;
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
                        RemoveClient(clientPiece);
                    }
                    
                    // Packet is boardRow = data[1], boardColumn = data[2], row = data[3],
                    // column = data[4], pieceType = data[5], client spawn packet.
                    var board = VirtualMap.Boards[data[1], data[2]];
                    if (board is null)
                    {
                        app.SendAsync(args.Client, new [] { (byte) ServerPackets.Reject });
                        return;
                    }

                    var token = Guid.NewGuid().ToString();

                    // Add piece to board
                    var piece = new Piece(token, (PieceType) data[5], board);
                    board.Pieces[data[3], data[4]] = piece;
                    Clients.Add(args.Client, piece);
                    
                    // Send the client back their token so that they can reconnect to the same piece
                    var guidBuffer = Encoding.UTF8.GetBytes(token);
                    var tokenBuffer = new byte[guidBuffer.Length + 1];
                    tokenBuffer[0] = (byte) ServerPackets.Token;
                    guidBuffer.CopyTo(tokenBuffer, 1);

                    app.SendAsync(args.Client, tokenBuffer);
                    
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
                {
                    if (!Clients.TryGetValue(args.Client, out var clientPiece))
                    {
                        return;
                    }
                    
                    // Check if move is valid - does not guarentee that it is completely allowed on board though
                    // For example, a diagonal pawn move by 1 space is valid, but only if they are moving onto a space
                    // That is occupied by another piece. On the board, white moves "up", while black moves "down"
                    var boardRow = data[1];
                    var boardColumn = data[2];
                    var row = data[3];
                    var column = data[4];

                    var moveColumns = data[1] - 1;
                    var moveRows = data[2] - 1;

                    var valid = false;
                    switch (clientPiece.Type)
                    {
                        case PieceType.Bishop:
                            if (moveColumns is < 8 and > -8 && moveRows is < 8 and > -8 && moveColumns == moveRows)
                            {
                                valid = true;
                            }
                            break;
                        case PieceType.King:
                            if (moveRows is 1 or -1 && moveColumns is 0) valid = true;
                            else if (moveRows is 0 && moveColumns is 1 or -1) valid = true;
                            else if (moveRows is 1 or -1 && moveColumns is 1 or -1 && moveColumns == moveRows)
                            {
                                valid = true;
                            }
                            break;
                        case PieceType.Knight:
                            if (moveRows is 0 && moveColumns is < 8 and > -8) valid = true;
                            else if (moveColumns is 0 && moveRows is < 8 and > -8) valid = true;
                            break;
                        case PieceType.Pawn:
                            if (moveRows is -1 or 0 or 1 && moveColumns is 1 && clientPiece.Colour == PieceColour.Black) valid = true;
                            else if (moveRows is -1 or 0 or 1 && moveColumns is -1 && clientPiece.Colour == PieceColour.White) valid = true;
                            break;
                        case PieceType.Queen:
                            if (moveRows == 0 && moveColumns is < 8 and > -8) valid = true;
                            else if (moveRows is 0 && moveColumns is < 8 and > -8) valid = true;
                            else if (moveRows is < 8 and > -8 && moveColumns is < 8 and > -8 && moveColumns == moveRows)
                            {
                                valid = true;
                            }
                            break;
                        case PieceType.Rook:
                            if (moveRows is 2 or -2 && moveColumns is 1 or -1) valid = true;
                            else  if (moveColumns is 2 or -2 && moveRows is 1 or -1) valid = true;
                            break;
                    }

                    if (!valid)
                    {
                        app.SendAsync(args.Client, new[] {(byte) ServerPackets.Reject});
                        return;
                    }
                    
                    // Check the effects of making move on board
                    
                    break;
                }
                case ClientPackets.Chat:
                    if (data.Length < 250)
                    {
                        return;
                    }
                    
                    
                    break;
            }
        };

        app.ClientDisconnected += (sender, args) =>
        {
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
        buffer[0] = (byte) ServerPackets.RemovePiece;
        SerialisePiecePacket(clientPiece).CopyTo(buffer, 1);

        foreach (var client in app.Clients)
        {
            app.SendAsync(client, buffer.ToArray());
        }
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