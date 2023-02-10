using AnarchyChess.Server.Virtual;
using WatsonWebsocket;

namespace AnarchyChess.Server;

public interface IServerInstance
{
    public Map VirtualMap { get; init; }
    public DualDictionary<ClientMetadata, string> Clients { get; }
    public WatsonWsServer App { get; }
    public Action<string>? Logger { get; }
    public int InstanceId { get; set; }
    
    public async Task StartAsync() { }
}