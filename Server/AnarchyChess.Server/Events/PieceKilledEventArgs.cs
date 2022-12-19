using AnarchyChess.Server.Virtual;

namespace AnarchyChess.Server.Events;

public class PieceKilledEventArgs
{
    public Piece Killer { get; }
    public Piece Killed { get; }
    
    public PieceKilledEventArgs(Piece killer, Piece killed)
    {
        Killer = killer;
        Killed = killed;
    }
}