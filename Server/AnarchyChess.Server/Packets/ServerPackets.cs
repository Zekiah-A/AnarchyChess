namespace AnarchyChess.Server.Packets;

public enum ServerPackets
{
    // Info sent only to the player
    Canvases,
    Token,

    // Relayed information from other clients
    Move,
    Spawn,
    Death,
    Chat,
    
    // Actions and responses
    TurnChanged,
    RejectMove,
    RejectSpawn,
    RejectChat,
    FriendRequest,
    FriendAdded,
    FriendRemoved,
    FriendsOnline,
}