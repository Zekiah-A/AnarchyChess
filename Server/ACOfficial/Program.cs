using AnarchyChess.Server;

// GameData data, Map map, string certificatePath, string keyPath, string origin, bool ssl, int port
var server = new ServerController
{
    Logger = message =>
    {
        Console.WriteLine("[ServerInstance]: " + message);
    }
};

server.AttachInstance(new AnarchyChessServerInstance(), 8087);
await server.StartAsync();