using System.Xml;

namespace AnarchyChess.Server.Virtual;

/// <summary>
/// The chess boards are placed on a map, 
/// </summary>
public sealed class Map
{
    public Board[,] Boards { get; private set; }

    public Map(byte columns = 1, byte rows = 1, byte pieceRows = 8, byte pieceColumns = 8, TimeSpan? period = null)
    {
        Boards = new Board[columns, rows];
        Boards.Fill(new Board(pieceRows, pieceColumns, period));
    }

    public void ShiftAll(int directionX, int directionY)
    {
        
    }

    public void AddColumn(int amount)
    {
        
    }

    public void AddRow(int amount)
    {
        
    }
}