using System.Xml;

namespace AnarchyChess.Server.Virtual;

/// <summary>
/// The chess boards are placed on a map, 
/// </summary>
public sealed class Map
{
    public Board?[,] Boards;

    public Map(byte columns = 1, byte rows = 1)
    {
        Boards = new Board?[columns, rows];
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