using System.Buffers.Binary;
using System.Text;
using AnarchyChess.Server.Events;
using AnarchyChess.Server.Packets;
using AnarchyChess.Server.Virtual;
using Microsoft.Extensions.Logging;
using WatsonWebsocket;
namespace AnarchyChess.Server;

public abstract sealed class ServerInstance
{
    public DualDictionary<ClientMetadata, string> Clients { get; set; } = new();
    public Action<string>? Logger;
    private WatsonWsServer app;

    public virtual async Task StartAsync()
    {         
        await app.StartAsync();
    }

    private virtual void DeleteIdlePieces(object? state)
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

    private virtual void RemovePiece(string token)
    {
        // Delete piece from that board
        VirtualMap.DeletePiece(token);

        // We send a position packet to say where the killed player was
        var killBuffer = new byte[6];
        killBuffer[0] = (byte) ServerPackets.PieceKilled;
        SerialisePositionPacket(token).CopyTo(killBuffer, 1);
        
        foreach (var client in app.Clients)
        {
            if (Clients.TryGetValue(client, out var pieceToken) && pieceToken.Equals(token))
            {
                killBuffer[5] = (byte) ServerPackets.Me;
                app.SendAsync(client, killBuffer);
                killBuffer[5] = 0;
                continue;
            }

            app.SendAsync(client, killBuffer);
        }
    }
    
    private virtual void OnPieceKilled(object? sender, PieceKilledEventArgs args)
    {
        RemovePiece(args.Killed.Token);
        Logger?.Invoke($"Piece {args.Killed.Token} was killed by {args.Killer.Token}.");
    }

    private virtual void OnTurnChanged(object? sender, TurnChangedEventArgs args)
    {
        // Turn change packet is data[1..2] = (ushort) current turn, data[3..7] = position packet of currently playing piece.
        var turnBuffer = (Span<byte>) stackalloc byte[8];
        turnBuffer[0] = (byte) ServerPackets.TurnChanged;
        BinaryPrimitives.WriteUInt16BigEndian(turnBuffer[1..], (ushort) args.Turn);
        new Span<byte>(SerialisePositionPacket(args.Token)).CopyTo(turnBuffer[3..]);
        
        lock (app.Clients)
        {
            foreach (var client in app.Clients)
            {
                if (Clients.TryGetKey(args.Token, out var clientMetadata) && clientMetadata == client)
                {
                    turnBuffer[7] = (byte) ServerPackets.Me;
                    app.SendAsync(client, turnBuffer.ToArray());
                    turnBuffer[7] = 0;
                }
                else
                {
                    app.SendAsync(client, turnBuffer.ToArray());
                }
            }
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
    /// Length = 8
    /// </summary>
    /// <param name="token">Token of client that we are serialising for</param>
    /// <param name="previousPosition">Byte array returned from "SerialisePositionPacket".</param>
    private byte[] SerialiseMovePacket(string token, byte[] previousPosition)
    {
        var located = VirtualMap.LocatePieceInstance(token);
        var buffer = new byte[8];
        previousPosition.CopyTo(buffer, 0);
        buffer[4] = (byte) located.BoardColumn;
        buffer[5] = (byte) located.BoardRow;
        buffer[6] = (byte) located.PieceColumn;
        buffer[7] = (byte) located.PieceRow;
        
        return buffer;
    }
}

// TODO: When saving board state, for example, with a server restart, we should 
// TODO: include the clients there (the piece instances in the board pieces arrays).
