using AnarchyChess.Server;
using AnarchyChess.Server.Virtual;

// GameData data, Map map, string certificatePath, string keyPath, string origin, bool ssl, int port
Console.WriteLine("Server starting...");
var server = new ServerInstance(8087, new Map(10, 10, period: TimeSpan.FromSeconds(10)));
await server.StartAsync();