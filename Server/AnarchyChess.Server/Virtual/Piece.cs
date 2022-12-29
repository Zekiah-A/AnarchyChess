using WatsonWebsocket;

namespace AnarchyChess.Server.Virtual;

public sealed record Piece
(
    string Token,
    PieceType Type,
    PieceColour Colour
)
{
    public int BoardColumn { get; set; }
    public int BoardRow { get; set; }
    public int Column { get; set; }
    public int Row { get; set; }
};