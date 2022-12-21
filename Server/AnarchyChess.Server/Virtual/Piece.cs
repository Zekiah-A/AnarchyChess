using WatsonWebsocket;

namespace AnarchyChess.Server.Virtual;

public sealed record Piece
(
    string Token,
    PieceType Type,
    PieceColour Colour
)
{
    public int Column;
    public int Row;
    public int BoardColumn;
    public int BoardRow;
};
