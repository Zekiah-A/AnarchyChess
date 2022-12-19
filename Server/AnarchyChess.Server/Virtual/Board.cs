using System.Collections;
using System.Threading;
using AnarchyChess.Server.Events;

namespace AnarchyChess.Server.Virtual;

public sealed class Board
{
    public Piece?[,] Pieces { get; private set; }
    public List<Piece> Turns { get; private set; }
    public int CurrentTurn { get; private set; }
    private Timer TurnTimer { get; set; }
    public event EventHandler<TurnChangedEventArgs> TurnChangedEvent = (_, _) => { };
    public event EventHandler<PieceKilledEventArgs> PieceKilledEvent = (_, _) => { };

    public Board(byte columns = 8, byte rows = 8, TimeSpan? period = null)
    {
        period ??= TimeSpan.FromMilliseconds(1000);
        
        Turns = new List<Piece>();
        Pieces = new Piece[columns, rows];
        TurnTimer = new Timer(ProgressTurn, new AutoResetEvent(true), 0, period.Value.Milliseconds);
    }
    
    private void ProgressTurn(object? stateInfo)
    {
        if (CurrentTurn == Turns.Count - 1)
        {
            CurrentTurn = 0;
            return;
        }
        
        CurrentTurn++;

        var currentPosition = Pieces.CoordinatesOf(Turns[CurrentTurn]);
        TurnChangedEvent.Invoke(this, new TurnChangedEventArgs(CurrentTurn, (byte) currentPosition.Row, (byte) currentPosition.Column));
    }
    
    // Spawning piece is instant, but their turn to move is last
    public bool TrySpawnPiece(Piece piece, int row, int column)
    {
        if (Pieces[row, column] is null)
        {
            return false;
        }
        
        Pieces[row, column] = piece;
        Turns.Add(piece);
        return true;
    }
    
    public bool TryMovePiece(Piece piece, int toRow, int toColumn)
    {
        if (Turns[CurrentTurn].Equals(piece))
        {
            return false;
        }
        
        var row = Pieces.CoordinatesOf(piece).Row;
        var column = Pieces.CoordinatesOf(piece).Column;
        var moveRows = toRow - row;
        var moveColumns = toColumn - column;
        
        // Limit every piece from phasing through another except knight/horse and king.
        var valid = false;
        switch (piece.Type)
        {
            case PieceType.Bishop:
                valid = moveRows is < 8 and > -8 && moveColumns is < 8 and > -8 && moveColumns == moveRows;
                
                
                break;
            case PieceType.King:
                valid = moveRows switch
                {
                    1 or -1 when moveColumns is 0 => true,
                    0 when moveColumns is 1 or -1 => true,
                    1 or -1 when moveColumns is 1 or -1 && moveColumns == moveRows => true,
                    _ => valid
                };
                
                // TODO: Add ability for king to be in check
                break;
            case PieceType.Knight:
                valid = moveRows switch
                {
                    0 when moveColumns is < 8 and > -8 => true,
                    < 8 and > -8 when moveColumns is 0 => true,
                    _ => valid
                };
                break;
            case PieceType.Pawn:
                valid = moveRows switch
                {
                    0 when moveColumns is 1 && piece.Colour == PieceColour.White && Pieces[toRow, toColumn] is null => true,
                    0 when moveColumns is -1 && piece.Colour == PieceColour.Black && Pieces[toRow, toColumn] is null => true,
                    1 or -1 when moveColumns is 1 && piece.Colour == PieceColour.White && Pieces[toRow, toColumn] is not null => true,
                    1 or -1 when moveColumns is -1 && piece.Colour == PieceColour.Black && Pieces[toRow, toColumn] is not null => true,
                    _ => valid
                };

                
                break;
            case PieceType.Queen:
                valid = moveRows switch
                {
                    0 when moveColumns is < 8 and > -8 => true,
                    0 when moveColumns is < 8 and > -8 => true,
                    < 8 and > -8 when moveColumns is < 8 and > -8 && moveColumns == moveRows => true,
                    _ => valid
                };
                
                
                break;
            case PieceType.Rook:
                valid = moveRows switch
                {
                    2 or -2 when moveColumns is 1 or -1 => true,
                    1 or -1 when moveColumns is 2 or -2 => true,
                    _ => valid
                };
                
                
                break;
        }
        
        if (!valid)
        {
            return false;
        }
        
        // If we are landing on an occupied space, we are taking that piece
        var taking = Pieces[toRow, toColumn];
        if (taking is not null)
        {
            PieceKilledEvent.Invoke(this, new PieceKilledEventArgs(piece, taking));
        }

        return true;
    }
}