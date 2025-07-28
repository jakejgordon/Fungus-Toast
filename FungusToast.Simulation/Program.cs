using FungusToast.Core.AI;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Analysis;
using FungusToast.Simulation.Models;
using System.Text;

namespace FungusToast.Simulation
{
    class Program
    {
        private const int DefaultNumberOfSimulationGames = 20;
        private const int DefaultNumberOfPlayers = 8;

        static void Main(string[] args)
        {
            // Parse command-line arguments
            int numberOfGames = DefaultNumberOfSimulationGames;
            int numberOfPlayers = DefaultNumberOfPlayers;
            int boardWidth = GameBalance.BoardWidth;
            int boardHeight = GameBalance.BoardHeight;
            bool outputToFile = false;
            string outputFileName = "";

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--games":
                    case "-g":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int games))
                        {
                            numberOfGames = games;
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--players":
                    case "-p":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int players))
                        {
                            numberOfPlayers = players;
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--width":
                    case "-w":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int width))
                        {
                            boardWidth = width;
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--height":
                    case "-h":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int height))
                        {
                            boardHeight = height;
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--output":
                    case "-o":
                        outputToFile = true;
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            outputFileName = args[i + 1];
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--help":
                        PrintUsage();
                        return;
                }
            }

            // Set up output redirection if requested
            OutputManager? outputManager = null;
            if (outputToFile)
            {
                outputManager = new OutputManager(outputFileName);
            }

            try
            {
                SimulationRunner.RunStandardSimulation(numberOfPlayers, numberOfGames, boardWidth, boardHeight);
            }
            finally
            {
                outputManager?.Dispose();
            }
            Environment.Exit(0);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("FungusToast Simulation Runner");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -g, --games <number>     Number of games to play per matchup (default: 100)");
            Console.WriteLine("  -p, --players <number>   Number of players/strategies to use (default: 8)");
            Console.WriteLine("  -w, --width <number>     Board width (default: 100)");
            Console.WriteLine("  --height <number>        Board height (default: 100)");
            Console.WriteLine("  -o, --output <filename>  Redirect output to a file");
            Console.WriteLine("  --help                   Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run                           # Run with defaults (8 players, 100 games each, 100x100 board)");
            Console.WriteLine("  dotnet run --games 10               # Run 10 games per matchup");
            Console.WriteLine("  dotnet run --players 4 --games 20   # Run 4 players, 20 games each");
            Console.WriteLine("  dotnet run -p 6 -g 15               # Run 6 players, 15 games each");
            Console.WriteLine("  dotnet run --width 50 --height 75   # Run with 50x75 board");
            Console.WriteLine("  dotnet run -w 200 -p 4              # Run 4 players on 200x100 board");
        }
    }
}
