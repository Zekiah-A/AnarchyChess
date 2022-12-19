using AnarchyChess.Server.Virtual;

namespace AnarchyChess.Server.Events;

public class TurnChangedEventArgs : EventArgs
{
    public int Turn { get; }
    public byte CurrentsRow { get; }
    public byte CurrentColumn { get; }
    
    public TurnChangedEventArgs(int turn, byte currentsRow, byte currentColumn)
    {
        Turn = turn;
        CurrentsRow = currentsRow;
        CurrentColumn = currentColumn;
    }
    
}