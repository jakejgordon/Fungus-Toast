using FungusToast.Core.AI;
using FungusToast.Core.Mutations;
using FungusToast.Simulation.Analysis;
using FungusToast.Simulation.Models;
using System.Text;

namespace FungusToast.Simulation
{
    class Program
    {
        private const int DefaultNumberOfSimulationGames = 5;
        private const int DefaultNumberOfPlayers = 8;

        static void Main(string[] args)
        {
            // Parse command-line arguments
            int numberOfGames = DefaultNumberOfSimulationGames;
            int numberOfPlayers = DefaultNumberOfPlayers;
            bool outputToFile = false;
            string outputFileName = "";
            bool runNeutralizingTest = false;
            bool runResistantTest = false;
            bool runBastionTest = false;

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
                    case "--output":
                    case "-o":
                        outputToFile = true;
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            outputFileName = args[i + 1];
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--test-neutralizing":
                    case "-t":
                        runNeutralizingTest = true;
                        break;
                    case "--test-resistant":
                    case "-r":
                        runResistantTest = true;
                        break;
                    case "--test-bastion":
                    case "-b":
                        runBastionTest = true;
                        break;
                    case "--help":
                    case "-h":
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
                if (runNeutralizingTest)
                {
                    NeutralizingTestRunner.RunTest(numberOfGames, outputToFile, outputFileName);
                }
                else if (runResistantTest)
                {
                    ResistantCellTester.RunTest(numberOfGames, outputToFile, outputFileName);
                }
                else if (runBastionTest)
                {
                    BastionTestRunner.RunTest(numberOfGames, outputToFile, outputFileName);
                }
                else
                {
                    SimulationRunner.RunStandardSimulation(numberOfPlayers, numberOfGames);
                }
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
            Console.WriteLine("  -g, --games <number>     Number of games to play per matchup (default: 5)");
            Console.WriteLine("  -p, --players <number>   Number of players/strategies to use (default: 8)");
            Console.WriteLine("  -o, --output <filename>  Redirect output to a file");
            Console.WriteLine("  -t, --test-neutralizing  Run Neutralizing Mantle test");
            Console.WriteLine("  -r, --test-resistant   Run Resistant cell system test");
            Console.WriteLine("  -b, --test-bastion     Run Mycelial Bastion test");
            Console.WriteLine("  -h, --help              Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run                           # Run with defaults (8 players, 5 games each)");
            Console.WriteLine("  dotnet run --games 10               # Run 10 games per matchup");
            Console.WriteLine("  dotnet run --players 4 --games 20   # Run 4 players, 20 games each");
            Console.WriteLine("  dotnet run -p 6 -g 15               # Run 6 players, 15 games each");
        }
    }
}
