namespace AnarchyChess.Server.Virtual;

public sealed class Board
{
    public Piece?[,] Pieces { get; set; }
    
    public Board(byte columns = 8, byte rows = 8)
    {
        Pieces = new Piece[columns, rows];
    }
    
    public void DeletePiece()
    {
        
    }
    
    public void MovePiece()
    {
        
    }    
}