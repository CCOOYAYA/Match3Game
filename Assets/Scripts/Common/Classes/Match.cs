using System.Collections.Generic;

public class Match
{
    public List<GridPosition> MatchingPositions { get; set; }
    public GridPosition CenterPosition { get; set; }
    public Powerup SpawnedPowerup { get; set; }


    public void AddPiece(Piece piece)
    {
        if (piece.CurrentMatch != null)
            return;

        MatchingPositions.Add(piece.GridPosition);
        piece.CurrentMatch = this;
    }
}
