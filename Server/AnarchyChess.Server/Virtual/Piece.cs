using WatsonWebsocket;

namespace AnarchyChess.Server.Virtual;

public sealed record Piece
(
    string Token,
    PieceType Type,
    PieceColour Colour,
    Board Board
);