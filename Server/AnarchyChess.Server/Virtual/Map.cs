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
    
    public Map(byte columns = 1, byte rows = 1, byte pieceColumns = 8, byte pieceRows = 8, TimeSpan period = default)
    {
        Boards = new Board[columns, rows];
        Columns = columns;
        Rows = rows;
        TokenLocations = new Dictionary<string, BoardLocation>();
        
        for (byte x = 0; x < columns; x++)
        {
            for (byte y = 0; y < rows; y++)
            {
                Boards[x, y] = new Board(pieceColumns, pieceRows, period);
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
        if (newLocation.BoardColumn < 0 || newLocation.BoardColumn > Columns || newLocation.BoardRow < 0 ||
            newLocation.BoardRow > Rows)
        {
            return false;
        }
        
        var board = Boards[newLocation.BoardColumn, newLocation.BoardRow];
        
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

    //TODO: Move move validation up to server instance level
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
                        if (offset == BoardLocation.Default || (x == currentLocation.PieceColumn && y == currentLocation.PieceRow))
                            continue;
                        
                        validMoves.Add(offset);
                    }
                }
                break;
            }
            case PieceType.Knight:
            {
                var offsets = new[]
                {
                    GetOffsetLocation(currentLocation with
                    {
                        PieceColumn = currentLocation.PieceColumn + 1, PieceRow = currentLocation.PieceRow + 2
                    }),
                    GetOffsetLocation(currentLocation with
                    {
                        PieceColumn = currentLocation.PieceColumn + 2, PieceRow = currentLocation.PieceRow + 1
                    }),
                    GetOffsetLocation(currentLocation with
                    {
                        PieceColumn = currentLocation.PieceColumn + 2, PieceRow = currentLocation.PieceRow - 1
                    }),
                    GetOffsetLocation(currentLocation with
                    {
                        PieceColumn = currentLocation.PieceColumn + 1, PieceRow = currentLocation.PieceRow - 2
                    }),
                    GetOffsetLocation(currentLocation with
                    {
                        PieceColumn = currentLocation.PieceColumn - 1, PieceRow = currentLocation.PieceRow - 2
                    }),
                    GetOffsetLocation(currentLocation with
                    {
                        PieceColumn = currentLocation.PieceColumn - 2, PieceRow = currentLocation.PieceRow - 1
                    }),
                    GetOffsetLocation(currentLocation with
                    {
                        PieceColumn = currentLocation.PieceColumn - 2, PieceRow = currentLocation.PieceRow + 1
                    }),
                    GetOffsetLocation(currentLocation with
                    {
                        PieceColumn = currentLocation.PieceColumn - 1, PieceRow = currentLocation.PieceRow + 2
                    })
                };

                validMoves.AddRange(offsets.Where(location => location != PieceLocation.Default));
                break;
            }
            case PieceType.Pawn:
            {
                var offset = GetOffsetLocation(currentLocation with
                {
                    PieceColumn = currentLocation.PieceColumn - 1, PieceRow = currentLocation.PieceRow - 1
                });
                if (offset != BoardLocation.Default)
                {
                    validMoves.Add(offset);
                }

                offset = GetOffsetLocation(currentLocation with
                {
                    PieceColumn = currentLocation.PieceColumn - 1, PieceRow = currentLocation.PieceRow + 1
                });
                if (offset != BoardLocation.Default)
                {
                    validMoves.Add(offset);
                }

                offset = GetOffsetLocation(currentLocation with
                {
                    PieceColumn = colour == PieceColour.Black
                        ? currentLocation.PieceColumn - 1
                        : currentLocation.PieceColumn + 1,
                    PieceRow = currentLocation.PieceRow
                });
                if (offset != BoardLocation.Default)
                {
                    validMoves.Add(offset);
                }
                break;
            }
            case PieceType.Queen:
            {
                break;
            }
            case PieceType.Rook:
            {
                for (var x = currentLocation.PieceColumn - 8; x < currentLocation.PieceColumn + 8; x++)
                {
                    var offset = GetOffsetLocation(currentLocation with {PieceColumn = x});
                    if (offset != BoardLocation.Default && x != currentLocation.PieceColumn)
                    {
                        validMoves.Add(offset);
                    }
                }

                for (var y = currentLocation.PieceRow - 8; y < currentLocation.PieceRow + 8; y++)
                {
                    var offset = GetOffsetLocation(currentLocation with { PieceRow = y });
                    if (offset != BoardLocation.Default && y != currentLocation.PieceRow)
                    {
                        validMoves.Add(offset); 
                    }
                }
                break;
            }
        }
        
        return validMoves.ToArray();
    }

    private BoardLocation GetOffsetLocation(BoardLocation newLocation)
    {
        while (newLocation.PieceColumn < 0)
        {
            newLocation.BoardColumn -= 1;
            if (newLocation.BoardColumn < 0)
            {
                return BoardLocation.Default;
            }

            newLocation.PieceColumn = Boards[newLocation.BoardColumn, newLocation.BoardRow].Columns - newLocation.PieceColumn;
        }
        while (newLocation.PieceRow < 0)
        {
            newLocation.BoardRow -= 1;
            if (newLocation.BoardRow < 0)
            {
                return BoardLocation.Default;
            }

            newLocation.PieceRow = Boards[newLocation.BoardColumn, newLocation.BoardRow].Rows - newLocation.PieceRow;
        }
        while (newLocation.PieceColumn > Boards[newLocation.BoardColumn, newLocation.BoardRow].Columns - 1)
        {
            newLocation.BoardColumn += 1;
            if (newLocation.BoardColumn > Columns)
            {
                return BoardLocation.Default;
            }

            newLocation.PieceColumn -= Boards[newLocation.BoardColumn, newLocation.BoardRow].Columns;
        }
        while (newLocation.PieceRow > Boards[newLocation.BoardColumn, newLocation.BoardRow].Rows - 1)
        {
            newLocation.BoardRow += 1;
            if (newLocation.BoardRow > Rows)
            {
                return BoardLocation.Default;
            }

            newLocation.PieceRow -= Boards[newLocation.BoardColumn, newLocation.BoardRow].Rows;
        }
        
        return newLocation;
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