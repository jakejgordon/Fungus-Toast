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
        private const int MaxSupportedPlayers = 8;

        private readonly record struct BoardSize(int Width, int Height);

        static void Main(string[] args)
        {
            // Parse command-line arguments
            var config = ParseCommandLineArguments(args);
            if (config == null) return; // Help was displayed, exit early

            string experimentId = string.IsNullOrWhiteSpace(config.ExperimentId)
                ? $"exp_{DateTime.UtcNow:yyyyMMddTHHmmss}"
                : config.ExperimentId.Trim();

            // Always set up output redirection - if no filename specified, OutputManager will generate one
            OutputManager? outputManager = null;
            outputManager = new OutputManager(config.OutputFileName);

            try
            {
                if (config.IsBatchMode)
                {
                    RunStratifiedBatch(config, experimentId);
                }
                else
                {
                    RunSingleSimulation(config, experimentId);
                }
            }
            finally
            {
                outputManager?.Dispose();
            }
            Environment.Exit(0);
        }

        private static void RunSingleSimulation(SimulationConfig config, string experimentId)
        {
            var strategyRng = config.BaseSeed.HasValue ? new Random(config.BaseSeed.Value) : null;
            var strategies = config.ExplicitStrategyNames is { Count: > 0 }
                ? AIRoster.GetStrategiesByName(config.StrategySet, config.ExplicitStrategyNames, out _)
                : AIRoster.GetStrategies(
                    config.NumberOfPlayers,
                    config.StrategySet,
                    strategyRng,
                    config.StrategySelectionPolicy,
                    cycleIndex: 0);
            var runMetadata = BuildRunMetadata(
                config,
                experimentId,
                strategies.Count,
                config.BoardWidth,
                config.BoardHeight,
                config.StrategySet,
                config.BaseSeed ?? 0);

            SimulationRunner.RunStandardSimulation(
                strategies.Count,
                config.NumberOfGames,
                strategies,
                config.BoardWidth,
                config.BoardHeight,
                enableKeyboardInterrupt: !config.DisableKeyboardInterrupt,
                baseSeed: config.BaseSeed,
                strategySet: config.StrategySet,
                slotAssignmentPolicy: config.SlotAssignmentPolicy,
                runMetadata: runMetadata,
                exportParquet: config.ExportParquet);
        }

        private static void RunStratifiedBatch(SimulationConfig config, string experimentId)
        {
            var playerCounts = config.PlayerCounts?.Count > 0
                ? config.PlayerCounts
                : new List<int> { config.NumberOfPlayers };
            var boardSizes = config.BoardSizes?.Count > 0
                ? config.BoardSizes
                : new List<BoardSize> { new(config.BoardWidth, config.BoardHeight) };
            var strategySets = config.StrategySets?.Count > 0
                ? config.StrategySets
                : new List<StrategySetEnum> { config.StrategySet };

            int totalStrata = playerCounts.Count * boardSizes.Count * strategySets.Count;
            int stratumIndex = 0;

            Console.WriteLine($"Starting stratified batch '{experimentId}' with {totalStrata} strata.");
            Console.WriteLine($"Per-stratum games: {config.NumberOfGames}");

            bool enableKeyboardInterrupt = !config.DisableKeyboardInterrupt;
            if (enableKeyboardInterrupt)
            {
                Console.WriteLine("Batch mode detected: keyboard interruption remains enabled. Use --no-keyboard for unattended runs.");
            }

            foreach (var players in playerCounts)
            {
                foreach (var board in boardSizes)
                {
                    foreach (var strategySet in strategySets)
                    {
                        stratumIndex++;
                        int stratumSeed = DeriveStratumSeed(config.BaseSeed ?? 0, players, board.Width, board.Height, strategySet);
                        string stratumExperimentId = $"{experimentId}__p{players}_w{board.Width}_h{board.Height}_s{strategySet}";
                        var strategyRng = new Random(stratumSeed);
                        var strategies = AIRoster.GetStrategies(
                            players,
                            strategySet,
                            strategyRng,
                            config.StrategySelectionPolicy,
                            cycleIndex: stratumIndex - 1);
                        var runMetadata = BuildRunMetadata(
                            config,
                            stratumExperimentId,
                            players,
                            board.Width,
                            board.Height,
                            strategySet,
                            stratumSeed);

                        Console.WriteLine();
                        Console.WriteLine($"=== Stratum {stratumIndex}/{totalStrata} ===");
                        Console.WriteLine($"Players={players}, Board={board.Width}x{board.Height}, StrategySet={strategySet}, Seed={stratumSeed}");

                        SimulationRunner.RunStandardSimulation(
                            players,
                            config.NumberOfGames,
                            strategies,
                            board.Width,
                            board.Height,
                            enableKeyboardInterrupt: enableKeyboardInterrupt,
                            baseSeed: stratumSeed,
                            strategySet: strategySet,
                            slotAssignmentPolicy: config.SlotAssignmentPolicy,
                            runMetadata: runMetadata,
                            exportParquet: config.ExportParquet);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Batch complete. Export root hint: SimulationParquet/{experimentId}__*");
        }

        private static SimulationRunMetadata BuildRunMetadata(
            SimulationConfig config,
            string experimentId,
            int numberOfPlayers,
            int boardWidth,
            int boardHeight,
            StrategySetEnum strategySet,
            int baseSeed)
        {
            return new SimulationRunMetadata
            {
                ExperimentId = experimentId,
                RunTimestampUtc = DateTime.UtcNow,
                StrategySet = strategySet,
                BaseSeed = baseSeed,
                SlotAssignmentPolicy = config.SlotAssignmentPolicy,
                NumberOfPlayers = numberOfPlayers,
                NumberOfGamesRequested = config.NumberOfGames,
                BoardWidth = boardWidth,
                BoardHeight = boardHeight
            };
        }

        private static int DeriveStratumSeed(int baseSeed, int players, int width, int height, StrategySetEnum strategySet)
        {
            unchecked
            {
                int seed = baseSeed;
                seed = (seed * 397) ^ players;
                seed = (seed * 397) ^ width;
                seed = (seed * 397) ^ height;
                seed = (seed * 397) ^ (int)strategySet;
                return seed;
            }
        }

        private static SimulationConfig? ParseCommandLineArguments(string[] args)
        {
            bool playersExplicitlySpecified = false;
            var config = new SimulationConfig
            {
                NumberOfGames = DefaultNumberOfSimulationGames,
                NumberOfPlayers = DefaultNumberOfPlayers,
                BoardWidth = GameBalance.BoardWidth,
                BoardHeight = GameBalance.BoardHeight,
                OutputFileName = "", // Default to empty string for auto-generated filename
                DisableKeyboardInterrupt = false,
                StrategySet = StrategySetEnum.Testing,
                BaseSeed = 0,
                SlotAssignmentPolicy = SlotAssignmentPolicy.Fixed,
                StrategySelectionPolicy = StrategySelectionPolicy.CoverageBalanced,
                ExportParquet = true,
                ExperimentId = "",
                PlayerCounts = null,
                BoardSizes = null,
                StrategySets = null
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
                            playersExplicitlySpecified = true;
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
                    case "--strategy-set":
                    case "-s":
                        if (i + 1 < args.Length)
                        {
                            var strategySetValue = args[i + 1];
                            if (!Enum.TryParse<StrategySetEnum>(strategySetValue, ignoreCase: true, out var parsedSet))
                            {
                                Console.WriteLine($"Invalid strategy set: {strategySetValue}");
                                Console.WriteLine($"Valid values: {string.Join(", ", Enum.GetNames(typeof(StrategySetEnum)))}");
                                return null;
                            }

                            config.StrategySet = parsedSet;
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--strategy-names":
                    case "--strategies":
                        if (i + 1 < args.Length)
                        {
                            var parsedNames = ParseCsvStrings(args[i + 1]);
                            if (parsedNames.Count == 0)
                            {
                                Console.WriteLine($"Invalid --strategy-names value: {args[i + 1]}");
                                return null;
                            }

                            if (parsedNames.Count > MaxSupportedPlayers)
                            {
                                Console.WriteLine($"--strategy-names contains {parsedNames.Count} entries, exceeding max supported players ({MaxSupportedPlayers}).");
                                return null;
                            }

                            config.ExplicitStrategyNames = parsedNames;
                            i++;
                        }
                        break;
                    case "--player-counts":
                        if (i + 1 < args.Length)
                        {
                            var parsed = ParseCsvIntegers(args[i + 1]);
                            if (parsed.Count == 0)
                            {
                                Console.WriteLine($"Invalid --player-counts value: {args[i + 1]}");
                                return null;
                            }

                            config.PlayerCounts = parsed;
                            i++;
                        }
                        break;
                    case "--board-sizes":
                        if (i + 1 < args.Length)
                        {
                            var parsed = ParseBoardSizes(args[i + 1]);
                            if (parsed.Count == 0)
                            {
                                Console.WriteLine($"Invalid --board-sizes value: {args[i + 1]}");
                                Console.WriteLine("Expected format: 80x80,160x160");
                                return null;
                            }

                            config.BoardSizes = parsed;
                            i++;
                        }
                        break;
                    case "--strategy-sets":
                        if (i + 1 < args.Length)
                        {
                            var parsed = ParseStrategySets(args[i + 1]);
                            if (parsed.Count == 0)
                            {
                                Console.WriteLine($"Invalid --strategy-sets value: {args[i + 1]}");
                                Console.WriteLine($"Valid values: {string.Join(", ", Enum.GetNames(typeof(StrategySetEnum)))}");
                                return null;
                            }

                            config.StrategySets = parsed;
                            i++;
                        }
                        break;
                    case "--seed":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int baseSeed))
                        {
                            config.BaseSeed = baseSeed;
                            i++; // Skip the next argument since we consumed it
                        }
                        break;
                    case "--selection-policy":
                    case "--strategy-selection":
                        if (i + 1 < args.Length)
                        {
                            if (!Enum.TryParse<StrategySelectionPolicy>(args[i + 1], ignoreCase: true, out var selectionPolicy))
                            {
                                Console.WriteLine($"Invalid selection policy: {args[i + 1]}");
                                Console.WriteLine($"Valid values: {string.Join(", ", Enum.GetNames(typeof(StrategySelectionPolicy)))}");
                                return null;
                            }

                            config.StrategySelectionPolicy = selectionPolicy;
                            i++;
                        }
                        break;
                    case "--experiment-id":
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            config.ExperimentId = args[i + 1];
                            i++;
                        }
                        break;
                    case "--no-keyboard":
                    case "--non-interactive":
                        config.DisableKeyboardInterrupt = true;
                        break;
                    case "--parquet":
                        config.ExportParquet = true;
                        break;
                    case "--no-parquet":
                        config.ExportParquet = false;
                        break;
                    case "--rotate-slots":
                        config.SlotAssignmentPolicy = SlotAssignmentPolicy.RotateByGame;
                        break;
                    case "--fixed-slots":
                        config.SlotAssignmentPolicy = SlotAssignmentPolicy.Fixed;
                        break;
                }
            }

            if (config.NumberOfPlayers > MaxSupportedPlayers)
            {
                Console.WriteLine($"Requested players ({config.NumberOfPlayers}) exceeds supported maximum ({MaxSupportedPlayers}). Capping to {MaxSupportedPlayers}.");
                config.NumberOfPlayers = MaxSupportedPlayers;
            }

            if (config.PlayerCounts != null && config.PlayerCounts.Count > 0)
            {
                var capped = config.PlayerCounts.Select(v => Math.Min(v, MaxSupportedPlayers)).Distinct().ToList();
                if (capped.Count != config.PlayerCounts.Count || capped.Any(v => !config.PlayerCounts.Contains(v)))
                {
                    Console.WriteLine($"Some --player-counts entries exceeded {MaxSupportedPlayers} and were capped.");
                }

                config.PlayerCounts = capped;
            }

            if (config.ExplicitStrategyNames is { Count: > 0 })
            {
                if (config.IsBatchMode)
                {
                    Console.WriteLine("--strategy-names is currently supported only for single-run mode (not with --player-counts/--board-sizes/--strategy-sets).");
                    return null;
                }

                var selectedByName = AIRoster.GetStrategiesByName(config.StrategySet, config.ExplicitStrategyNames, out var missingNames);
                if (missingNames.Count > 0)
                {
                    var availableNames = AIRoster.GetStrategyProfiles(config.StrategySet)
                        .Select(p => p.StrategyName)
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    Console.WriteLine($"Unknown strategy name(s) for set {config.StrategySet}: {string.Join(", ", missingNames)}");
                    Console.WriteLine($"Available strategies: {string.Join(", ", availableNames)}");
                    return null;
                }

                if (selectedByName.Count == 0)
                {
                    Console.WriteLine("No valid strategy names were provided.");
                    return null;
                }

                if (playersExplicitlySpecified && config.NumberOfPlayers != selectedByName.Count)
                {
                    Console.WriteLine($"Overriding --players with explicit strategy count: {selectedByName.Count}");
                }

                config.NumberOfPlayers = selectedByName.Count;
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
            Console.WriteLine($"  -g, --games <number>     Number of games to play per matchup (default: {DefaultNumberOfSimulationGames})");
            Console.WriteLine($"  -p, --players <number>   Number of players/strategies to use (default: {DefaultNumberOfPlayers}, max: {MaxSupportedPlayers})");
            Console.WriteLine($"  -w, --width <number>     Board width (default: {GameBalance.BoardWidth})");
            Console.WriteLine($"  --height <number>        Board height (default: {GameBalance.BoardHeight})");
            Console.WriteLine("  -s, --strategy-set <set> Strategy set: Proven, Testing, Mycovariants, Campaign (default: Testing)");
            Console.WriteLine("  --player-counts <csv>    Batch mode list, e.g. 2,4,8");
            Console.WriteLine("  --board-sizes <csv>      Batch mode list, e.g. 80x80,160x160");
            Console.WriteLine("  --strategy-sets <csv>    Batch mode list, e.g. Testing,Proven,Mycovariants");
            Console.WriteLine("  --strategy-names <csv>   Explicit strategy names for single-run mode (overrides --players)");
            Console.WriteLine("  --seed <number>          Base seed for deterministic strategy/order/game seeds (default: 0)");
            Console.WriteLine("  --selection-policy <p>   Strategy sampler: RandomUnique, CoverageBalanced, StratifiedCycle (default: CoverageBalanced)");
            Console.WriteLine("  --experiment-id <id>     Identifier for this run's analytics artifacts");
            Console.WriteLine("  -o, --output <filename>  Specify output filename (default: auto-generated with timestamp)");
            Console.WriteLine("  --no-keyboard            Disable keyboard interruption (Q/Escape), useful for automation");
            Console.WriteLine("  --non-interactive        Alias for --no-keyboard");
            Console.WriteLine("  --parquet                Export canonical Parquet datasets (default: enabled)");
            Console.WriteLine("  --no-parquet             Disable Parquet export");
            Console.WriteLine("  --rotate-slots           Rotate strategy-to-player slot assignment each game");
            Console.WriteLine("  --fixed-slots            Keep strategy-to-player slot assignment fixed (default)");
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
            Console.WriteLine("  dotnet run --strategy-set Proven    # Use proven strategy roster");
            Console.WriteLine("  dotnet run --player-counts 2,4,8 --board-sizes 80x80,160x160 --strategy-sets Testing,Proven --games 50 --no-keyboard");
            Console.WriteLine("  dotnet run --seed 12345             # Run with deterministic seed 12345");
            Console.WriteLine("  dotnet run --selection-policy StratifiedCycle --games 200 --no-keyboard");
            Console.WriteLine("  dotnet run --strategy-set Testing --strategy-names TST_BalancedGeneralistControl,TST_BalancedControl_MaxEconomy --games 20 --no-keyboard");
            Console.WriteLine("  dotnet run --experiment-id testA    # Tag outputs under experiment ID testA");
            Console.WriteLine("  dotnet run --width 50 --height 75   # Run with 50x75 board");
            Console.WriteLine("  dotnet run -w 200 -p 4              # Run 4 players on 200x100 board");
            Console.WriteLine("  dotnet run -o my_test.txt           # Run with custom output filename");
            Console.WriteLine("  dotnet run --rotate-slots --games 100 --no-keyboard  # Rotate slot assignment per game");
            Console.WriteLine("  dotnet run --games 1 --no-keyboard  # Run non-interactive (no Q/Escape listener)");
        }

        private class SimulationConfig
        {
            public int NumberOfGames { get; set; }
            public int NumberOfPlayers { get; set; }
            public int BoardWidth { get; set; }
            public int BoardHeight { get; set; }
            public string OutputFileName { get; set; } = "";
            public bool DisableKeyboardInterrupt { get; set; }
            public StrategySetEnum StrategySet { get; set; }
            public int? BaseSeed { get; set; }
            public SlotAssignmentPolicy SlotAssignmentPolicy { get; set; }
            public StrategySelectionPolicy StrategySelectionPolicy { get; set; }
            public bool ExportParquet { get; set; }
            public string ExperimentId { get; set; } = "";
            public List<int>? PlayerCounts { get; set; }
            public List<BoardSize>? BoardSizes { get; set; }
            public List<StrategySetEnum>? StrategySets { get; set; }
            public List<string>? ExplicitStrategyNames { get; set; }

            public bool IsBatchMode =>
                (PlayerCounts != null && PlayerCounts.Count > 0) ||
                (BoardSizes != null && BoardSizes.Count > 0) ||
                (StrategySets != null && StrategySets.Count > 0);
        }

        private static List<int> ParseCsvIntegers(string csv)
        {
            var result = new List<int>();
            var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var value) || value <= 0)
                {
                    return new List<int>();
                }

                result.Add(value);
            }

            return result.Distinct().ToList();
        }

        private static List<BoardSize> ParseBoardSizes(string csv)
        {
            var result = new List<BoardSize>();
            var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                var dims = part.Split('x', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (dims.Length != 2 ||
                    !int.TryParse(dims[0], out var width) ||
                    !int.TryParse(dims[1], out var height) ||
                    width <= 0 ||
                    height <= 0)
                {
                    return new List<BoardSize>();
                }

                result.Add(new BoardSize(width, height));
            }

            return result.Distinct().ToList();
        }

        private static List<StrategySetEnum> ParseStrategySets(string csv)
        {
            var result = new List<StrategySetEnum>();
            var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!Enum.TryParse<StrategySetEnum>(part, ignoreCase: true, out var parsed))
                {
                    return new List<StrategySetEnum>();
                }

                result.Add(parsed);
            }

            return result.Distinct().ToList();
        }

        private static List<string> ParseCsvStrings(string csv)
        {
            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();
        }
    }
}
