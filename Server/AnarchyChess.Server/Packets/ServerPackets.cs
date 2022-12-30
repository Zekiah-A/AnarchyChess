namespace AnarchyChess.Server.Packets;

public enum ServerPackets
{
    // Info sent only to the player
    Canvases,
    Token,

    // Relayed information from other clients
    Move,
    Spawn,
    Chat,
    PieceKilled,
    TurnChanged,

    // Actions and responses
    RejectMove,
    RejectSpawn,
    RejectChat,
    RejectToken,
    FriendRequest,
    FriendAdded,
    FriendRemoved,
    FriendsOnline,
}