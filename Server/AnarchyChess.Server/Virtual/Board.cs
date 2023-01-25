using System.Timers;
using AnarchyChess.Server.Events;
using Timer = System.Timers.Timer;

namespace AnarchyChess.Server.Virtual;

// ServerInstance should never have to see this class, only it's direct descendant, Map
public struct Board
{
    public Piece[,] Pieces { get; private set; }
    public List<string> Turns { get; private set; }
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
        Turns = new List<string>();
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
        Turns.Add(piece.Token);
        return true;
    }
    
    public bool TryMovePiece(string token, PieceLocation newLocation)
    {
        var currentLocation = LocatePieceInstance(token);
        var piece = Pieces[currentLocation.PieceColumn, currentLocation.PieceRow];

        if (!Turns[CurrentTurn].Equals(token))
        {
            // CLien tis trying to move while it is not their turn
            return false;
        }
        
        // If we are landing on an occupied space, we are taking that piece
        var taking = Pieces[newLocation.PieceColumn, newLocation.PieceRow];
        if (!string.IsNullOrEmpty(taking.Token) && taking.Colour != piece.Colour)
        {
            PieceKilledEvent.Invoke(this, new PieceKilledEventArgs(piece, taking));
        }
        
        // Move piece to that location
        Pieces[currentLocation.PieceColumn, currentLocation.PieceRow] = Piece.Empty;
        Pieces[newLocation.PieceColumn, newLocation.PieceRow] = piece;

        // Stop running this turn, and go to the next turn if all was valid
        TurnTimer.Stop();
        ProgressTurn(this, null);
        TurnTimer.Start();
        
        return true;
    }

    public void DeletePiece(string token)
    {
        var position = LocatePieceInstance(token);
        
        Turns.Remove(token);
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
}