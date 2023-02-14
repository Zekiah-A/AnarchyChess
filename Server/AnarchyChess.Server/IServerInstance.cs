using AnarchyChess.Server.Virtual;
using WatsonWebsocket;

namespace AnarchyChess.Server;

public interface IServerInstance
{
    public Map VirtualMap { get; init; }
    public DualDictionary<ClientMetadata, string> Clients { get; }
    public WatsonWsServer App { get; set; }
    public Action<string>? Logger { get; set;  }
    public int InstanceId { get; set; }

    public Task StartAsync()
    {
        return Task.CompletedTask;
    }
}