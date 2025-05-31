using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Dungeon.Models;

/// <summary>
/// Represents a 2D grid of cells that makes up the dungeon.
/// </summary>
public class Grid
{
    private readonly Cell[,] _cells;
    private readonly List<Room> _rooms = new();

    /// <summary>
    /// Gets the width of the grid.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the grid.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the list of rooms in the dungeon.
    /// </summary>
    public IReadOnlyList<Room> Rooms => _rooms;

    /// <summary>
    /// Gets a list of all cells in the grid.
    /// </summary>
    public IReadOnlyList<Cell> Cells => GetAllCells().ToList();

    /// <summary>
    /// Initializes a new instance of the <see cref="Grid"/> class with the specified dimensions.
    /// </summary>
    /// <param name="width">The width of the grid.</param>
    /// <param name="height">The height of the grid.</param>
    public Grid(int width, int height)
    {
        Width = width;
        Height = height;
        _cells = new Cell[width, height];

        // Initialize all cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _cells[x, y] = new Cell(x, y);
            }
        }
    }

    /// <summary>
    /// Gets the cell at the specified coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>The cell at the specified coordinates, or null if the coordinates are out of bounds.</returns>
    public Cell GetCell(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;

        return _cells[x, y];
    }

    /// <summary>
    /// Gets an enumerable collection of all cells in the grid.
    /// </summary>
    /// <returns>An enumerable collection of all cells, traversing the grid from left to right, top to bottom.</returns>
    public IEnumerable<Cell> GetAllCells()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                yield return _cells[x, y];
            }
        }
    }

    /// <summary>
    /// Adds a room to the grid.
    /// </summary>
    /// <param name="room">The room to add.</param>
    internal void AddRoom(Room room)
    {
        _rooms.Add(room);
    }

    /// <summary>
    /// Updates the list of rooms in the dungeon.
    /// </summary>
    /// <param name="newRooms">The new list of rooms to replace the existing ones.</param>
    internal void UpdateRooms(List<Room> newRooms)
    {
        _rooms.Clear();
        _rooms.AddRange(newRooms);
    }
}
