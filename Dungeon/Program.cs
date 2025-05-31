using Dungeon.Services;
using Dungeon.Renderers;

namespace Dungeon;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("DUNGEON!");

        try
        {
            // Parse command line arguments.
            var config = Config.ParseArgs(args);
            config.Display();

            // Generate grid
            var grid = Generator.Generate(config);

            // Render SVG
            RenderSVG.ToFile(grid, config);
            Console.WriteLine();
            Console.WriteLine("Generated SVG file:");
            Console.WriteLine(Path.GetFullPath(config.Filename));

            // Render OBJ
            RenderOBJ.ToFile(grid, config);
            Console.WriteLine();
            Console.WriteLine("Generated OBJ file:");
            Console.WriteLine(Path.GetFullPath(Path.ChangeExtension(config.Filename, ".obj")));

            // Render JSON
            RenderJSON.ToFile(grid, config);
            Console.WriteLine();
            Console.WriteLine("Generated JSON file:");
            Console.WriteLine(Path.GetFullPath(Path.ChangeExtension(config.Filename, ".json")));
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine();
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine();
        return 0;
    }
}
