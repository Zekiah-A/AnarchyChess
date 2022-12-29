namespace AnarchyChess.Server.Virtual;

public record struct BoardLocation
(
    int BoardRow,
    int BoardColumn,
    int PieceRow,
    int PieceColumn
)
{
    public static BoardLocation Default => new(-1, -1, -1, -1);
}