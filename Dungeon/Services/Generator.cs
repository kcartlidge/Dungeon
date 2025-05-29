using Dungeon.Models;

namespace Dungeon.Services;

/// <summary>
/// Service responsible for generating dungeon layouts.
/// </summary>
public static class Generator
{
    private static Random _random = new();
    private static int _currentRegion = -1;
    private const int WindingPercent = 30; // Chance to continue in same direction
    private const int ExtraConnectorChance = 20; // 1 in 20 chance for extra connectors

    // Define direction tuples with proper names
    private static readonly (int dx, int dy)[] Directions = new[]
    {
        (0, -1),  // North
        (1, 0),   // East
        (0, 1),   // South
        (-1, 0)   // West
    };

    /// <summary>
    /// Generates a new dungeon grid with the specified configuration.
    /// </summary>
    /// <param name="config">The configuration settings for dungeon generation.</param>
    /// <returns>A new grid initialized with the specified dimensions.</returns>
    public static Grid Generate(Config config)
    {
        _random = new Random(config.Seed);
        var grid = new Grid(config.Width, config.Height);

        // Ensure odd dimensions for proper maze generation
        if (grid.Width % 2 == 0 || grid.Height % 2 == 0)
        {
            throw new ArgumentException("Dungeon dimensions must be odd");
        }

        AddRooms(grid, config.NumRoomTries);
        FillWithMazes(grid);

        EnsureRoomConnectivity(grid);
        ConnectRegions(grid);
        RemoveDeadEnds(grid);

        EliminateRedundantEntrances(grid);
        RemoveDeadEnds(grid);

        EliminateMultipleEntrancesFromSameCorridor(grid);
        RemoveDeadEnds(grid);

        AssignDoorsToRoomExits(grid);
        RenumberRooms(grid);

        return grid;
    }

    private static void AddRooms(Grid grid, int numRoomTries)
    {
        const int roomExtraSize = 1;
        int roomNumber = 1;

        for (int i = 0; i < numRoomTries; i++)
        {
            // Generate random room size with rectangularity
            int size = (_random.Next(2 + roomExtraSize) + 1) * 2 + 1;
            int rectangularity = _random.Next(1 + size / 2) * 2;
            int width = size;
            int height = size;

            if (_random.Next(2) == 0)
            {
                width += rectangularity;
            }
            else
            {
                height += rectangularity;
            }

            // Ensure odd dimensions and proper alignment
            int x = _random.Next((grid.Width - width) / 2) * 2 + 1;
            int y = _random.Next((grid.Height - height) / 2) * 2 + 1;

            var room = new Room(roomNumber, x, y, width, height);

            // Check for room overlaps
            bool overlaps = false;
            foreach (var existingRoom in grid.Rooms)
            {
                if (room.Overlaps(existingRoom))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                grid.AddRoom(room);
                StartRegion();
                for (int ry = room.Y; ry < room.Y + room.Height; ry++)
                {
                    for (int rx = room.X; rx < room.X + room.Width; rx++)
                    {
                        var cell = grid.GetCell(rx, ry);
                        if (cell != null)
                        {
                            cell.Type = CellType.Room;
                            cell.Region = _currentRegion;
                        }
                    }
                }
                roomNumber++;
            }
        }
    }

    private static void FillWithMazes(Grid grid)
    {
        // Fill remaining space with mazes
        for (int y = 1; y < grid.Height; y += 2)
        {
            for (int x = 1; x < grid.Width; x += 2)
            {
                var cell = grid.GetCell(x, y);
                if (cell != null && cell.Type == CellType.Rock)
                {
                    GrowMaze(grid, x, y);
                }
            }
        }
    }

    private static void GrowMaze(Grid grid, int startX, int startY)
    {
        var cells = new Stack<(int x, int y)>();
        cells.Push((startX, startY));
        StartRegion();
        Carve(grid, startX, startY);

        var lastDir = (dx: 0, dy: 0);
        while (cells.Count > 0)
        {
            var (x, y) = cells.Peek();
            var unmadeCells = new List<(int dx, int dy)>();

            foreach (var dir in Directions)
            {
                if (CanCarve(grid, x, y, dir.dx, dir.dy))
                {
                    unmadeCells.Add(dir);
                }
            }

            if (unmadeCells.Count > 0)
            {
                var dir = unmadeCells[0];
                if (CanUseLastDir(unmadeCells, lastDir))
                {
                    dir = lastDir;
                }
                else
                {
                    dir = unmadeCells[_random.Next(unmadeCells.Count)];
                }

                var pos1 = (x: x + dir.dx, y: y + dir.dy);
                var pos2 = (x: pos1.x + dir.dx, y: pos1.y + dir.dy);

                Carve(grid, pos1.x, pos1.y);
                Carve(grid, pos2.x, pos2.y);

                cells.Push(pos2);
                lastDir = dir;
            }
            else
            {
                cells.Pop();
                lastDir = (dx: 0, dy: 0);
            }
        }
    }

    private static bool CanUseLastDir(List<(int dx, int dy)> unmadeCells, (int dx, int dy) lastDir)
    {
        if (lastDir.dx == 0 && lastDir.dy == 0) return false;
        return unmadeCells.Contains(lastDir) && _random.Next(100) > WindingPercent;
    }

    private static bool CanCarve(Grid grid, int x, int y, int dx, int dy)
    {
        int nx = x + dx * 2;
        int ny = y + dy * 2;
        if (nx < 0 || nx >= grid.Width || ny < 0 || ny >= grid.Height)
            return false;
        var nextCell = grid.GetCell(nx, ny);
        return nextCell != null && nextCell.Type == CellType.Rock;
    }

    private static void StartRegion()
    {
        _currentRegion++;
    }

    private static void Carve(Grid grid, int x, int y)
    {
        var cell = grid.GetCell(x, y);
        if (cell != null)
        {
            cell.Type = CellType.Corridor;
            cell.Region = _currentRegion;
        }
    }

    private static void ConnectRegions(Grid grid)
    {
        var connectors = new List<(int x, int y, HashSet<int> regions)>();

        // Find all possible connectors
        for (int y = 1; y < grid.Height - 1; y++)
        {
            for (int x = 1; x < grid.Width - 1; x++)
            {
                var cell = grid.GetCell(x, y);
                if (cell != null && cell.Type == CellType.Rock)
                {
                    var regions = new HashSet<int>();
                    foreach (var dir in Directions)
                    {
                        var neighbor = grid.GetCell(x + dir.dx, y + dir.dy);
                        if (neighbor != null && neighbor.Region != -1)
                        {
                            regions.Add(neighbor.Region);
                        }
                    }

                    if (regions.Count >= 2)
                    {
                        connectors.Add((x, y, regions));
                    }
                }
            }
        }

        // Connect regions
        var merged = new int[_currentRegion + 1];
        var openRegions = new HashSet<int>();
        for (int i = 0; i <= _currentRegion; i++)
        {
            merged[i] = i;
            openRegions.Add(i);
        }

        while (openRegions.Count > 1)
        {
            if (connectors.Count == 0)
                break;

            // Pick a random connector
            int index = _random.Next(connectors.Count);
            var (x, y, regions) = connectors[index];
            connectors.RemoveAt(index);

            // Add the connection
            var cell = grid.GetCell(x, y);
            if (cell != null)
            {
                cell.Type = CellType.Corridor;
                // Merge the regions
                var regionsList = regions.Select(r => merged[r]).Distinct().ToList();
                int dest = regionsList[0];
                var sources = regionsList.Skip(1).ToList();
                for (int i = 0; i <= _currentRegion; i++)
                {
                    if (sources.Contains(merged[i]))
                    {
                        merged[i] = dest;
                    }
                }
                foreach (var s in sources)
                    openRegions.Remove(s);
                cell.Region = dest;
            }

            // Remove or update connectors
            var newConnectors = new List<(int x, int y, HashSet<int> regions)>();
            foreach (var c in connectors)
            {
                var newRegions = new HashSet<int>(c.regions.Select(r => merged[r]));
                if (newRegions.Count > 1)
                {
                    newConnectors.Add((c.x, c.y, newRegions));
                }
                else if (_random.Next(ExtraConnectorChance) == 0)
                {
                    // Optionally add extra connector for loops
                    var extraCell = grid.GetCell(c.x, c.y);
                    if (extraCell != null)
                    {
                        extraCell.Type = CellType.Corridor;
                        extraCell.Region = newRegions.First();
                    }
                }
            }
            connectors = newConnectors;
        }
    }

    private static void EliminateRedundantEntrances(Grid grid)
    {
        foreach (var room in grid.Rooms)
        {
            // Each wall: (isVertical, x, y, length)
            var walls = new[]
            {
                (isVertical: false, x: room.X, y: room.Y - 1, length: room.Width), // North
                (isVertical: false, x: room.X, y: room.Y + room.Height, length: room.Width), // South
                (isVertical: true, x: room.X - 1, y: room.Y, length: room.Height), // West
                (isVertical: true, x: room.X + room.Width, y: room.Y, length: room.Height) // East
            };

            foreach (var wall in walls)
            {
                var entrances = new List<(int x, int y)>();
                if (wall.isVertical)
                {
                    for (int y = wall.y; y < wall.y + wall.length; y++)
                    {
                        var cell = grid.GetCell(wall.x, y);
                        if (cell != null && cell.Type != CellType.Rock)
                        {
                            entrances.Add((wall.x, y));
                        }
                    }
                }
                else
                {
                    for (int x = wall.x; x < wall.x + wall.length; x++)
                    {
                        var cell = grid.GetCell(x, wall.y);
                        if (cell != null && cell.Type != CellType.Rock)
                        {
                            entrances.Add((x, wall.y));
                        }
                    }
                }

                if (entrances.Count > 1)
                {
                    // Always keep at least one entrance
                    int keepIndex = _random.Next(entrances.Count);
                    for (int i = 0; i < entrances.Count; i++)
                    {
                        if (i == keepIndex) continue;
                        var (ex, ey) = entrances[i];
                        var cell = grid.GetCell(ex, ey);
                        if (cell == null) continue;

                        // Temporarily remove the entrance
                        var originalType = cell.Type;
                        var originalRegion = cell.Region;
                        cell.Type = CellType.Rock;
                        cell.Region = -1;

                        // Check if the room is still connected to a corridor
                        bool stillConnected = false;
                        var visited = new HashSet<(int x, int y)>();
                        var stack = new Stack<(int x, int y)>();
                        stack.Push((room.X, room.Y));
                        while (stack.Count > 0)
                        {
                            var (cx, cy) = stack.Pop();
                            if (!visited.Add((cx, cy))) continue;
                            var ccell = grid.GetCell(cx, cy);
                            if (ccell == null) continue;
                            if (ccell.Type == CellType.Corridor)
                            {
                                stillConnected = true;
                                break;
                            }
                            if (ccell.Type != CellType.Room) continue;
                            foreach (var dir in Directions)
                            {
                                int nx = cx + dir.dx;
                                int ny = cy + dir.dy;
                                if (nx < 0 || nx >= grid.Width || ny < 0 || ny >= grid.Height) continue;
                                stack.Push((nx, ny));
                            }
                        }

                        // If not connected, restore the entrance
                        if (!stillConnected)
                        {
                            cell.Type = originalType;
                            cell.Region = originalRegion;
                        }
                    }
                }
            }
        }
    }

    private static void EliminateMultipleEntrancesFromSameCorridor(Grid grid)
    {
        foreach (var room in grid.Rooms)
        {
            // Collect all perimeter tiles
            var perimeter = new List<(int x, int y)>();
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                perimeter.Add((x, room.Y - 1)); // North
                perimeter.Add((x, room.Y + room.Height)); // South
            }
            for (int y = room.Y; y < room.Y + room.Height; y++)
            {
                perimeter.Add((room.X - 1, y)); // West
                perimeter.Add((room.X + room.Width, y)); // East
            }

            // Find all entrances: (x, y, corridorRegion)
            var entrances = new List<(int x, int y, int corridorRegion)>();
            foreach (var (x, y) in perimeter)
            {
                var cell = grid.GetCell(x, y);
                if (cell != null && cell.Type != CellType.Rock)
                {
                    foreach (var dir in Directions)
                    {
                        var neighbor = grid.GetCell(x + dir.dx, y + dir.dy);
                        if (neighbor != null && neighbor.Type == CellType.Corridor)
                        {
                            entrances.Add((x, y, neighbor.Region));
                            break;
                        }
                    }
                }
            }

            // Group entrances by corridor region
            var grouped = entrances.GroupBy(e => e.corridorRegion);
            foreach (var group in grouped)
            {
                var groupList = group.ToList();
                if (groupList.Count > 1)
                {
                    // Always keep at least one entrance per corridor region
                    int keepIndex = _random.Next(groupList.Count);
                    for (int i = 0; i < groupList.Count; i++)
                    {
                        if (i == keepIndex) continue;
                        var (ex, ey, _) = groupList[i];
                        var cell = grid.GetCell(ex, ey);
                        if (cell == null) continue;

                        // Temporarily remove the entrance
                        var originalType = cell.Type;
                        var originalRegion = cell.Region;
                        cell.Type = CellType.Rock;
                        cell.Region = -1;

                        // Check if the room is still connected to a corridor
                        bool stillConnected = false;
                        var visited = new HashSet<(int x, int y)>();
                        var stack = new Stack<(int x, int y)>();
                        stack.Push((room.X, room.Y));
                        while (stack.Count > 0)
                        {
                            var (cx, cy) = stack.Pop();
                            if (!visited.Add((cx, cy))) continue;
                            var ccell = grid.GetCell(cx, cy);
                            if (ccell == null) continue;
                            if (ccell.Type == CellType.Corridor)
                            {
                                stillConnected = true;
                                break;
                            }
                            if (ccell.Type != CellType.Room) continue;
                            foreach (var dir in Directions)
                            {
                                int nx = cx + dir.dx;
                                int ny = cy + dir.dy;
                                if (nx < 0 || nx >= grid.Width || ny < 0 || ny >= grid.Height) continue;
                                stack.Push((nx, ny));
                            }
                        }

                        // If not connected, restore the entrance
                        if (!stillConnected)
                        {
                            cell.Type = originalType;
                            cell.Region = originalRegion;
                        }
                    }
                }
            }
        }
    }

    private static void RemoveDeadEnds(Grid grid)
    {
        bool changed;
        do
        {
            changed = false;
            for (int y = 1; y < grid.Height - 1; y++)
            {
                for (int x = 1; x < grid.Width - 1; x++)
                {
                    var cell = grid.GetCell(x, y);
                    if (cell != null && cell.Type == CellType.Corridor)
                    {
                        int openSides = 0;
                        foreach (var dir in Directions)
                        {
                            var neighbor = grid.GetCell(x + dir.dx, y + dir.dy);
                            if (neighbor != null && neighbor.Type != CellType.Rock)
                            {
                                openSides++;
                            }
                        }

                        if (openSides == 1)
                        {
                            cell.Type = CellType.Rock;
                            cell.Region = -1;
                            changed = true;
                        }
                    }
                }
            }
        } while (changed);
    }

    private static void RenumberRooms(Grid grid)
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

    // Ensures every room has at least one adjacent corridor or region
    private static void EnsureRoomConnectivity(Grid grid)
    {
        foreach (var room in grid.Rooms)
        {
            // Collect all perimeter tiles
            var perimeter = new List<(int x, int y)>();
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                perimeter.Add((x, room.Y - 1)); // North
                perimeter.Add((x, room.Y + room.Height)); // South
            }
            for (int y = room.Y; y < room.Y + room.Height; y++)
            {
                perimeter.Add((room.X - 1, y)); // West
                perimeter.Add((room.X + room.Width, y)); // East
            }

            // Check if already connected
            bool connected = false;
            foreach (var (x, y) in perimeter)
            {
                var cell = grid.GetCell(x, y);
                if (cell != null && cell.Type != CellType.Rock)
                {
                    connected = true;
                    break;
                }
            }
            if (connected) continue;

            // Dig through rock to the nearest corridor (or open cell)
            var queue = new Queue<List<(int x, int y)>>();
            var visited = new HashSet<(int x, int y)>();
            foreach (var (x, y) in perimeter)
            {
                var cell = grid.GetCell(x, y);
                if (cell != null)
                {
                    queue.Enqueue(new List<(int x, int y)> { (x, y) });
                    visited.Add((x, y));
                }
            }
            bool found = false;
            List<(int x, int y)> path = null;
            while (queue.Count > 0 && !found)
            {
                var currentPath = queue.Dequeue();
                var (cx, cy) = currentPath.Last();
                foreach (var dir in Directions)
                {
                    int nx = cx + dir.dx;
                    int ny = cy + dir.dy;
                    var ncell = grid.GetCell(nx, ny);
                    if (ncell == null || visited.Contains((nx, ny))) continue;
                    if (ncell.Type == CellType.Corridor && !ncell.InRoom)
                    {
                        // Found a corridor connection
                        path = new List<(int x, int y)>(currentPath) { (nx, ny) };
                        found = true;
                        break;
                    }
                    // Always allow tunneling through rock
                    if (ncell.Type == CellType.Rock || ncell.Type == CellType.Corridor)
                    {
                        var newPath = new List<(int x, int y)>(currentPath) { (nx, ny) };
                        queue.Enqueue(newPath);
                        visited.Add((nx, ny));
                    }
                }
            }
            if (found && path != null)
            {
                // Carve the path
                foreach (var (px, py) in path)
                {
                    var c = grid.GetCell(px, py);
                    if (c != null && c.Type == CellType.Rock)
                    {
                        c.Type = CellType.Corridor;
                        c.Region = -1;
                    }
                }
            }
        }
    }

    // Assign doors to room-corridor exits with 50% no door, 25% open, 25% closed
    private static void AssignDoorsToRoomExits(Grid grid)
    {
        var random = _random;
        foreach (var cell in grid.GetAllCells())
        {
            if (cell.Type != CellType.Room) continue;
            int x = cell.X;
            int y = cell.Y;

            // Skip if cell already has a door
            if (cell.HasExit) continue;

            // For each direction, check if neighbor is a corridor
            var directions = new[]
            {
                (dx: 0, dy: -1, setExit: new Action<Cell, DoorState>((c, s) => c.NorthExit = s), getExit: new Func<Cell, DoorState>(c => c.NorthExit),
                    setNeighborExit: new Action<Cell, DoorState>((c, s) => c.SouthExit = s)),
                (dx: 1, dy: 0, setExit: new Action<Cell, DoorState>((c, s) => c.EastExit = s), getExit: new Func<Cell, DoorState>(c => c.EastExit),
                    setNeighborExit: new Action<Cell, DoorState>((c, s) => c.WestExit = s)),
                (dx: 0, dy: 1, setExit: new Action<Cell, DoorState>((c, s) => c.SouthExit = s), getExit: new Func<Cell, DoorState>(c => c.SouthExit),
                    setNeighborExit: new Action<Cell, DoorState>((c, s) => c.NorthExit = s)),
                (dx: -1, dy: 0, setExit: new Action<Cell, DoorState>((c, s) => c.WestExit = s), getExit: new Func<Cell, DoorState>(c => c.WestExit),
                    setNeighborExit: new Action<Cell, DoorState>((c, s) => c.EastExit = s)),
            };
            foreach (var dir in directions)
            {
                int nx = x + dir.dx;
                int ny = y + dir.dy;
                var neighbor = grid.GetCell(nx, ny);
                if (neighbor == null || neighbor.Type != CellType.Corridor) continue;
                // Only assign if not already set (avoid double assignment)
                if (dir.getExit(cell) != DoorState.None) continue;
                // 0-99: 0-49 = None, 50-74 = Open, 75-99 = Closed
                int roll = random.Next(100);
                DoorState state = DoorState.None;
                if (roll >= 50 && roll < 75) state = DoorState.Open;
                else if (roll >= 75) state = DoorState.Closed;
                dir.setExit(cell, state);
                dir.setNeighborExit(neighbor, state);
                break; // Only add one door per cell
            }
        }
    }
}
