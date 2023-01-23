namespace AnarchyChess.Server.Events;

public class TurnChangedEventArgs : EventArgs
{
    public int Turn { get; }
    public string Token { get; }

    public TurnChangedEventArgs(int turn, string token)
    {
        Turn = turn;
        Token = token;
    }
    
}