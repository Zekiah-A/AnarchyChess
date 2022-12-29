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
    // ReSharper disable MemberCanBePrivate.Global
    public Map VirtualMap { get; }
    public Dictionary<ClientMetadata, string> Clients { get; set; } = new();
    public Action<string>? Logger;

    private WatsonWsServer app;

    public ServerInstance(int port, Map? map = null, bool ssl = false, string? certificatePath = null, string? keyPath = null)
    {
        map ??= new Map();
        VirtualMap = map;
        app = new WatsonWsServer(port, ssl, certificatePath,  keyPath, LogLevel.None, "localhost");
        
        foreach (var board in VirtualMap.Boards)
        {
            board.PieceKilledEvent += OnPieceKilled;
            board.TurnChangedEvent += OnTurnChanged;
        }
    }

    public async Task StartAsync()
    {
        // Encode entire virtual map to JSON to get client up to date
        app.ClientConnected += (sender, args) =>
        {
            // We can not serialise 2D arrays to JSON, this makes just sending the virtual board impossible, Therefore
            // we send the player the data for every single piece, followed by the params used to construct the boards
            // on the map, so that the client can reconstruct it by itself.
            
            // (byte) Packet code, (byte) boards columns, (byte) boards rows,
            // (byte) pieces columns, (byte) pieces rows, (byte..[]) pieces
            var buffer = new byte[Clients.Count * 5 + 5];
            buffer[0] = (byte) ServerPackets.Canvases;
            buffer[1] = (byte) VirtualMap.Boards.GetLength(0);
            buffer[2] = (byte) VirtualMap.Boards.GetLength(1);
            buffer[3] = (byte) VirtualMap.Boards[0, 0].Pieces.GetLength(0);
            buffer[4] = (byte) VirtualMap.Boards[0, 0].Pieces.GetLength(1);
            
            var i = 5;
            foreach (var pieceToken in Clients.Values)
            {
                var clientPiece = GetPieceInstance(pieceToken);
                SerialisePiecePacket(clientPiece).CopyTo(buffer, i);
                i += 4;
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
                    //TODO: Implement token re-authentication
                    if (Clients.TryGetValue(args.Client, out var clientToken))
                    {
                        var clientPiece = GetPieceInstance(clientToken);
                        RemoveClient(clientPiece);
                    }

                    if (data.Length != 7)
                    {
                        Logger?.Invoke($"Rejected spawn from client {args.Client.IpPort} due to invalid packet.");
                        return;
                    }
                    
                    // Packet is boardColumn = data[1], boardRow = data[2], column = data[3],
                    // row = data[4], pieceType = data[5], pieceColour = data[6]
                    ref var boardReference = ref VirtualMap.Boards[data[1], data[2]];

                    // Add piece to board
                    var token = Guid.NewGuid().ToString();
                    var piece = new Piece
                    (
                        token,
                        (PieceType) data[5],
                        (PieceColour) data[6],
                        data[1],
                        data[2],
                        data[3],
                        data[4]
                    )
                    {
                        BoardColumn = boardReference.Column,
                        BoardRow = boardReference.Row
                    };
                    
                    if (!boardReference.TrySpawnPiece(piece))
                    {
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
                    var sendBuffer = new byte[6];
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
                    if (!Clients.TryGetValue(args.Client, out var clientToken))
                    {
                        return;
                    }

                    var clientPiece = GetPieceInstance(clientToken);
                    
                    // Record previous move packet, board row and board column, before manipulating piece position.
                    var previousMove = SerialisePiecePacket(clientPiece);
                    var boardRow = data[1];
                    var boardColumn = data[2];
                    
                    // Attempt move piece, reject if move is invalid 
                    if (!VirtualMap.Boards[clientPiece.BoardColumn, clientPiece.BoardRow]
                            .TryMovePiece(clientPiece, boardRow, boardColumn))
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
            // TODO: We do not necessarily have to kill players off on disconnect, for example, on server shutdown,
            // TODO: players should be able to reauthenticate into their piece via the token saved in localstorage.
            if (Clients.TryGetValue(args.Client, out var clientToken))
            {
                var clientPiece = GetPieceInstance(clientToken);
                RemoveClient(clientPiece);
            }
        };
        
        await app.StartAsync();
        await Task.Delay(-1);
    }

    private void RemoveClient(Piece clientPiece)
    {
        // Delete piece from that board
        VirtualMap.DeletePiece(clientPiece);

        // We send the Map boards Row, Column Board Row, Column and finally the PieceType (not used)
        var killBuffer = new byte[6];
        killBuffer[0] = (byte) ServerPackets.PieceKilled;
        SerialisePositionPacket(clientPiece).CopyTo(killBuffer, 1);

        foreach (var client in app.Clients)
        {
            app.SendAsync(client, killBuffer.ToArray());
        }
    }
    
    private void OnPieceKilled(object? sender, PieceKilledEventArgs args)
    {
        var killBuffer = new byte[10];
        killBuffer[0] = (byte) ServerPackets.PieceKilled;
        SerialisePositionPacket(args.Killed).CopyTo(killBuffer, 1);
        SerialisePositionPacket(args.Killer).CopyTo(killBuffer, 5);
        
        foreach (var client in app.Clients)
        {
            app.SendAsync(client, killBuffer.ToArray());
        }
    }

    private void OnTurnChanged(object? sender, TurnChangedEventArgs args)
    {
        var turnBuffer = new byte[9];
        turnBuffer[0] = (byte) ServerPackets.TurnChanged;
        BinaryPrimitives.WriteUInt32BigEndian(turnBuffer.AsSpan()[1..], (uint) args.Turn);
        SerialisePositionPacket(args.CurrentPiece).CopyTo(turnBuffer, 5);
        
        foreach (var client in app.Clients)
        {
            app.SendAsync(client, turnBuffer.ToArray());   
        }
    }

    // TODO: Switch packets to use "column, row" (x, y) instead of "row, column" for consistency.

    /// <summary>
    /// Length = 4
    /// </summary>
    private byte[] SerialisePositionPacket(Piece piece)
    {
        return new[]
        {
            (byte) piece.BoardColumn,
            (byte) piece.BoardRow,
            (byte) piece.Column,
            (byte) piece.Row
        };
    }
    
    /// <summary>
    /// Length = 6
    /// </summary>
    private byte[] SerialisePiecePacket(Piece piece)
    {
        var buffer = new byte[6];
        
        SerialisePositionPacket(piece).CopyTo(buffer, 0);
        buffer[4] = (byte) piece.Type;
        buffer[5] = (byte) piece.Colour;
        
        return buffer;
    }

    /// <summary>
    /// Length = 9
    /// </summary>
    /// <param name="previousPosition">Byte array returned from "SerialisePositionPacket".</param>
    private byte[] SerialiseMovePacket(Piece piece, byte[] previousPosition)
    {
        var buffer = new byte[9];
        previousPosition.CopyTo(buffer, 0);
        buffer[5] = (byte) piece.BoardColumn;
        buffer[6] = (byte) piece.BoardRow;
        buffer[7] = (byte) piece.Column;
        buffer[8] = (byte) piece.Row;
        return buffer;
    }

    private Piece GetPieceInstance(string token)
    {
        var located = VirtualMap.LocatePieceInstance(token);
        
        if (located != (-1, -1, -1, -1))
        {
            return VirtualMap.Boards[located.BoardColumn, located.BoardRow]
                .Pieces[located.BoardColumn, located.BoardRow]!;
        }

        return null!;
    }
}