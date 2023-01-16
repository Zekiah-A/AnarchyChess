using AnarchyChess.Server.Virtual;

namespace AnarchyChess.Server.Events;

public class TurnChangedEventArgs : EventArgs
{
    public int Turn { get; }
    public Piece CurrentPiece { get; }

    public TurnChangedEventArgs(int turn, Piece currentPiece)
    {
        Turn = turn;
        CurrentPiece = currentPiece;
    }
    
}