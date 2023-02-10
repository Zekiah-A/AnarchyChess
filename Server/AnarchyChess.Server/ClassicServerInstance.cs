using System.Buffers.Binary;
using System.Text;
using AnarchyChess.Server.Events;
using AnarchyChess.Server.Packets;
using AnarchyChess.Server.Virtual;
using Microsoft.Extensions.Logging;
using WatsonWebsocket;
namespace AnarchyChess.Server;

public sealed class ClassicServerInstance : ServerInstance
{
    public ClassicServerInstance(ref WatsonWsServer server, ref Action<string>? logger, Map? map = null) : base(ref server, ref logger, map)
    {
    }
}