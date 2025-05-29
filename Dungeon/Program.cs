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

            // Generate grid and write SVG file.
            var grid = Generator.Generate(config);
            RenderSVG.ToFile(grid, config);
            Console.WriteLine("Generated SVG file:");
            Console.WriteLine(Path.GetFullPath( config.Filename));
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
