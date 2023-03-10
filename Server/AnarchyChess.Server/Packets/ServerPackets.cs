namespace AnarchyChess.Server.Packets;

public enum ServerPackets
{
    // Info sent only to the player
    Canvases,
    Token,
    ColourBalance,

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
    Me = 255 // Confirms to a client that an acton originated from them
}