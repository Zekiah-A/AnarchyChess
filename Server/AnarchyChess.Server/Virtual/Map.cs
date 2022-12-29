using System.Runtime.InteropServices;
using System.Xml;

namespace AnarchyChess.Server.Virtual;

/// <summary>
/// The chess boards are placed on a map, 
/// </summary>
public sealed class Map
{
    public Board[,] Boards { get; private set; }

    public Map(byte columns = 1, byte rows = 1, byte pieceRows = 8, byte pieceColumns = 8, TimeSpan period = default)
    {
        Boards = new Board[columns, rows];

        for (byte x = 0; x < columns; x++)
        {
            for (byte y = 0; y < rows; y++)
            {
                Boards[x, y] = new Board(x, y, pieceColumns, pieceRows, period);
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