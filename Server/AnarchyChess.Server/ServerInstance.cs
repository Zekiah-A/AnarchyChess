namespace AnarchyChess.Server;

public sealed class ServerInstance
{
    private GameData gameData;

    public ServerInstance(GameData data)
    {
        gameData = data;
    }

    public async Task Start()
    {
        //Create WS, etc
    }
}