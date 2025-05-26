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
            var config = Config.ParseArgs(args);
            config.Display();
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
