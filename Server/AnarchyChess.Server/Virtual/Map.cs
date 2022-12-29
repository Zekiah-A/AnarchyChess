using System.Runtime.InteropServices;
using System.Xml;

namespace AnarchyChess.Server.Virtual;

/// <summary>
/// The chess boards are placed on a map, 
/// </summary>
public sealed class Map
{
    public Board[,] Boards { get; private set; }
    public byte Columns { get; set; }
    public byte Rows { get; set; }
    public Dictionary<string, (int BoardColumn, int BoardRow, int PieceColumn, int PieceRow)> TokenLocations { get; private set; } 

    public Map(byte columns = 1, byte rows = 1, byte pieceRows = 8, byte pieceColumns = 8, TimeSpan period = default)
    {
        Boards = new Board[columns, rows];
        Columns = columns;
        Rows = rows;
        TokenLocations = new Dictionary<string, (int BoardColumn, int BoardRow, int PieceColumn, int PieceRow)>();
        
        for (byte x = 0; x < columns; x++)
        {
            for (byte y = 0; y < rows; y++)
            {
                Boards[x, y] = new Board(x, y, pieceColumns, pieceRows, period);
            }
        }
    }

    public bool TrySpawnPiece(Piece piece)
    {
        if (!Boards[piece.BoardColumn, piece.BoardRow].TrySpawnPiece(piece))
        {
            return false;
        }
        
        TokenLocations.Add(piece.Token, (piece.BoardColumn, piece.BoardRow, piece.Column, piece.Row));
        return true;
    }

    public bool TryMovePiece(Piece piece, int toColumn, int toRow)
    {
        if (!Boards[piece.BoardColumn, piece.BoardRow].TryMovePiece(piece, toColumn, toRow))
        {
            // TODO: Check for if ToColumn/ToRow is off that piece's current board, if so, initiate a piece transfer
            // TODO: to that new board.
            return false;
        }

        TokenLocations.Remove(piece.Token);
        TokenLocations.Add(piece.Token, (piece.BoardColumn, piece.BoardRow, toColumn, toRow));
        return true;
    }

    public void DeletePiece(Piece piece)
    {
        // This passes straight through map - no operations needed.
        Boards[piece.BoardColumn, piece.BoardRow].DeletePiece(piece);
    }

    // Will try to find a piece on the map, by looking through TokenLocation caches and recursion.
    public (int BoardColumn, int BoardRow, int PieceColumn, int PieceRow) LocatePieceInstance(string token)
    {
        if (TokenLocations.TryGetValue(token, out var location))
        {
            var pieceLocation = Boards[location.BoardColumn, location.BoardRow].LocatePieceInstance(token);

            return (location.BoardColumn, location.BoardRow, pieceLocation.PieceRow, pieceLocation.PieceColumn);
        }

        for (byte x = 0; x < Columns; x++)
        {
            for (byte y = 0; y < Rows; y++)
            {
                var pieceLocation = Boards[x, y].LocatePieceInstance(token);

                if (pieceLocation == (-1, -1))
                {
                    continue;
                }
                
                var foundLocation = (x, y, pieceLocation.PieceRow, pieceLocation.PieceColumn);
                TokenLocations.Add(token, foundLocation);
                return foundLocation;
            }
        }
        
        return (-1, -1, -1, -1);
    }
}