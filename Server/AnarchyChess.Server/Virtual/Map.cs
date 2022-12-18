using System.Xml;

namespace AnarchyChess.Server.Virtual;

/// <summary>
/// The chess boards are placed on a map, 
/// </summary>
public sealed class Map
{
    public Board[,] Boards { get; private set; }

    public Map(byte columns = 1, byte rows = 1, byte pieceRows = 8, byte pieceColumns = 8)
    {
        Boards = new Board[columns, rows];
        Boards.Fill(new Board(pieceRows, pieceColumns));
    }

    public void ShiftAll(int directionX, int directionY)
    {
        
    }

    public void AddBoard(int column, int row)
    {
        
    }

    public void RemoveBoard(int column, int row)
    {
        
    }

    public void MovePiece(Piece piece)
    {
        
    }

    public void DeletePiece(Piece piece)
    {
        
    }

    public void AddPiece(Piece piece)
    {
        
    }
}