using AnarchyChess.Server;

//GameData data, Map map, string certificatePath, string keyPath, string origin, bool ssl, int port
var server = new ServerInstance(8087);
await server.StartAsync();