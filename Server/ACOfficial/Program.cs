using AnarchyChess.Server;
using AnarchyChess.Server.Virtual;

// GameData data, Map map, string certificatePath, string keyPath, string origin, bool ssl, int port
Console.WriteLine("Server starting...");
var server = new ServerInstance(8087, new Map(4, 4, period: TimeSpan.FromSeconds(10)));

server.Logger += message =>
{
    Console.WriteLine("[ServerInstance]: " + message);
};

await server.StartAsync();
await Task.Delay(-1);