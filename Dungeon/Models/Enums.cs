namespace Dungeon.Models;

/// <summary>
/// Represents the type of a cell in the dungeon grid.
/// </summary>
public enum CellType
{
    /// <summary>
    /// Solid rock that cannot be traversed.
    /// </summary>
    Rock,

    /// <summary>
    /// A room cell that can be traversed.
    /// </summary>
    Room,

    /// <summary>
    /// A corridor cell that can be traversed.
    /// </summary>
    Corridor
}

/// <summary>
/// Represents the state of an exit or door in a cell.
/// </summary>
public enum DoorState
{
    /// <summary>
    /// No exit or door exists in this direction.
    /// </summary>
    None,

    /// <summary>
    /// An open exit or door exists in this direction.
    /// </summary>
    Open,

    /// <summary>
    /// A closed door exists in this direction.
    /// </summary>
    Closed
}
