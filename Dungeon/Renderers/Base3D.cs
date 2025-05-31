using Dungeon.Models;

namespace Dungeon.Renderers;

/// <summary>
/// Provides base functionality for generating 3D primitives from a dungeon grid.
/// </summary>
public class Base3D
{
    private const float CELL_SIZE = 2.0f; // 1 metre per cell
    private const float WALL_HEIGHT = 4.0f; // 4 metres high
    private const float DOOR_HEIGHT = WALL_HEIGHT * 0.9f; // 90% of wall height
    private const float FLOOR_DEPTH = CELL_SIZE / 1.25f; // 1/1.25th of cell width for floor depth (doubled from 1/2.5th)
    private const float DOOR_THICKNESS = 1.2f; // Door thickness (increased by 50%)
    private const float WALL_THICKNESS = 0.015f; // Wall thickness (reduced from 0.025)
    private const float SUBFLOOR_THICKNESS = CELL_SIZE / 2.0f; // Subfloor thickness (customizable)
    private const float ROOF_THICKNESS = SUBFLOOR_THICKNESS; // Roof thickness matches subfloor

    public class Primitive
    {
        public List<(float x, float y, float z)> Vertices { get; } = new();
        public List<(int v1, int v2, int v3, int v4, string material)> Faces { get; } = new();
        public Dictionary<string, (float r, float g, float b)> Materials { get; } = new();
    }

    /// <summary>
    /// Generates 3D primitives from a dungeon grid.
    /// </summary>
    /// <param name="grid">The dungeon grid to convert to primitives.</param>
    /// <param name="config">The configuration settings for rendering.</param>
    /// <returns>A Primitive object containing vertices, faces, and materials.</returns>
    public static Primitive GeneratePrimitives(Grid grid, Config config)
    {
        var primitive = new Primitive();

        // Define materials
        primitive.Materials["room_floor"] = (0.9f * 0.85f, 0.9f * 0.85f, 0.9f * 0.85f);  // Light gray for room floors (darkened by 15%)
        primitive.Materials["corridor_floor"] = (0.6f * 0.85f, 0.6f * 0.85f, 0.6f * 0.85f);  // Medium gray for corridor floors (darkened by 15%)
        primitive.Materials["room_wall"] = (0.7f, 0.7f, 0.7f);  // Medium gray for room walls
        primitive.Materials["corridor_wall"] = (0.8f, 0.8f, 0.8f);  // Light gray for corridor walls
        primitive.Materials["rock"] = (0.2f, 0.2f, 0.2f);  // Very dark gray for rock ceilings
        primitive.Materials["door_wall"] = (0.7f, 0.4f, 0.2f);  // Slightly redder for closed door walls
        primitive.Materials["light_rock"] = (Math.Min(0.2f * 1.75f, 1.0f), Math.Min(0.2f * 1.75f, 1.0f), Math.Min(0.2f * 1.75f, 1.0f));  // 75% lighter rock for inside faces
        primitive.Materials["room_wall"] = (0.9f * 0.8f, 0.9f * 0.8f, 0.9f * 0.8f);  // 20% darker than room floor
        primitive.Materials["corridor_wall"] = (0.6f * 0.8f, 0.6f * 0.8f, 0.6f * 0.8f);  // 20% darker than corridor floor
        primitive.Materials["outer_rock"] = (Math.Min(0.2f * 1.75f, 1.0f), Math.Min(0.2f * 1.75f, 1.0f), Math.Min(0.2f * 1.75f, 1.0f));  // 75% brighter than rock for outside faces
        primitive.Materials["roof"] = (0.9f * 0.85f, 0.9f * 0.85f, 0.9f * 0.85f);  // Same as room floor

        // Find all rooms and their bounds
        var rooms = new List<(float minX, float minZ, float maxX, float maxZ, List<(int x, int y)> cells)>();
        var visited = new HashSet<(int x, int y)>();

        foreach (var cell in grid.GetAllCells())
        {
            if (cell.Type == CellType.Room && !visited.Contains((cell.X, cell.Y)))
            {
                // Find room bounds
                float roomMinX = cell.X * CELL_SIZE;
                float roomMinZ = cell.Y * CELL_SIZE;
                float roomMaxX = roomMinX;
                float roomMaxZ = roomMinZ;
                var roomCells = new List<(int x, int y)>();

                // Flood fill to find room extent
                var queue = new Queue<(int x, int y)>();
                queue.Enqueue((cell.X, cell.Y));
                visited.Add((cell.X, cell.Y));
                roomCells.Add((cell.X, cell.Y));

                while (queue.Count > 0)
                {
                    var (x, y) = queue.Dequeue();
                    var currentCell = grid.GetCell(x, y);
                    if (currentCell?.Type != CellType.Room) continue;

                    roomMaxX = Math.Max(roomMaxX, (x + 1) * CELL_SIZE);
                    roomMaxZ = Math.Max(roomMaxZ, (y + 1) * CELL_SIZE);

                    // Check adjacent cells
                    if (x > 0 && !visited.Contains((x - 1, y))) { queue.Enqueue((x - 1, y)); visited.Add((x - 1, y)); roomCells.Add((x - 1, y)); }
                    if (x < grid.Width - 1 && !visited.Contains((x + 1, y))) { queue.Enqueue((x + 1, y)); visited.Add((x + 1, y)); roomCells.Add((x + 1, y)); }
                    if (y > 0 && !visited.Contains((x, y - 1))) { queue.Enqueue((x, y - 1)); visited.Add((x, y - 1)); roomCells.Add((x, y - 1)); }
                    if (y < grid.Height - 1 && !visited.Contains((x, y + 1))) { queue.Enqueue((x, y + 1)); visited.Add((x, y + 1)); roomCells.Add((x, y + 1)); }
                }

                rooms.Add((roomMinX, roomMinZ, roomMaxX, roomMaxZ, roomCells));
            }
        }

        // Create solid geometry for each room
        foreach (var (roomMinX, roomMinZ, roomMaxX, roomMaxZ, roomCells) in rooms)
        {
            // Create floor box
            CreateBox(
                roomMinX, -FLOOR_DEPTH, roomMinZ,
                roomMaxX, 0, roomMaxZ,
                "room_floor",
                primitive.Vertices,
                primitive.Faces);

            // Restore only door rendering logic
            for (int x = (int)(roomMinX / CELL_SIZE); x < (int)(roomMaxX / CELL_SIZE); x++)
            {
                for (int y = (int)(roomMinZ / CELL_SIZE); y < (int)(roomMaxZ / CELL_SIZE); y++)
                {
                    var cell = grid.GetCell(x, y);
                    if (cell == null) continue;

                    // North door
                    if (y == (int)(roomMinZ / CELL_SIZE) && grid.GetCell(x, y - 1)?.Type == CellType.Corridor)
                    {
                        var doorCell = grid.GetCell(x, y - 1);
                        if (doorCell?.SouthExit == DoorState.Closed)
                        {
                            CreateBox(
                                x * CELL_SIZE + WALL_THICKNESS, 0, roomMinZ - DOOR_THICKNESS,
                                (x + 1) * CELL_SIZE - WALL_THICKNESS, DOOR_HEIGHT, roomMinZ,
                                "door_wall",
                                primitive.Vertices,
                                primitive.Faces);
                        }
                    }
                    // South door
                    if (y == (int)(roomMaxZ / CELL_SIZE) - 1 && grid.GetCell(x, y + 1)?.Type == CellType.Corridor)
                    {
                        var doorCell = grid.GetCell(x, y + 1);
                        if (doorCell?.NorthExit == DoorState.Closed)
                        {
                            CreateBox(
                                x * CELL_SIZE + WALL_THICKNESS, 0, roomMaxZ,
                                (x + 1) * CELL_SIZE - WALL_THICKNESS, DOOR_HEIGHT, roomMaxZ + DOOR_THICKNESS,
                                "door_wall",
                                primitive.Vertices,
                                primitive.Faces);
                        }
                    }
                    // East door
                    if (x == (int)(roomMaxX / CELL_SIZE) - 1 && grid.GetCell(x + 1, y)?.Type == CellType.Corridor)
                    {
                        var doorCell = grid.GetCell(x + 1, y);
                        if (doorCell?.WestExit == DoorState.Closed)
                        {
                            CreateBox(
                                roomMaxX, 0, y * CELL_SIZE + WALL_THICKNESS,
                                roomMaxX + DOOR_THICKNESS, DOOR_HEIGHT, (y + 1) * CELL_SIZE - WALL_THICKNESS,
                                "door_wall",
                                primitive.Vertices,
                                primitive.Faces);
                        }
                    }
                    // West door
                    if (x == (int)(roomMinX / CELL_SIZE) && grid.GetCell(x - 1, y)?.Type == CellType.Corridor)
                    {
                        var doorCell = grid.GetCell(x - 1, y);
                        if (doorCell?.EastExit == DoorState.Closed)
                        {
                            CreateBox(
                                roomMinX - DOOR_THICKNESS, 0, y * CELL_SIZE + WALL_THICKNESS,
                                roomMinX, DOOR_HEIGHT, (y + 1) * CELL_SIZE - WALL_THICKNESS,
                                "door_wall",
                                primitive.Vertices,
                                primitive.Faces);
                        }
                    }
                }
            }
        }

        // Add floors for corridor cells
        foreach (var cell in grid.GetAllCells())
        {
            if (cell.Type == CellType.Corridor)
            {
                float minX = cell.X * CELL_SIZE;
                float minZ = cell.Y * CELL_SIZE;
                float maxX = (cell.X + 1) * CELL_SIZE;
                float maxZ = (cell.Y + 1) * CELL_SIZE;

                // Create corridor floor box
                CreateBox(
                    minX, -FLOOR_DEPTH, minZ,
                    maxX, 0, maxZ,
                    "corridor_floor",
                    primitive.Vertices,
                    primitive.Faces);
            }
            else if (cell.Type != CellType.Room && cell.Type != CellType.Corridor) // Draw rock cells as solid blocks
            {
                float minX = cell.X * CELL_SIZE;
                float minY = -FLOOR_DEPTH;
                float minZ = cell.Y * CELL_SIZE;
                float maxX = (cell.X + 1) * CELL_SIZE;
                float maxY = WALL_HEIGHT;
                float maxZ = (cell.Y + 1) * CELL_SIZE;

                // Check neighbors for inside and outside faces
                string westMat = "rock";
                var westCell = grid.GetCell(cell.X - 1, cell.Y);
                if (westCell == null) westMat = "outer_rock";
                else if (westCell.Type == CellType.Room) westMat = "room_wall";
                else if (westCell.Type == CellType.Corridor) westMat = "corridor_wall";

                string eastMat = "rock";
                var eastCell = grid.GetCell(cell.X + 1, cell.Y);
                if (eastCell == null) eastMat = "outer_rock";
                else if (eastCell.Type == CellType.Room) eastMat = "room_wall";
                else if (eastCell.Type == CellType.Corridor) eastMat = "corridor_wall";

                string northMat = "rock";
                var northCell = grid.GetCell(cell.X, cell.Y - 1);
                if (northCell == null) northMat = "outer_rock";
                else if (northCell.Type == CellType.Room) northMat = "room_wall";
                else if (northCell.Type == CellType.Corridor) northMat = "corridor_wall";

                string southMat = "rock";
                var southCell = grid.GetCell(cell.X, cell.Y + 1);
                if (southCell == null) southMat = "outer_rock";
                else if (southCell.Type == CellType.Room) southMat = "room_wall";
                else if (southCell.Type == CellType.Corridor) southMat = "corridor_wall";

                int baseIndex = primitive.Vertices.Count;
                // Add vertices
                primitive.Vertices.Add((minX, minY, minZ)); // 0
                primitive.Vertices.Add((maxX, minY, minZ)); // 1
                primitive.Vertices.Add((maxX, minY, maxZ)); // 2
                primitive.Vertices.Add((minX, minY, maxZ)); // 3
                primitive.Vertices.Add((minX, maxY, minZ)); // 4
                primitive.Vertices.Add((maxX, maxY, minZ)); // 5
                primitive.Vertices.Add((maxX, maxY, maxZ)); // 6
                primitive.Vertices.Add((minX, maxY, maxZ)); // 7
                // Add faces (bottom, top, north, east, south, west)
                primitive.Faces.Add((baseIndex + 1, baseIndex + 2, baseIndex + 3, baseIndex + 4, "rock")); // Bottom
                primitive.Faces.Add((baseIndex + 5, baseIndex + 6, baseIndex + 7, baseIndex + 8, "rock")); // Top
                primitive.Faces.Add((baseIndex + 1, baseIndex + 5, baseIndex + 6, baseIndex + 2, northMat)); // North
                primitive.Faces.Add((baseIndex + 2, baseIndex + 6, baseIndex + 7, baseIndex + 3, eastMat)); // East
                primitive.Faces.Add((baseIndex + 3, baseIndex + 7, baseIndex + 8, baseIndex + 4, southMat)); // South
                primitive.Faces.Add((baseIndex + 4, baseIndex + 8, baseIndex + 5, baseIndex + 1, westMat)); // West
            }
        }

        float dungeonWidth = grid.Width * CELL_SIZE;
        float dungeonHeight = grid.Height * CELL_SIZE;

        // Optionally add a single subfloor box under the entire dungeon
        if (config.HasExtraFloor)
        {
        CreateBox(
            0, -FLOOR_DEPTH - SUBFLOOR_THICKNESS, 0,
            dungeonWidth, -FLOOR_DEPTH, dungeonHeight,
            "room_floor",
            primitive.Vertices,
            primitive.Faces);
        }

        // Optionally add a roof
        if (config.HasRoof)
        {
            CreateBox(
                0, WALL_HEIGHT, 0,
                dungeonWidth, WALL_HEIGHT + ROOF_THICKNESS, dungeonHeight,
                "roof",
                primitive.Vertices,
                primitive.Faces);
        }

        // Calculate translation to center the dungeon
        float centerX = -(grid.Width * CELL_SIZE) / 2;
        float centerZ = -(grid.Height * CELL_SIZE) / 2;

        // Translate all vertices to center the dungeon
        for (int i = 0; i < primitive.Vertices.Count; i++)
        {
            var (x, y, z) = primitive.Vertices[i];
            primitive.Vertices[i] = (x + centerX, y, z + centerZ);
        }

        return primitive;
    }

    /// <summary>
    /// Creates a box with the given dimensions and material, adding vertices and faces to the provided lists.
    /// </summary>
    private static int CreateBox(
        float minX, float minY, float minZ,
        float maxX, float maxY, float maxZ,
        string material,
        List<(float x, float y, float z)> vertices,
        List<(int v1, int v2, int v3, int v4, string material)> faces)
    {
        int baseIndex = vertices.Count;

        // Add vertices
        vertices.Add((minX, minY, minZ)); // 0
        vertices.Add((maxX, minY, minZ)); // 1
        vertices.Add((maxX, minY, maxZ)); // 2
        vertices.Add((minX, minY, maxZ)); // 3
        vertices.Add((minX, maxY, minZ)); // 4
        vertices.Add((maxX, maxY, minZ)); // 5
        vertices.Add((maxX, maxY, maxZ)); // 6
        vertices.Add((minX, maxY, maxZ)); // 7

        // Add faces
        faces.Add((baseIndex + 1, baseIndex + 2, baseIndex + 3, baseIndex + 4, material)); // Bottom
        faces.Add((baseIndex + 5, baseIndex + 6, baseIndex + 7, baseIndex + 8, material)); // Top
        faces.Add((baseIndex + 1, baseIndex + 5, baseIndex + 6, baseIndex + 2, material)); // North
        faces.Add((baseIndex + 2, baseIndex + 6, baseIndex + 7, baseIndex + 3, material)); // East
        faces.Add((baseIndex + 3, baseIndex + 7, baseIndex + 8, baseIndex + 4, material)); // South
        faces.Add((baseIndex + 4, baseIndex + 8, baseIndex + 5, baseIndex + 1, material)); // West

        return baseIndex;
    }
}
