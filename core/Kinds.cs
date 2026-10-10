namespace CaveCurse.Core;

public enum CellKind
{
    Start,
    Combat,
    Shop,
    Event,
    Gate,
}

public enum Dir
{
    North,
    East,
    South,
    West,
}

public enum HalfMode
{
    Opposite,
    Diagonal,
}
