using WatsonWebsocket;

namespace AnarchyChess.Server.Virtual;

/// <summary>
/// If token is default, or empty, this piece is considered, and that tile is seen as empty, when this struct is
/// passed around, you will always receive a copy, ref should always be used when mutating anything in here!
/// </summary>
public record struct Piece
(
    string Token,
    PieceType Type,
    PieceColour Colour
);