using System.Buffers.Binary;
using System.Text;
using AnarchyChess.Server.Events;
using AnarchyChess.Server.Packets;
using AnarchyChess.Server.Virtual;
using Microsoft.Extensions.Logging;
using WatsonWebsocket;
namespace AnarchyChess.Server;

public sealed class ServerInstance
{
    public Map VirtualMap { get; }
    public Dictionary<ClientMetadata, string> Clients { get; set; } = new();
    public Dictionary<ClientMetadata, DateTime> IdlePieces = new();
    public Timer IdleDeletionTick; 
    public Action<string>? Logger;

    private WatsonWsServer app;

    public ServerInstance(int port, Map? map = null, bool ssl = false, string? certificatePath = null, string? keyPath = null)
    {
        map ??= new Map();
        VirtualMap = map;
        app = new WatsonWsServer(port, ssl, certificatePath,  keyPath, LogLevel.Trace, "localhost");
        
        foreach (var board in VirtualMap.Boards)
        {
            board.PieceKilledEvent += OnPieceKilled;
            board.TurnChangedEvent += OnTurnChanged;
        }

        IdleDeletionTick = new Timer(DeleteIdlePieces, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }

    public async Task StartAsync()
    {
        app.ClientConnected += (sender, args) =>
        {
            // (byte) Packet code = data[0], (byte) boards columns = data[1], (byte) boards rows = data[2],
            // (byte) pieces columns = data[3], (byte) pieces rows = data[4], (byte..[]) pieces data[5..]
            var buffer = new byte[Clients.Count * 5 + 5];
            buffer[0] = (byte) ServerPackets.Canvases;
            buffer[1] = (byte) VirtualMap.Boards.GetLength(0);
            buffer[2] = (byte) VirtualMap.Boards.GetLength(1);
            buffer[3] = (byte) VirtualMap.Boards[0, 0].Pieces.GetLength(0);
            buffer[4] = (byte) VirtualMap.Boards[0, 0].Pieces.GetLength(1);
            
            var i = 5;
            foreach (var clientPiece in Clients.Values.Select(pieceToken => GetPieceInstance(pieceToken)))
            {
                SerialisePiecePacket(clientPiece).CopyTo(buffer, i);
                i += 6;
            }

            app.SendAsync(args.Client, buffer);
        };

        app.MessageReceived += (sender, args) =>
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
                        var existingClient = Clients
                            .Where(clientPair => clientPair.Value.Equals(authenticatingToken))
                            .Select(clientPair => clientPair.Key).FirstOrDefault();

                        if (existingClient is null)
                        {
                            Logger?.Invoke($"Spawn rejected from client {args.Client} due to invalid authentication token.");
                            app.SendAsync(args.Client, new [] { (byte) ServerPackets.RejectToken });
                            return;
                        }

                        // Re-hook up this client to their rightful auth token
                        Clients.Remove(existingClient);
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
                    var piece = new Piece
                    (
                        token,
                        (PieceType) data[5],
                        (PieceColour) data[6]
                    );
                
                    var location = new BoardLocation(
                        data[1],
                        data[2],
                        data[3],
                        data[4]
                    );
                
                    if (!VirtualMap.TrySpawnPiece(piece, location))
                    {
                        Logger?.Invoke($"Spawn rejected from client {args.Client} due to invalid spawn location.");
                        app.SendAsync(args.Client, new [] { (byte) ServerPackets.RejectSpawn });
                        return;
                    }
                    
                    Clients.Add(args.Client, token);
                    
                    // Send the client back their token so that they can reconnect to the same piece
                    var guidBuffer = Encoding.UTF8.GetBytes(token);
                    var tokenBuffer = new byte[guidBuffer.Length + 1];
                    tokenBuffer[0] = (byte) ServerPackets.Token;
                    guidBuffer.CopyTo(tokenBuffer, 1);

                    app.SendAsync(args.Client, tokenBuffer);
                
                    // Send to all connected clients
                    var sendBuffer = new byte[7];
                    sendBuffer[0] = (byte) ServerPackets.Spawn;
                    SerialisePiecePacket(piece).CopyTo(sendBuffer, 1);
                
                    foreach (var client in app.Clients)
                    {
                        app.SendAsync(client, sendBuffer);
                    }
                    break;
                }
                case ClientPackets.Move:
                {
                    // Packet is boardColumn = data[1], boardRow = data[2], pieceColumn = data[3], pieceRow = data[4]
                    if (data.Length != 5)
                    {
                        return;
                    }
                    
                    if (!Clients.TryGetValue(args.Client, out var clientToken))
                    {
                        Logger?.Invoke($"Move rejected from client {args.Client} due to no registered client token.");
                        return;
                    }

                    // Record previous move packet, board row and board column, before manipulating piece position.
                    var previousMove = SerialisePositionPacket(clientToken);
                    var newLocation = new BoardLocation(data[1], data[2], data[3], data[4]);
                    
                    // Attempt move piece, reject if move is invalid 
                    if (!VirtualMap.TryMovePiece(clientToken, newLocation))
                    {
                        Logger?.Invoke($"Move rejected from client {args.Client} due to invalid piece move location.");
                        app.SendAsync(args.Client, new[] { (byte) ServerPackets.RejectMove });
                        return;
                    }

                    // Send to all connected clients
                    var sendBuffer = new byte[9];
                    sendBuffer[0] = (byte) ServerPackets.Spawn;
                    SerialiseMovePacket(clientToken, previousMove).CopyTo(sendBuffer, 1);
                    
                    foreach (var client in app.Clients)
                    {
                        app.SendAsync(client, sendBuffer.ToArray());
                    }
                    break;
                }
                case ClientPackets.Chat:
                {
                    // Packet is UTF-8 encoded message text = data [1..]
                    if (data.Length > 250)
                    {
                        Logger?.Invoke($"Chat rejected from client {args.Client} due to too long message length.");
                        app.SendAsync(args.Client, new[] { (byte) ServerPackets.RejectChat });
                        return;
                    }

                    // Rebrand chat as a server packet and send to all players
                    data[0] = (byte) ServerPackets.Chat;
                    
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
            // We queue these clients to be fully removed from the game after 5 minutes of disconnection
            // If they reconnect before they will still be able to reauthenticate into their piece via their token.
            IdlePieces.Add(args.Client, DateTime.Now.AddMinutes(5));
        };
        
        await app.StartAsync();
    }

    private void DeleteIdlePieces(object? state)
    {
        foreach (var pair in IdlePieces)
        {
            // We skip deleting clients whose times have not yet expired
            if (pair.Value < DateTime.Now)
            {
                continue;
            }
            
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

        foreach (var client in app.Clients)
        {
            app.SendAsync(client, killBuffer.ToArray());
        }
    }
    
    private void OnPieceKilled(object? sender, PieceKilledEventArgs args)
    {
        RemovePiece(args.Killed.Token);
        Logger?.Invoke($"Piece {args.Killed.Token} was killed by {args.Killer.Token}.");
    }

    private void OnTurnChanged(object? sender, TurnChangedEventArgs args)
    {
        // Turn change packet is data[1..4] = (int) current turn, data[5..9] = position packet of currently playing piece.
        var turnBuffer = new byte[9];
        turnBuffer[0] = (byte) ServerPackets.TurnChanged;
        BinaryPrimitives.WriteUInt32BigEndian(turnBuffer.AsSpan()[1..], (uint) args.Turn);
        SerialisePositionPacket(args.CurrentPiece.Token).CopyTo(turnBuffer, 5);
        
        foreach (var client in app.Clients)
        {
            app.SendAsync(client, turnBuffer.ToArray());   
        }
    }

    /// <summary>
    /// Length = 4
    /// </summary>
    private byte[] SerialisePositionPacket(string token)
    {
        var located = VirtualMap.LocatePieceInstance(token);
        
        return new[]
        {
            (byte) located.BoardColumn,
            (byte) located.BoardRow,
            (byte) located.PieceColumn,
            (byte) located.PieceRow
        };
    }
    
    /// <summary>
    /// Length = 6
    /// </summary>
    private byte[] SerialisePiecePacket(Piece piece)
    {
        var buffer = new byte[6];
        SerialisePositionPacket(piece.Token).CopyTo(buffer, 0);
        buffer[4] = (byte) piece.Type;
        buffer[5] = (byte) piece.Colour;
        
        return buffer;
    }

    /// <summary>
    /// Length = 9
    /// </summary>
    /// <param name="previousPosition">Byte array returned from "SerialisePositionPacket".</param>
    private byte[] SerialiseMovePacket(string token, byte[] previousPosition)
    {
        var located = VirtualMap.LocatePieceInstance(token);
        var buffer = new byte[9];
        previousPosition.CopyTo(buffer, 0);
        buffer[5] = (byte) located.BoardColumn;
        buffer[6] = (byte) located.BoardRow;
        buffer[7] = (byte) located.PieceColumn;
        buffer[8] = (byte) located.PieceRow;
        
        return buffer;
    }
    
    private ref Piece GetPieceInstance(string token)
    {
        var located = VirtualMap.LocatePieceInstance(token);
        
        return ref VirtualMap.Boards[located.BoardColumn, located.BoardRow]
            .Pieces[located.BoardColumn, located.BoardRow];
    }
}

// TODO: When saving backups, including client pieces is fine. But when saving board state, for example, with a server
// TODO: restart, we should not include the clients there, we only need to save the parameters used to create such board.
// TODO: Include a copy of clients in save to skip all of this.