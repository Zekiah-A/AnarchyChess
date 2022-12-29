using System.Timers;
using AnarchyChess.Server.Events;
using Timer = System.Timers.Timer;

namespace AnarchyChess.Server.Virtual;

public sealed class Board
{
    public Piece?[,] Pieces { get; private set; }
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
    public bool TrySpawnPiece(Piece piece)
    {
        if (Pieces[piece.Column, piece.Row] is not null)
        {
            return false;
        }
        
        Pieces[piece.Column, piece.Row] = piece;
        Turns.Add(piece);
        return true;
    }
    
    public bool TryMovePiece(Piece piece, int toColumn, int toRow)
    {
        TurnTimer.Stop();
        
        if (Turns[CurrentTurn].Equals(piece))
        {
            return false;
        }
        
        var moveColumns = toColumn - piece.Column;
        var moveRows = toColumn - piece.Row;
        
        // Limit every piece from phasing through another except knight/horse and king.
        var valid = false;
        switch (piece.Type)
        {
            case PieceType.Bishop:
                valid = moveColumns is < 8 and > -8 && moveRows is < 8 and > -8 && moveRows == moveColumns;
                
                
                break;
            case PieceType.King:
                // TODO: Add ability for king to be in check
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
                    0 when moveRows is 1 && piece.Colour == PieceColour.White && Pieces[toColumn, toRow] is null => true,
                    0 when moveRows is -1 && piece.Colour == PieceColour.Black && Pieces[toColumn, toRow] is null => true,
                    1 or -1 when moveRows is 1 && piece.Colour == PieceColour.White && Pieces[toColumn, toRow] is not null => true,
                    1 or -1 when moveRows is -1 && piece.Colour == PieceColour.Black && Pieces[toColumn, toRow] is not null => true,
                    _ => valid
                };

                
                break;
            case PieceType.Queen:
                valid = moveColumns switch
                {
                    0 when moveRows is < 8 and > -8 => true,
                    0 when moveRows is < 8 and > -8 => true,
                    < 8 and > -8 when moveRows is < 8 and > -8 && moveRows == moveColumns => true,
                    _ => valid
                };
                
                
                break;
            case PieceType.Rook:
                valid = moveColumns switch
                {
                    0 when moveRows is < 8 and > -8 => true,
                    < 8 and > -8 when moveRows is 0 => true,
                    _ => valid
                };
                
                
                break;
        }
        
        if (!valid)
        {
            return false;
        }
        
        // If we are landing on an occupied space, we are taking that piece
        var taking = Pieces[toColumn, toColumn];
        if (taking is not null)
        {
            PieceKilledEvent.Invoke(this, new PieceKilledEventArgs(piece, taking));
        }

        ProgressTurn(this, null);
        TurnTimer.Start();
        
        return true;
    }

    public void DeletePiece(Piece piece)
    {
        Turns.Remove(piece);
        Pieces[piece.Row, piece.Column] = null;
    }

    public (int PieceColumn, int PieceRow) LocatePieceInstance(string token)
    {
        for (byte x = 0; x < Columns; x++)
        {
            for (byte y = 0; y < Rows; y++)
            {
                var piece = Pieces[x, y];
                if (piece is null)
                {
                    continue;
                }
                
                if (piece.Token.Equals(token))
                {
                    return (x, y);
                }
            }
        }

        return (-1, -1);
    }
}