using WatsonWebsocket;

namespace AnarchyChess.Server.Virtual;

public record Piece(string Guid, PieceType Type, Board Board);