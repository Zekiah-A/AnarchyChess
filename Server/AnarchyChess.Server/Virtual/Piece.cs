using WatsonWebsocket;

namespace AnarchyChess.Server.Virtual;

public sealed record Piece(string Token, PieceType Type, PieceColour Colour, Board Board);
// TODO: Holding a board instance in piece mem violates tree direction, and creates circular reference (memory leak).
// TODO: Instead, make each element in the tree know it's column and row in the parent's array.