namespace AnarchyChess.Server.Virtual;

/// <summary>
/// The chess boards are placed on a map, 
/// </summary>
public struct Map
{
    public Board[,] Boards { get; private set; }
    public byte Columns { get; set; }
    public byte Rows { get; set; }
    public Dictionary<string, BoardLocation> TokenLocations { get; private set; }
    
    public Map(byte columns = 1, byte rows = 1, byte pieceRows = 8, byte pieceColumns = 8, TimeSpan period = default)
    {
        Boards = new Board[columns, rows];
        Columns = columns;
        Rows = rows;
        TokenLocations = new Dictionary<string, BoardLocation>();
        
        for (byte x = 0; x < columns; x++)
        {
            for (byte y = 0; y < rows; y++)
            {
                Boards[x, y] = new Board(x, y, pieceColumns, pieceRows, period);
            }
        }
    }

    public bool TrySpawnPiece(Piece piece, BoardLocation location)
    {
        if (!Boards[location.BoardColumn, location.BoardRow].TrySpawnPiece(piece, location))
        {
            return false;
        }
        
        TokenLocations.Add(piece.Token, location);
        return true;
    }

    public bool TryMovePiece(string token, BoardLocation newLocation)
    {
        var currentLocation = LocatePieceInstance(token);
        var board = Boards[currentLocation.BoardColumn, currentLocation.BoardRow];

        // Check if this movement across boards is legal before performing board-specific checks
        if (!board.TryMovePiece(token, newLocation)) //Make this return a BoardLocation
        {
            // TODO: Check for if ToColumn/ToRow is off that piece's current board, if so, initiate a piece transfer
            // TODO: to that new board. Mutating the piece's boardrow and coardcolumn values (via reference).
            return false;
        }

        TokenLocations.Remove(token);
        TokenLocations.Add(token, newLocation);
        return true;
    }

    public void DeletePiece(string token)
    {
        var location = LocatePieceInstance(token);
        
        TokenLocations.Remove(token);
        Boards[location.BoardColumn, location.BoardRow].DeletePiece(token);
    }

    // Will try to find a piece on the map, by looking through TokenLocation caches and iteration.
    public BoardLocation LocatePieceInstance(string token)
    {
        if (TokenLocations.TryGetValue(token, out var location))
        {
            var pieceLocation = Boards[location.BoardColumn, location.BoardRow].LocatePieceInstance(token);
            return location with { PieceColumn = pieceLocation.PieceColumn, PieceRow = pieceLocation.PieceRow };
        }

        for (byte x = 0; x < Columns; x++)
        {
            for (byte y = 0; y < Rows; y++)
            {
                var pieceLocation = Boards[x, y].LocatePieceInstance(token);

                if (pieceLocation == PieceLocation.Default)
                {
                    continue;
                }
                
                var foundLocation = new BoardLocation(x, y, pieceLocation.PieceColumn, pieceLocation.PieceRow);
                TokenLocations.Add(token, foundLocation);
                return foundLocation;
            }
        }
        
        return BoardLocation.Default;
    }
}