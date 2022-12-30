namespace AnarchyChess.Server.Virtual;

public record struct BoardLocation
(
    int BoardColumn,
    int BoardRow,
    int PieceColumn,
    int PieceRow
)
{
    public static BoardLocation Default => new(-1, -1, -1, -1);
}