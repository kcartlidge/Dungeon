using Dungeon.Services;

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

            // Generate grid.
            var grid = Generator.Generate(config);
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
