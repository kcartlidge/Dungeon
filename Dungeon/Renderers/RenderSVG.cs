using System.Text;
using Dungeon.Models;

namespace Dungeon.Renderers;

/// <summary>
/// Provides functionality to render a dungeon grid as an SVG file.
/// </summary>
public static class RenderSVG
{
    /// <summary>
    /// Renders the dungeon grid as an SVG file.
    /// </summary>
    /// <param name="grid">The dungeon grid to render.</param>
    /// <param name="config">The configuration settings for rendering.</param>
    public static void ToFile(Grid grid, Config config)
    {
        var svg = new StringBuilder();

        // SVG header with viewBox
        svg.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
        svg.AppendLine($"<svg width=\"{grid.Width * config.CellSize}\" height=\"{grid.Height * config.CellSize}\" " +
                      $"viewBox=\"0 0 {grid.Width * config.CellSize} {grid.Height * config.CellSize}\" " +
                      "xmlns=\"http://www.w3.org/2000/svg\">");

        // Add stylesheet
        int pad = config.CellSize * 2 / 10;
        int sw = pad / 2;
        int fontSize = pad * 3;
        svg.AppendLine("  <style>");
        svg.AppendLine($"    rect.cell {{ stroke: #666; stroke-width: 1; width: {config.CellSize}px; height: {config.CellSize}px; }}");
        svg.AppendLine($"    rect.rock {{ fill: #444; }}");
        svg.AppendLine($"    rect.room {{ fill: #eee; }}");
        svg.AppendLine($"    rect.corridor {{ fill: #999; }}");
        svg.AppendLine($"    text {{ paint-order: stroke fill markers; fill: #000; stroke: #eee; stroke-width: {sw}px; font-size: {fontSize}px; font-family: Verdana, Tahoma, Sans-Serif; text-anchor: left; dominant-baseline: middle; }}");
        svg.AppendLine("  </style>");

        // Draw each cell
        foreach (var cell in grid.GetAllCells())
        {
            var x = cell.X * config.CellSize;
            var y = cell.Y * config.CellSize;
            var fill = cell.Type.ToString().ToLower();

            svg.AppendLine($"  <rect class=\"cell {fill}\" x=\"{x}\" y=\"{y}\" />");
        }

        // Draw room numbers
        foreach (var room in grid.Rooms)
        {
            // Write room number at offset from cell top-left
            double textX = (room.X * config.CellSize) + pad;
            double textY = (room.Y * config.CellSize) + (pad * 2.75f);
            svg.AppendLine($"  <text x=\"{textX}\" y=\"{textY}\">{room.RoomNumber}</text>");
        }

        // SVG footer
        svg.AppendLine("</svg>");

        // Write to file
        File.WriteAllText(config.Filename, svg.ToString());
    }
}
