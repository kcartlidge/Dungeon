using System.Text.Json;
using Dungeon.Models;

namespace Dungeon.Renderers;

/// <summary>
/// Provides functionality to render a dungeon grid as a JSON file.
/// </summary>
public static class RenderJSON
{
    /// <summary>
    /// Renders the dungeon grid as a JSON file.
    /// </summary>
    /// <param name="grid">The dungeon grid to render.</param>
    /// <param name="config">The configuration settings for rendering.</param>
    public static void ToFile(Grid grid, Config config)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var jsonString = JsonSerializer.Serialize(grid, options);
        File.WriteAllText(Path.ChangeExtension(config.Filename, ".json"), jsonString);
    }
}
