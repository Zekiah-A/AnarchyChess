using WatsonWebsocket;

namespace AnarchyChess.Server.Virtual;

public sealed record Piece
(
    string Token,
    PieceType Type,
    PieceColour Colour,
    
    // TODO: We ditch these properties, and move fully to piece locator model
    int BoardColumn,
    int BoardRow,
    int Column,
    int Row
);