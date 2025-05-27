namespace Dungeon.Models;

/// <summary>
/// Represents a rectangular room in the dungeon.
/// </summary>
public class Room
{
    /// <summary>
    /// Gets the room number, starting from 1.
    /// </summary>
    public int RoomNumber { get; }

    /// <summary>
    /// Gets the X coordinate of the room's top-left corner.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Gets the Y coordinate of the room's top-left corner.
    /// </summary>
    public int Y { get; }

    /// <summary>
    /// Gets the width of the room.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the room.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Room"/> class.
    /// </summary>
    /// <param name="roomNumber">The room number, starting from 1.</param>
    /// <param name="x">The X coordinate of the room's top-left corner.</param>
    /// <param name="y">The Y coordinate of the room's top-left corner.</param>
    /// <param name="width">The width of the room.</param>
    /// <param name="height">The height of the room.</param>
    public Room(int roomNumber, int x, int y, int width, int height)
    {
        RoomNumber = roomNumber;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Checks if this room overlaps with another room.
    /// </summary>
    /// <param name="other">The other room to check for overlap.</param>
    /// <returns>True if the rooms overlap, false otherwise.</returns>
    public bool Overlaps(Room other)
    {
        return X < other.X + other.Width &&
               X + Width > other.X &&
               Y < other.Y + other.Height &&
               Y + Height > other.Y;
    }
}
