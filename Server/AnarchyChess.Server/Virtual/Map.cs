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

        var piece = GetPieceInstance(token);
        var valid = GetValidMoves(currentLocation, piece.Type, piece.Colour);

        if (!board.TryMovePiece(token, newLocation))
        {
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

    private BoardLocation[] GetValidMoves(BoardLocation currentLocation, PieceType type, PieceColour colour)
    {
        var validMoves = new List<BoardLocation>();
        
        switch (type)
        {
            case PieceType.Bishop:
            {
                break;
            }
            case PieceType.King:
            {
                for (var x = currentLocation.PieceColumn - 1; x <= currentLocation.PieceColumn + 1; x++)
                {
                    for (var y = currentLocation.PieceRow - 1; y <= currentLocation.PieceRow + 1; y++)
                    {
                        var offset = GetOffsetLocation(currentLocation with { PieceColumn = x, PieceRow = y });
                        if (offset is null || (x == currentLocation.PieceColumn && y == currentLocation.PieceRow))
                            continue;
                        
                        validMoves.Add(offset.Value);
                    }
                }
                break;
            }
            case PieceType.Knight:
            {
                break;
            }
            case PieceType.Pawn:
            {
                break;
            }
            case PieceType.Queen:
            {
                break;
            }
            case PieceType.Rook:
            {
                break;
            }
        }
        
        return validMoves.ToArray();
        /*
        switch(type) {
            case pieceTypes.Bishop:
                break
            case pieceTypes.King: {
                for (let x = column - 1; x <= column + 1; x++) {
                    for (let y = row - 1; y <= row + 1; y++) {
                        let offset = getOffsetLocation(boardX, boardY, x, y)
                        if ((x == column && y == row) || offset == null)
                            continue

                        validMoves.push(offset)
                    }
                }
                break
            }
            case pieceTypes.Knight: {
                let offsets: Array<(BoardLocation | null)> = [
                    getOffsetLocation(boardX, boardY, column + 1, row + 2),
                    getOffsetLocation(boardX, boardY, column + 2, row + 1),
                    getOffsetLocation(boardX, boardY, column + 2, row - 1),
                    getOffsetLocation(boardX, boardY, column + 1, row - 2),
                    getOffsetLocation(boardX, boardY, column - 1, row - 2),
                    getOffsetLocation(boardX, boardY, column - 2, row - 1),
                    getOffsetLocation(boardX, boardY, column - 2, row + 1),
                    getOffsetLocation(boardX, boardY, column - 1, row + 2)
                ]

                for (let i = 0; i < offsets.length; i++) {
                    if (offsets[i] == null)
                        continue

                    validMoves.push(offsets[i])
                }
                break
            }
            case pieceTypes.Pawn: {
                let offset = getOffsetLocation(boardX, boardY, column - 1, row - 1)
                if (offset != null) {
                    let pieces = map.boards[offset.boardColumn][offset.boardRow].pieces

                    if ((pieces[offset.pieceColumn - 1] != null  && pieces[offset.pieceColumn - 1][offset.pieceRow - 1] != null)) {
                        validMoves.push(offset)
                    }
                }

                offset = getOffsetLocation(boardX, boardY, column - 1, row + 1)
                if (offset != null) {
                    let pieces = map.boards[offset.boardColumn][offset.boardRow].pieces

                    if ((pieces[offset.pieceColumn - 1] != null && pieces[offset.pieceColumn - 1][offset.pieceRow + 1] != null)) {
                        validMoves.push(offset)
                    }
                }

                offset = getOffsetLocation(boardX, boardY, column, colour == pieceColours.Black ? row - 1 : row + 1)
                if (offset != null) {
                    let pieces = map.boards[offset.boardColumn][offset.boardRow].pieces

                    if ((pieces[offset.pieceColumn - 1] == null)) {
                        validMoves.push(offset)
                    }
                }
                break
            }
            case pieceTypes.Queen:
                break
            case pieceTypes.Rook: {
                for (let x = -COLUMNS + column; x < COLUMNS + column + 1; x++) {
                    let offset = getOffsetLocation(boardX, boardY, x, row)
                    if (x == column || offset == null)
                        continue

                    validMoves.push(offset)

                }

                for (let y = -ROWS + row; y < ROWS + row + 1; y++) {
                    let offset = getOffsetLocation(boardX, boardY, column, y)
                    if (y == row || offset == null)
                        continue

                    validMoves.push(offset)
                }
                break
            }
        }
         */
    }

    private BoardLocation? GetOffsetLocation(BoardLocation currentLocation)
    {
        return BoardLocation.Default;
        /*
        while (newLocation.pieceColumn < 0) {
            newLocation.boardColumn -= 1
            if (newLocation.boardColumn < 0) {
                return null
            }

            newLocation.pieceColumn = COLUMNS - newLocation.pieceColumn
        }
        while (newLocation.pieceRow < 0) {
            newLocation.boardRow -= 1
            if (newLocation.boardRow < 0) {
                return null
            }

            newLocation.pieceRow = ROWS - newLocation.pieceRow
        }
        while (newLocation.pieceColumn > COLUMNS - 1) {            
            newLocation.boardColumn += 1
            if (newLocation.boardColumn > map.boardsColumns) {
                return null
            }

            newLocation.pieceColumn -= COLUMNS
        }
        while (newLocation.pieceRow > ROWS - 1) {
            newLocation.boardRow += 1
            if (newLocation.boardRow > map.boardsRows) {
                return null
            }

            newLocation.pieceRow -= ROWS
        }

        return newLocation
         */
    }
    
    public ref Piece GetPieceInstance(string token)
    {
        var located = LocatePieceInstance(token);
        
        return ref Boards[located.BoardColumn, located.BoardRow]
            .Pieces[located.PieceColumn, located.PieceRow];
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