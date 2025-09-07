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
        private const int DefaultNumberOfSimulationGames = 1;
        private const int DefaultNumberOfPlayers = 8;

        static void Main(string[] args)
        {
            // Parse command-line arguments
            var config = ParseCommandLineArguments(args);
            if (config == null) return; // Help was displayed, exit early

            //var strategies = AIRoster.GetStrategies(config.NumberOfPlayers, StrategySetEnum.Proven);
            var strategies = AIRoster.GetStrategies(config.NumberOfPlayers, StrategySetEnum.Testing);

            // Always set up output redirection - if no filename specified, OutputManager will generate one
            OutputManager? outputManager = null;
            outputManager = new OutputManager(config.OutputFileName);

            try
            {
                SimulationRunner.RunStandardSimulation(config.NumberOfPlayers, config.NumberOfGames, strategies, config.BoardWidth, config.BoardHeight);
            }
            finally
            {
                outputManager?.Dispose();
            }
            Environment.Exit(0);
        }

        private static SimulationConfig? ParseCommandLineArguments(string[] args)
        {
            var config = new SimulationConfig
            {
                NumberOfGames = DefaultNumberOfSimulationGames,
                NumberOfPlayers = DefaultNumberOfPlayers,
                BoardWidth = GameBalance.BoardWidth,
                BoardHeight = GameBalance.BoardHeight,
                OutputFileName = "" // Default to empty string for auto-generated filename
            };

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--games":
                    case "-g":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int games))
                        {
                            config.NumberOfGames = games;
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--players":
                    case "-p":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int players))
                        {
                            config.NumberOfPlayers = players;
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--width":
                    case "-w":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int width))
                        {
                            config.BoardWidth = width;
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--height":
                    case "-h":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int height))
                        {
                            config.BoardHeight = height;
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--output":
                    case "-o":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            config.OutputFileName = args[i + 1];
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--help":
                        PrintUsage();
                        return null; // Return null to indicate help was displayed
                }
            }

            return config;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("FungusToast Simulation Runner");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -g, --games <number>     Number of games to play per matchup (default: 500)");
            Console.WriteLine("  -p, --players <number>   Number of players/strategies to use (default: 8)");
            Console.WriteLine("  -w, --width <number>     Board width (default: 100)");
            Console.WriteLine("  --height <number>        Board height (default: 100)");
            Console.WriteLine("  -o, --output <filename>  Specify output filename (default: auto-generated with timestamp)");
            Console.WriteLine("  --help                   Show this help message");
            Console.WriteLine();
            Console.WriteLine("Note: All simulation output is automatically saved to the SimulationOutput folder.");
            Console.WriteLine("If no output filename is specified, a timestamped filename will be generated.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run                           # Run with defaults, auto-generated output file");
            Console.WriteLine("  dotnet run --games 10               # Run 10 games per matchup");
            Console.WriteLine("  dotnet run --players 4 --games 20   # Run 4 players, 20 games each");
            Console.WriteLine("  dotnet run -p 6 -g 15               # Run 6 players, 15 games each");
            Console.WriteLine("  dotnet run --width 50 --height 75   # Run with 50x75 board");
            Console.WriteLine("  dotnet run -w 200 -p 4              # Run 4 players on 200x100 board");
            Console.WriteLine("  dotnet run -o my_test.txt           # Run with custom output filename");
        }

        private class SimulationConfig
        {
            public int NumberOfGames { get; set; }
            public int NumberOfPlayers { get; set; }
            public int BoardWidth { get; set; }
            public int BoardHeight { get; set; }
            public string OutputFileName { get; set; } = "";
        }
    }
}
