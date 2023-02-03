using System.Buffers.Binary;
using System.Text;
using AnarchyChess.Server.Events;
using AnarchyChess.Server.Packets;
using AnarchyChess.Server.Virtual;
using Microsoft.Extensions.Logging;
using WatsonWebsocket;
namespace AnarchyChess.Server;

public sealed class ServerController
{
    public List<IServerInstance> Instances;
    public Action<string>? Logger;
    public WatsonWsServer App;

    public ServerController(int port, bool ssl = false, string? hostname = null, string? certificatePath = null, string? keyPath = null)
    {
        Instances = new List<IServerInstance>();
        App = new WatsonWsServer(port, ssl, certificatePath, keyPath, LogLevel.Trace, hostname ?? "localhost");
    }

    public void CreateInstance<T>(params object[]? args) where T : IServerInstance
    {
        var instance = (T) Activator.CreateInstance(typeof(T), App, Logger, args)!;
        Instances.Add(instance);
    }

    public async Task StartAsync()
    {
        await Task.WhenAll(Instances.Select(instance => instance.StartAsync()).ToList());
        await Task.Delay(-1);
    }
}