using AnarchyChess.Server.Virtual;
using WatsonWebsocket;
namespace AnarchyChess.Server;

public abstract class ServerInstance : IServerInstance
{
    public Map VirtualMap { get; init; }
    public DualDictionary<ClientMetadata, string> Clients { get; }
    public WatsonWsServer App { get; set; }
    public Action<string> Logger { get; set;  }
    public int InstanceId { get; set; }

    public ServerInstance(Map? map = null)
    {
        Clients = new DualDictionary<ClientMetadata, string>();
    }

    public async Task StartAsync()
    {
        App.ClientConnected += OnClientConnected;
        App.MessageReceived += OnMessageReceived;
        App.ClientDisconnected += OnClientDisconnected;

        await App.StartAsync();
    }

    private protected virtual void OnClientConnected(object? sender, ClientConnectedEventArgs args)
    { }

    private protected virtual void OnMessageReceived(object? sender, MessageReceivedEventArgs args)
    { }

    private protected virtual void OnClientDisconnected(object? sender, ClientDisconnectedEventArgs args)
    { }

    /// <summary>
    /// Length = 4
    /// </summary>
    private protected byte[] SerialisePositionPacket(string token)
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
    private protected byte[] SerialisePiecePacket(Piece piece)
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
    private protected byte[] SerialiseMovePacket(string token, byte[] previousPosition)
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
