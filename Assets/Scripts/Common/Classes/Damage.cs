using System;
using System.Collections.Generic;


public class Damage
{
    public Guid SourceGuid { get; private set; }
    public List<GridPosition> DamagePositions { get; private set; }
    public HashSet<GridPosition> IgnorePositions { get; private set; }


    public Damage()
    {
        SourceGuid = Guid.NewGuid();
        DamagePositions = new ();
        IgnorePositions = new ();
    }

    public void AddToDamagePositions(GridPosition gridPosition)
    {
        if (!DamagePositions.Contains(gridPosition))
            DamagePositions.Add(gridPosition);
    }

    public void AddToIgnorePositions(GridPosition ignoreDamagePosition)
    {
        IgnorePositions.Add(ignoreDamagePosition);
    }

    public void AddToIgnorePositions(IEnumerable<GridPosition> ignoreDamagePositions)
    {
        foreach (var pos in ignoreDamagePositions)
        {
            IgnorePositions.Add(pos);
        }
    }
}
