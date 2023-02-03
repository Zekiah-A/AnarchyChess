using System.Buffers.Binary;
using System.Text;
using AnarchyChess.Server.Events;
using AnarchyChess.Server.Packets;
using AnarchyChess.Server.Virtual;
using Microsoft.Extensions.Logging;
using WatsonWebsocket;
namespace AnarchyChess.Server;

public sealed class AnarchyChessServerInstance : ServerInstance
{
    public Dictionary<ClientMetadata, DateTime> IdlePieces = new();
    public Timer IdleDeletionTick;

    public AnarchyChessServerInstance(WatsonWsServer server, Action<string>? logger = null, Map? map = null) : base(server, logger, map)
    {
        map ??= new Map();
        VirtualMap = map.Value;
        
        for (var x = 0; x < VirtualMap.Columns; x++)
        {
            for (var y = 0; y < VirtualMap.Rows; y++)
            {
                VirtualMap.Boards[x, y].PieceKilledEvent += OnPieceKilled;
                VirtualMap.Boards[x, y].TurnChangedEvent += OnTurnChanged;
            }
        }

        IdleDeletionTick = new Timer(DeleteIdlePieces, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }
    
    private protected override void OnClientConnected(object? sender, ClientConnectedEventArgs args)
    {
        // (byte) Packet code = data[0], (byte) boards columns = data[1], (byte) boards rows = data[2],
        // (byte) pieces columns = data[3], (byte) pieces rows = data[4], (byte..[]) pieces data[5..]
        var canvasesBuffer = new byte[Clients.Count * 6 + 5];
        canvasesBuffer[0] = (byte) ServerPackets.Canvases;
        canvasesBuffer[1] = (byte) VirtualMap.Boards.GetLength(0);
        canvasesBuffer[2] = (byte) VirtualMap.Boards.GetLength(1);
        canvasesBuffer[3] = (byte) VirtualMap.Boards[0, 0].Pieces.GetLength(0);
        canvasesBuffer[4] = (byte) VirtualMap.Boards[0, 0].Pieces.GetLength(1);

        // (byte) Packet code = data[0], (int) piece count white = data[1], (int) piece count black = data [5]
        var colourBalanceBuffer = (Span<byte>) stackalloc byte[9];
        colourBalanceBuffer[0] = (byte) ServerPackets.ColourBalance;

        var piecesWhite = 0;
        var piecesBlack = 0;
        var i = 5;
        foreach (var clientPiece in Clients.Values.Select(clientToken => VirtualMap.GetPieceInstance(clientToken)))
        {
            SerialisePiecePacket(clientPiece).CopyTo(canvasesBuffer, i);

            if (clientPiece.Colour == PieceColour.White)
            {
                piecesWhite++;
            }
            else
            {
                piecesBlack++;
            }

            i += 6;
        }

        BinaryPrimitives.WriteUInt32BigEndian(colourBalanceBuffer[1..], (uint) piecesWhite);
        BinaryPrimitives.WriteUInt32BigEndian(colourBalanceBuffer[5..], (uint) piecesBlack);

        App.SendAsync(args.Client, canvasesBuffer);
        App.SendAsync(args.Client, colourBalanceBuffer.ToArray());
    }

    private protected override void OnMessageReceived(object? sender, MessageReceivedEventArgs args)
    {
        var data = new Span<byte>(args.Data.ToArray());
        var code = data[0];

        switch ((ClientPackets) code)
        {
            case ClientPackets.Spawn:
            {
                // Packet is boardColumn = data[1], boardRow = data[2], column = data[3],
                // row = data[4], pieceType = data[5], pieceColour = data[6], or token = data[1..]
                if (data.Length != 7 && data.Length != 37)
                {
                    Logger?.Invoke($"Rejected spawn from client {args.Client.IpPort} due to invalid packet length.");
                    return;
                }

                // We try to reconnect a client to their piece if they reconnect with the same token. 
                if (data.Length == 37)
                {
                    var authenticatingToken = Encoding.UTF8.GetString(data[8..]);

                    if (Clients.TryGetKey(authenticatingToken, out var existingMetadata))
                    {
                        Logger?.Invoke(
                            $"Spawn rejected from client {args.Client.IpPort} due to invalid authentication token.");
                        App.SendAsync(args.Client, new[] {(byte) ServerPackets.RejectToken});
                        return;
                    }

                    // Re-hook up this client to their rightful auth token
                    Clients.Remove(existingMetadata);
                    Clients.Add(args.Client, authenticatingToken);

                    // Skip all the ceremony, and act like they were connected all along
                    break;
                }

                // Remove if they already have a piece on the board
                if (Clients.TryGetValue(args.Client, out var clientToken))
                {
                    RemovePiece(clientToken);
                }

                // Add piece to board
                var token = Guid.NewGuid().ToString();
                var piece = new Piece(token, (PieceType) data[5], (PieceColour) data[6]);

                var location = new BoardLocation(data[1], data[2], data[3], data[4]);

                if (!VirtualMap.TrySpawnPiece(piece, location))
                {
                    Logger?.Invoke($"Spawn rejected from client {args.Client.IpPort} due to invalid spawn location.");
                    App.SendAsync(args.Client, new[] {(byte) ServerPackets.RejectSpawn});
                    return;
                }

                Clients.Add(args.Client, token);

                // Send the client back their token so that they can reconnect to the same piece
                var guidBuffer = Encoding.UTF8.GetBytes(token);
                var tokenBuffer = new byte[guidBuffer.Length + 1];
                tokenBuffer[0] = (byte) ServerPackets.Token;
                guidBuffer.CopyTo(tokenBuffer, 1);

                App.SendAsync(args.Client, tokenBuffer);

                // Send to all connected clients
                var sendBuffer = new byte[8];
                sendBuffer[0] = (byte) ServerPackets.Spawn;
                SerialisePiecePacket(piece).CopyTo(sendBuffer, 1);

                sendBuffer[7] = (byte) ServerPackets.Me;
                App.SendAsync(args.Client, sendBuffer);
                sendBuffer[7] = 0;

                foreach (var client in App.Clients.Where(client => client != args.Client))
                {
                    App.SendAsync(client, sendBuffer);
                }

                break;
            }
            case ClientPackets.Move:
            {
                // Packet is boardColumn = data[1], boardRow = data[2], pieceColumn = data[3], pieceRow = data[4]
                if (data.Length != 5)
                {
                    Logger?.Invoke($"Move rejected from client {args.Client.IpPort} due to invalid packet length.");
                    return;
                }

                if (!Clients.TryGetValue(args.Client, out var clientToken))
                {
                    Logger?.Invoke($"Move rejected from client {args.Client.IpPort} due to no registered client token.");
                    return;
                }

                // Record previous move packet, board row and board column, before manipulating piece position.
                var previousPosition = SerialisePositionPacket(clientToken);
                var newLocation = new BoardLocation(data[1], data[2], data[3], data[4]);

                // Attempt move piece, reject if move is invalid 
                if (!VirtualMap.TryMovePiece(clientToken, newLocation))
                {
                    Logger?.Invoke($"Move rejected from client {args.Client.IpPort} due to invalid piece move location.");
                    App.SendAsync(args.Client, new[] {(byte) ServerPackets.RejectMove});
                    return;
                }

                // Send to all connected clients
                var moveBuffer = new byte[10];
                moveBuffer[0] = (byte) ServerPackets.Move;
                SerialiseMovePacket(clientToken, previousPosition).CopyTo(moveBuffer, 1);

                foreach (var client in App.Clients)
                {
                    if (Clients.TryGetValue(client, out var pieceToken) && pieceToken.Equals(clientToken))
                    {
                        moveBuffer[9] = (byte) ServerPackets.Me;
                        App.SendAsync(client, moveBuffer);
                        moveBuffer[9] = 0;
                        continue;
                    }

                    App.SendAsync(client, moveBuffer);
                }
                break;
            }
            case ClientPackets.Chat:
            {
                // Packet is UTF-8 encoded message text = data [1..]
                if (data.Length > 250)
                {
                    Logger?.Invoke($"Chat rejected from client {args.Client.IpPort} due to too long message length.");
                    App.SendAsync(args.Client, new[] {(byte) ServerPackets.RejectChat});
                    return;
                }

                // Rebrand chat as a server packet and send to all players
                data[0] = (byte) ServerPackets.Chat;

                foreach (var client in App.Clients)
                {
                    App.SendAsync(client, data.ToArray());
                }

                break;
            }
        }
    }
    
    private protected override void OnClientDisconnected(object? sender, ClientDisconnectedEventArgs args)
    {
        // We queue these clients to be fully removed from the game after 5 minutes of disconnection
        // If they reconnect before they will still be able to reauthenticate into their piece via their token.
        IdlePieces.Add(args.Client, DateTime.Now.AddMinutes(5));
    }
    
    private void DeleteIdlePieces(object? state)
    {
        foreach (var pair in IdlePieces.Where(pair => pair.Value >= DateTime.Now))
        {
            if (Clients.TryGetValue(pair.Key, out var clientToken))
            {
                RemovePiece(clientToken);
            }
            
            Clients.Remove(pair.Key);
            IdlePieces.Remove(pair.Key);
        }
    }

    private void RemovePiece(string token)
    {
        // Delete piece from that board
        VirtualMap.DeletePiece(token);

        // We send a position packet to say where the killed player was
        var killBuffer = new byte[6];
        killBuffer[0] = (byte) ServerPackets.PieceKilled;
        SerialisePositionPacket(token).CopyTo(killBuffer, 1);
        
        foreach (var client in App.Clients)
        {
            if (Clients.TryGetValue(client, out var pieceToken) && pieceToken.Equals(token))
            {
                killBuffer[5] = (byte) ServerPackets.Me;
                App.SendAsync(client, killBuffer);
                killBuffer[5] = 0;
                continue;
            }

            App.SendAsync(client, killBuffer);
        }
    }
    
    private void OnPieceKilled(object? sender, PieceKilledEventArgs args)
    {
        RemovePiece(args.Killed.Token);
        Logger?.Invoke($"Piece {args.Killed.Token} was killed by {args.Killer.Token}.");
    }

    private void OnTurnChanged(object? sender, TurnChangedEventArgs args)
    {
        // Turn change packet is data[1..2] = (ushort) current turn, data[3..7] = position packet of currently playing piece.
        var turnBuffer = (Span<byte>) stackalloc byte[8];
        turnBuffer[0] = (byte) ServerPackets.TurnChanged;
        BinaryPrimitives.WriteUInt16BigEndian(turnBuffer[1..], (ushort) args.Turn);
        new Span<byte>(SerialisePositionPacket(args.Token)).CopyTo(turnBuffer[3..]);
        
        lock (App.Clients)
        {
            foreach (var client in App.Clients)
            {
                if (Clients.TryGetKey(args.Token, out var clientMetadata) && clientMetadata == client)
                {
                    turnBuffer[7] = (byte) ServerPackets.Me;
                    App.SendAsync(client, turnBuffer.ToArray());
                    turnBuffer[7] = 0;
                }
                else
                {
                    App.SendAsync(client, turnBuffer.ToArray());
                }
            }
        }
    }
}