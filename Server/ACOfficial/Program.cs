using AnarchyChess.Server;


// GameData data, Map map, string certificatePath, string keyPath, string origin, bool ssl, int port
var server = new ServerController(8087)
{
    Logger = message =>
    {
        Console.WriteLine("[ServerInstance]: " + message);
    }
};

server.Instances.Add(new AnarchyChessServerInstance(ref server.App, ref server.Logger));
server.Instances.Add(new BattleRoyalServerInstance(ref server.App, ref server.Logger));
server.Instances.Add(new ClassicServerInstance(ref server.App, ref server.Logger));
server.Instances.Add(new SituationServerInstance(ref server.App, ref server.Logger));

await server.StartAsync();