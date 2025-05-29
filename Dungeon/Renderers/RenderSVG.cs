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
        double cellSize = config.CellSize;
        double doorLen = cellSize * 0.5;
        double doorThick = cellSize * 0.3;
        double offset = (cellSize - doorLen) / 2;
        double inwardOffset = cellSize * 0.25; // door rests against opening
        int sw = pad / 2;
        int fontSize = (pad * 7) / 2;
        svg.AppendLine("  <style>");
        svg.AppendLine($"    rect.cell {{ stroke: #666; stroke-width: 1; width: {config.CellSize}px; height: {config.CellSize}px; }}");
        svg.AppendLine($"    rect.rock {{ fill: #444; }}");
        svg.AppendLine($"    rect.room {{ fill: #eee; }}");
        svg.AppendLine($"    rect.corridor {{ fill: #999; }}");
        svg.AppendLine($"    line.door {{ stroke: #b44; stroke-width: {doorThick}px; stroke-linecap: square; }}");
        svg.AppendLine($"    line.open {{ stroke: #696; stroke-width: {doorThick}px; stroke-linecap: square; }}");
        svg.AppendLine($"    text {{ paint-order: stroke fill markers; fill: #000; stroke: #eee; stroke-width: {sw}px; font-size: {fontSize}px; font-family: Verdana, Tahoma, Sans-Serif; text-anchor: left; dominant-baseline: middle; }}");
        svg.AppendLine("  </style>");

        // Draw each cell
        foreach (var cell in grid.GetAllCells())
        {
            var x = cell.X * config.CellSize;
            var y = cell.Y * config.CellSize;
            var fill = cell.Type.ToString().ToLower();
            svg.AppendLine($"  <rect class=\"cell {fill}\" x=\"{x}\" y=\"{y}\" />");

            // Only draw doors if this is a room cell
            if (cell.Type == CellType.Room)
            {
                // North
                if (cell.NorthExit == DoorState.Closed || cell.NorthExit == DoorState.Open)
                {
                    double x1 = x + offset;
                    double x2 = x + cellSize - offset;
                    double y1 = y + inwardOffset;
                    double y2 = y + inwardOffset;
                    svg.AppendLine($"  <line class=\"door {cell.NorthExit.ToString().ToLower()}\" x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" />");
                }
                // South
                if (cell.SouthExit == DoorState.Closed || cell.SouthExit == DoorState.Open)
                {
                    double x1 = x + offset;
                    double x2 = x + cellSize - offset;
                    double y1 = y + cellSize - inwardOffset;
                    double y2 = y + cellSize - inwardOffset;
                    svg.AppendLine($"  <line class=\"door {cell.SouthExit.ToString().ToLower()}\" x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" />");
                }
                // West
                if (cell.WestExit == DoorState.Closed || cell.WestExit == DoorState.Open)
                {
                    double x1 = x + inwardOffset;
                    double x2 = x + inwardOffset;
                    double y1 = y + offset;
                    double y2 = y + cellSize - offset;
                    svg.AppendLine($"  <line class=\"door {cell.WestExit.ToString().ToLower()}\" x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" />");
                }
                // East
                if (cell.EastExit == DoorState.Closed || cell.EastExit == DoorState.Open)
                {
                    double x1 = x + cellSize - inwardOffset;
                    double x2 = x + cellSize - inwardOffset;
                    double y1 = y + offset;
                    double y2 = y + cellSize - offset;
                    svg.AppendLine($"  <line class=\"door {cell.EastExit.ToString().ToLower()}\" x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" />");
                }
            }
        }

        // Draw room numbers
        foreach (var room in grid.Rooms)
        {
            // Write room number at offset from cell top-left
            double textX = (room.X * config.CellSize) + (config.CellSize * 1.1f);
            double textY = (room.Y * config.CellSize) + (config.CellSize * 1.5f);
            svg.AppendLine($"  <text x=\"{textX}\" y=\"{textY}\">{room.RoomNumber}</text>");
        }

        // SVG footer
        svg.AppendLine("</svg>");

        // Write to file
        File.WriteAllText(config.Filename, svg.ToString());
    }
}
