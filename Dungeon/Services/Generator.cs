using System;
using System.Collections.Generic;
using Dungeon.Models;
using System.Linq;

namespace Dungeon.Services;

/// <summary>
/// Service responsible for generating dungeon layouts.
/// </summary>
public static class Generator
{
    private static Random _random = new();

    /// <summary>
    /// Generates a new dungeon grid with the specified configuration.
    /// </summary>
    /// <param name="config">The configuration settings for dungeon generation.</param>
    /// <returns>A new grid initialized with the specified dimensions.</returns>
    public static Grid Generate(Config config)
    {
        _random = new Random(config.Seed);
        var grid = new Grid(config.Width, config.Height);
        AddRandomRooms(grid, 1);
        SetRoomCells(grid);
        RenumberRoomsInZigZagPattern(grid);
        return grid;
    }

    private static void AddRandomRooms(Grid grid, int startRoomNumber)
    {
        const int roomMargin = 1;
        const int minSize = 3;
        const int maxSize = 8;
        const int maxAttempts = 50;
        int failedAttempts = 0;
        int roomNumber = startRoomNumber;

        while (failedAttempts < maxAttempts)
        {
            // Generate random room dimensions
            int width = _random.Next(minSize, maxSize);
            int height = _random.Next(minSize, maxSize);

            // Try to place the room, accounting for margin from grid edges
            int x = _random.Next(roomMargin, grid.Width - width - roomMargin);
            int y = _random.Next(roomMargin, grid.Height - height - roomMargin);

            // Create a room with margin for overlap checking
            var roomWithMargin = new Room(
                roomNumber,
                x - roomMargin,
                y - roomMargin,
                width + (2 * roomMargin),
                height + (2 * roomMargin)
            );

            // Check if the room overlaps with any existing rooms
            bool overlaps = false;
            foreach (var existingRoom in grid.Rooms)
            {
                if (roomWithMargin.Overlaps(existingRoom))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                // Add the room without the margin
                var actualRoom = new Room(roomNumber, x, y, width, height);
                grid.AddRoom(actualRoom);
                roomNumber++;
                failedAttempts = 0;
            }
            else
            {
                failedAttempts++;
            }
        }
    }

    private static void SetRoomCells(Grid grid)
    {
        // First, set all cells that are currently Room type back to Rock
        foreach (var cell in grid.GetAllCells())
        {
            if (cell.Type == CellType.Room)
                cell.Type = CellType.Rock;
        }

        // Then, set all cells within room areas to Room type
        foreach (var room in grid.Rooms)
        {
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    var cell = grid.GetCell(x, y);
                    if (cell != null)
                        cell.Type = CellType.Room;
                }
            }
        }
    }

    /// <summary>
    /// Renumbers rooms in a zig-zag pattern for intuitive navigation.
    /// Rooms are grouped into columns based on X-coordinate proximity,
    /// then numbered in alternating up/down pattern across columns.
    /// </summary>
    private static void RenumberRoomsInZigZagPattern(Grid grid)
    {
        const int columnWidth = 5; // Width of a column in grid units
        var rooms = grid.Rooms.ToList();

        // Group rooms into columns based on X-coordinate
        var columns = rooms
            .GroupBy(room => room.X / columnWidth)
            .OrderBy(group => group.Key)
            .ToList();

        int roomNumber = 1;
        bool ascending = true;

        // Process each column
        foreach (var column in columns)
        {
            // Sort rooms in column by Y-coordinate
            var roomsInColumn = column
                .OrderBy(room => ascending ? room.Y : -room.Y)
                .ToList();

            // Assign new room numbers
            foreach (var room in roomsInColumn)
            {
                // Create new room with updated number
                var renumberedRoom = new Room(
                    roomNumber,
                    room.X,
                    room.Y,
                    room.Width,
                    room.Height
                );

                // Replace old room with renumbered room
                var index = rooms.IndexOf(room);
                rooms[index] = renumberedRoom;
                roomNumber++;
            }

            // Toggle direction for next column
            ascending = !ascending;
        }

        // Update grid with renumbered rooms
        grid.UpdateRooms(rooms);
    }
}
