using ArgsParser;

namespace Dungeon;

/// <summary>
/// Represents the configuration settings for dungeon generation.
/// </summary>
public class Config
{
    /// <summary>
    /// Gets or sets the random seed used for dungeon generation.
    /// </summary>
    public int Seed { get; set; }

    /// <summary>
    /// Gets or sets the width of the dungeon grid.
    /// </summary>
    public int Width { get; set; } = 41;

    /// <summary>
    /// Gets or sets the height of the dungeon grid.
    /// </summary>
    public int Height { get; set; } = 21;

    /// <summary>
    /// Gets or sets the size of each cell in pixels.
    /// </summary>
    public int CellSize { get; set; } = 32;

    /// <summary>
    /// Gets or sets the output filename for the generated dungeon.
    /// </summary>
    public string Filename { get; set; } = "dungeon.svg";

    /// <summary>
    /// Gets or sets the number of attempts to place rooms in the dungeon.
    /// </summary>
    public int NumRoomTries { get; set; } = 50;

    /// <summary>
    /// Gets or sets the text description of the number of attempts to place rooms in the dungeon.
    /// </summary>
    public string RoomTriesText { get; set; } = "default";

    /// <summary>
    /// Parses command line arguments to create a configuration instance.
    /// </summary>
    /// <param name="args">The command line arguments to parse.</param>
    /// <returns>A new <see cref="Config"/> instance with values from the command line arguments.</returns>
    /// <exception cref="ArgumentException">Thrown when invalid command line arguments are provided.</exception>
    public static Config ParseArgs(string[] args)
    {
        var roomValues = new SortedList<string, double> { { "sparse", 0.25 }, { "default", 1 }, { "dense", 2 } };
        var config = new Config();
        var parser = new Parser(args)
            .SupportsOption<int>("seed", "Random seed for dungeon generation")
            .SupportsOption<int>("width", "Width of the dungeon", 41)
            .SupportsOption<int>("height", "Height of the dungeon", 21)
            .SupportsOption<int>("cellsize", "Size of each cell in pixels", 32)
            .SupportsOption<string>("rooms", "Rough room frequency", "default")
            .RequiresOption<string>("filename", "Output filename", "dungeon.svg")
            .AddCustomValidator("width", (name, value) => ((int)value) < 13 ? new List<string> { "Width must be at least 13" } : new List<string>())
            .AddCustomValidator("height", (name, value) => ((int)value) < 13 ? new List<string> { "Height must be at least 13" } : new List<string>())
            .AddCustomValidator("cellsize", (name, value) => ((int)value) < 24 ? new List<string> { "Cell size must be at least 24 pixels" } : new List<string>())
            .AddCustomValidator("rooms", (name, value) => roomValues.ContainsKey(((string)value).ToLower()) ? new List<string>() : new List<string> { "Rooms must be one of " + string.Join(", ", roomValues.Keys) + $", but got `{value}`" })
            .AddExtraHelp("Restrictions:", new[]
            {
                "Width must be at least 13",
                "Height must be at least 13",
                "Cell size must be at least 24 pixels",
                "Rooms must be one of " + string.Join(", ", roomValues.Keys)
            })
            .Help(2, "Usage:")
            .Parse();

        if (parser.HasErrors)
        {
            parser.ShowErrors(2, "Issues:");
            throw new ArgumentException("Invalid command line arguments");
        }

        if (parser.IsOptionProvided("seed"))
            config.Seed = parser.GetOption<int>("seed");
        else
            config.Seed = Random.Shared.Next(99999);

        config.Width = parser.GetOption<int>("width");
        config.Height = parser.GetOption<int>("height");
        config.CellSize = parser.GetOption<int>("cellsize");
        config.Filename = parser.GetOption<string>("filename");

        config.RoomTriesText = parser.GetOption<string>("rooms");
        var roomAdjustment = roomValues[config.RoomTriesText];
        config.NumRoomTries = (int)((config.Width + config.Height) * roomAdjustment);

        // Ensure width and height are odd numbers
        if (config.Width % 2 == 0 || config.Height % 2 == 0)
        {
            if (config.Width % 2 == 0) config.Width++;
            if (config.Height % 2 == 0) config.Height++;
            Console.WriteLine($"  Width and height are forced to be odd");
        }

        return config;
    }

    /// <summary>
    /// Displays the current configuration settings to the console.
    /// </summary>
    public void Display()
    {
        Console.WriteLine();
        Console.WriteLine("Configuration:");
        Console.WriteLine($"  Seed      : {Seed}");
        Console.WriteLine($"  Width     : {Width}");
        Console.WriteLine($"  Height    : {Height}");
        Console.WriteLine($"  Cell Size : {CellSize}");
        Console.WriteLine($"  Filename  : {Filename}");
        Console.WriteLine($"  Rooms     : {RoomTriesText}");
        Console.WriteLine();
    }
}
