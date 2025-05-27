using System.Collections.Generic;

namespace Dungeon.Models;

/// <summary>
/// Represents a 2D grid of cells that makes up the dungeon.
/// </summary>
public class Grid
{
    private readonly Cell[,] _cells;

    /// <summary>
    /// Gets the width of the grid.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the grid.
    /// </summary>
    public int Height { get; }

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
}
