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
        for (byte column = 0; column < columns; column++)
        {
            for (byte row = 0; row < rows; row++)
            {
                Boards[column, row] = new Board(column, row, pieceColumns, pieceRows, period);
            }
        }
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