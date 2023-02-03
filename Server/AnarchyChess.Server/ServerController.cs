using System.Buffers.Binary;
using System.Text;
using AnarchyChess.Server.Events;
using AnarchyChess.Server.Packets;
using AnarchyChess.Server.Virtual;
using Microsoft.Extensions.Logging;
using WatsonWebsocket;
namespace AnarchyChess.Server;

public abstract sealed class ServerController
{
    public List<ServerInstance> Instances = new(); 

    public ServerController(int portStart, bool ssl = false, string? certificatePath = null, string? keyPath = null)
    {
        
    }
}