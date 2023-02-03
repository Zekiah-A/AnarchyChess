using AnarchyChess.Server;

// GameData data, Map map, string certificatePath, string keyPath, string origin, bool ssl, int port
var server = new ServerController(8087);
server.CreateInstance<AnarchyChessServerInstance>();
server.CreateInstance<BattleRoyalServerInstance>();
server.CreateInstance<ClassicServerInstance>();
server.CreateInstance<SituationServerInstance>();

server.Logger = message =>
{
    Console.WriteLine("[ServerInstance]: " + message);
};

await server.StartAsync();