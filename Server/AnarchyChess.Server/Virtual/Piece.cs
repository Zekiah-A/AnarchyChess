using WatsonWebsocket;

namespace AnarchyChess.Server.Virtual;

public sealed record Piece(string Guid, PieceType Type, PieceColour Colour, Board Board);