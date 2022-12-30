using System.Timers;
using AnarchyChess.Server.Events;
using Timer = System.Timers.Timer;

namespace AnarchyChess.Server.Virtual;

// ServerInstance should never have to see this class, only it's direct descendant, Map
public sealed class Board
{
    public Piece[,] Pieces { get; private set; }
    public List<Piece> Turns { get; private set; }
    public int CurrentTurn { get; private set; }
    private Timer TurnTimer { get; set; }
    private int Columns { get; }
    private int Rows { get; }
    public byte Column { get; }
    public byte Row { get; }
    public event EventHandler<TurnChangedEventArgs> TurnChangedEvent = (_, _) => { };
    public event EventHandler<PieceKilledEventArgs> PieceKilledEvent = (_, _) => { };

    public Board(byte row, byte column, byte columns = 8, byte rows = 8, TimeSpan period = default)
    {
        Row = row;
        Column = column;
        Rows = rows;
        Columns = columns;
        Turns = new List<Piece>();
        Pieces = new Piece[columns, rows];
        
        TurnTimer = new Timer(period.Milliseconds <= 0 ? 10_000 : period.Milliseconds);
        TurnTimer.Elapsed += ProgressTurn;
        TurnTimer.Enabled = true;
        TurnTimer.Start();
    }
    
    public void ProgressTurn(object? sender, ElapsedEventArgs? args)
    {
        if (Turns.Count == 0)
        {
            return;
        }

        CurrentTurn = CurrentTurn == Turns.Count - 1 ? 0 : CurrentTurn + 1;
        TurnChangedEvent.Invoke(this, new TurnChangedEventArgs(CurrentTurn, Turns[CurrentTurn]));
    }
    
    // Spawning piece is instant, but their turn to move is last
    public bool TrySpawnPiece(Piece piece, PieceLocation location)
    {
        if (!string.IsNullOrEmpty(Pieces[location.PieceColumn, location.PieceRow].Token))
        {
            return false;
        }
        
        Pieces[location.PieceColumn, location.PieceRow] = piece;
        Turns.Add(piece);
        return true;
    }
    
    public bool TryMovePiece(string token, PieceLocation newLocation)
    {
        TurnTimer.Stop();

        var currentLocation = LocatePieceInstance(token);
        var piece = Pieces[currentLocation.PieceColumn, currentLocation.PieceRow];

        if (Turns[CurrentTurn].Equals(piece))
        {
            return false;
        }
        
        var moveColumns = newLocation.PieceColumn - currentLocation.PieceColumn;
        var moveRows = newLocation.PieceRow - currentLocation.PieceRow;
        
        var valid = false;
        switch (piece.Type)
        {
            case PieceType.Bishop:
                valid = moveColumns is < 8 and > -8 && moveRows is < 8 and > -8 && Math.Abs(moveRows) == Math.Abs(moveColumns);

                if (PieceBlocksDiagonal(currentLocation, newLocation))
                {
                    valid = false;
                }
                break;
            case PieceType.King:
                valid = moveColumns switch
                {
                    1 or -1 when moveRows is 0 => true,
                    0 when moveRows is 1 or -1 => true,
                    1 or -1 when moveRows is 1 or -1 && moveRows == moveColumns => true,
                    _ => valid
                };
                break;
            case PieceType.Knight:
                valid = moveColumns switch
                {
                    2 or -2 when moveRows is 1 or -1 => true,
                    1 or -1 when moveRows is 2 or -2 => true,
                    _ => valid
                };
                break;
            case PieceType.Pawn:
                valid = moveColumns switch
                {
                    1 when moveRows is 0 && piece.Colour == PieceColour.White => true,
                    -1 when moveRows is 0 && piece.Colour == PieceColour.Black => true,
                    1 or -1 when moveRows is 1 && piece.Colour == PieceColour.White &&
                        !string.IsNullOrEmpty(Pieces[newLocation.PieceColumn, newLocation.PieceRow].Token) => true,
                    1 or -1 when moveRows is -1 && piece.Colour == PieceColour.Black &&
                        !string.IsNullOrEmpty(Pieces[newLocation.PieceColumn, newLocation.PieceRow].Token) => true,
                    _ => valid
                };
                break;
            case PieceType.Queen:
                valid = moveColumns switch
                {
                    0 when moveRows is < 8 and > -8 => true,
                    < 8 and > -8 when moveRows is 0 => true,
                    < 8 and > -8 when moveRows is < 8 and > -8 && Math.Abs(moveRows) == Math.Abs(moveColumns) => true,
                    _ => valid
                };

                if (PieceBlocksHorizontal(currentLocation, newLocation) || PieceBlocksVertical(currentLocation, newLocation)
                                                                        || PieceBlocksDiagonal(currentLocation, newLocation))
                {
                    valid = false;
                }
                break;
            case PieceType.Rook:
                valid = moveColumns switch
                {
                    0 when moveRows is < 8 and > -8 => true,
                    < 8 and > -8 when moveRows is 0 => true,
                    _ => valid
                };

                if (PieceBlocksHorizontal(currentLocation, newLocation) || PieceBlocksVertical(currentLocation, newLocation))
                {
                    valid = false;
                }
                break;
        }
        
        if (!valid)
        {
            return false;
        }
        
        // If we are landing on an occupied space, we are taking that piece
        var taking = Pieces[newLocation.PieceColumn, newLocation.PieceRow];
        if (!string.IsNullOrEmpty(taking.Token) && taking.Colour != piece.Colour)
        {
            PieceKilledEvent.Invoke(this, new PieceKilledEventArgs(piece, taking));
        }

        ProgressTurn(this, null);
        TurnTimer.Start();
        
        return true;
    }

    public void DeletePiece(string token)
    {
        var position = LocatePieceInstance(token);
        var piece = Pieces[position.PieceColumn, position.PieceRow];
        
        Turns.Remove(piece);
        Pieces[position.PieceColumn, position.PieceRow].Token = "";
    }

    public PieceLocation LocatePieceInstance(string token)
    {
        for (byte x = 0; x < Columns; x++)
        {
            for (byte y = 0; y < Rows; y++)
            {
                var piece = Pieces[x, y];
                if (string.IsNullOrEmpty(piece.Token))
                {
                    continue;
                }
                
                if (piece.Token.Equals(token))
                {
                    return new PieceLocation(x, y);
                }
            }
        }

        return new PieceLocation(-1, -1);
    }

    private bool PieceBlocksHorizontal(PieceLocation start, PieceLocation end)
    {
        // If left of start piece column
        if (end.PieceColumn < start.PieceColumn)
        {
            for (var x = end.PieceColumn; x < start.PieceColumn; x++)
            {
                if (!string.IsNullOrEmpty(Pieces[x, start.PieceRow].Token))
                {
                    return true;
                }
            }

            return false;
        }
        
        for (var x = start.PieceColumn; x < end.PieceColumn; x++)
        {
            if (!string.IsNullOrEmpty(Pieces[x, start.PieceRow].Token))
            {
                return true;
            }

        }

        return false;
    }
    
    private bool PieceBlocksVertical(PieceLocation start, PieceLocation end)
    {
        // If below start piece row
        if (end.PieceRow < start.PieceRow)
        {
            for (var y = end.PieceRow; y < start.PieceRow; y++)
            {
                if (!string.IsNullOrEmpty(Pieces[y, start.PieceRow].Token))
                {
                    return true;
                }
            }

            return false;
        }
        
        for (var y = start.PieceRow; y < end.PieceRow; y++)
        {
            if (!string.IsNullOrEmpty(Pieces[y, start.PieceRow].Token))
            {
                return true;
            }

        }

        return false;
    }

    private bool PieceBlocksDiagonal(PieceLocation start, PieceLocation end)
    {
        var y = 0;

        if (end.PieceColumn > start.PieceColumn)
        {
            for (var x = start.PieceColumn; x < end.PieceColumn; x++)
            {
                if (!string.IsNullOrEmpty(Pieces[x, start.PieceRow + y].Token))
                {
                    return true;
                }

                y = start.PieceRow < end.PieceRow ? y + 1 : y - 1; 
            }

            return false;
        }

        y = 0;
        
        for (var x = end.PieceColumn; x < start.PieceColumn; x++)
        {
            if (!string.IsNullOrEmpty(Pieces[x, start.PieceRow + y].Token))
            {
                return true;
            }

            y = start.PieceRow < end.PieceRow ? y + 1 : y - 1; 
        }

        return false;
    }
}