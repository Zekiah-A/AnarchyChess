namespace AnarchyChess.Server.Virtual;

public record struct PieceLocation
(
    int PieceColumn,
    int PieceRow
)
{
    public static PieceLocation Default => new(-1, -1);
    
    // This allows us to emulate inheritance, by allowing a BoardLocation to be casted to a PieceLocation
    public static implicit operator PieceLocation(BoardLocation boardLocation)
    {
        return new PieceLocation(boardLocation.PieceColumn, boardLocation.PieceRow);
    }
};