using Microsoft.Extensions.Logging;
using WatsonWebsocket;
namespace AnarchyChess.Server;

public sealed class ServerController
{
    private IList<IServerInstance> instances;
    
    public IList<IServerInstance> Instances { get => instances.AsReadOnly(); private init => instances = value; }
    public Action<string>? Logger;
    public WatsonWsServer App;

    public ServerController(int port, bool ssl = false, string? hostname = null, string? certificatePath = null, string? keyPath = null)
    {
        Instances = new List<IServerInstance>();
        App = new WatsonWsServer(port, ssl, certificatePath, keyPath, LogLevel.Trace, hostname ?? "localhost");
    }

    public void AttachInstance(IServerInstance instance)
    {
        instance.InstanceId = instances.Count < 255 ? instances.Count + 1 : throw new ArgumentOutOfRangeException();
        Instances.Add(instance);
    }
    
    public async Task StartAsync()
    {
        await Task.WhenAll(Instances.Select(instance => instance.StartAsync()).ToList());
        await App.StartAsync();
        await Task.Delay(-1);
    }
}