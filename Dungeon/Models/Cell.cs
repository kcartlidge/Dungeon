namespace Dungeon.Models;

/// <summary>
/// Represents a single cell in the dungeon grid, containing information about its type and exits.
/// </summary>
public class Cell
{
    /// <summary>
    /// Gets the X coordinate of the cell in the grid.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the Y coordinate of the cell in the grid.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Gets or sets the type of the cell (Rock, Room, or Corridor).
    /// </summary>
    public CellType Type { get; set; }

    /// <summary>
    /// Gets or sets the state of the exit to the north of this cell.
    /// </summary>
    public DoorState NorthExit { get; set; }

    /// <summary>
    /// Gets or sets the state of the exit to the south of this cell.
    /// </summary>
    public DoorState SouthExit { get; set; }

    /// <summary>
    /// Gets or sets the state of the exit to the east of this cell.
    /// </summary>
    public DoorState EastExit { get; set; }

    /// <summary>
    /// Gets or sets the state of the exit to the west of this cell.
    /// </summary>
    public DoorState WestExit { get; set; }

    /// <summary>
    /// Gets a value indicating whether this cell is part of a room.
    /// </summary>
    public bool InRoom => Type == CellType.Room;

    /// <summary>
    /// Gets a value indicating whether this cell has any exits in any direction.
    /// </summary>
    public bool HasExit => NorthExit != DoorState.None ||
                          SouthExit != DoorState.None ||
                          EastExit != DoorState.None ||
                          WestExit != DoorState.None;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cell"/> class at the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate of the cell.</param>
    /// <param name="y">The Y coordinate of the cell.</param>
    public Cell(int x, int y)
    {
        X = x;
        Y = y;
        Type = CellType.Rock;
        NorthExit = DoorState.None;
        SouthExit = DoorState.None;
        EastExit = DoorState.None;
        WestExit = DoorState.None;
    }
}
