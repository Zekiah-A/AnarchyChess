using Microsoft.Extensions.Logging;
using WatsonWebsocket;
namespace AnarchyChess.Server;

public sealed class ServerController
{
    private List<IServerInstance> instances;
    
    public IReadOnlyList<IServerInstance> Instances { get => instances.AsReadOnly(); }
    public Action<string>? Logger;

    public ServerController()
    {
        instances = new List<IServerInstance>();
    }

    public void AttachInstance(IServerInstance instance, int port, bool ssl = false, string hostname = "localhost", string? certificatePath = null, string? keyPath = null)
    {
        instance.Logger = Logger;
        instance.App = new WatsonWsServer(port, ssl, certificatePath, keyPath, LogLevel.None, hostname);
        instance.InstanceId = instances.Count < 255 ? instances.Count + 1 : throw new ArgumentOutOfRangeException();
        instances.Add(instance);
    }
    
    public async Task StartAsync()
    {
        await Task.WhenAll(Instances.Select(instance => instance.StartAsync()));
        await Task.Delay(-1);
    }
}