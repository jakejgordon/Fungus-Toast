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
            var filteredStrategyPool = AIRoster.GetStrategiesByFilter(config.StrategySet, config.StrategyFilter);
            var strategies = config.ExplicitStrategyNames is { Count: > 0 }
                ? AIRoster.GetStrategiesByName(config.StrategySet, config.ExplicitStrategyNames, out _)
                : SelectStrategies(
                    config.NumberOfPlayers,
                    config.StrategySet,
                    strategyRng,
                    config.StrategySelectionPolicy,
                    config.StrategyFilter,
                    cycleIndex: 0,
                    prefilteredStrategies: filteredStrategyPool);
            var runMetadata = BuildRunMetadata(
                config,
                experimentId,
                strategies,
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
                exportParquet: config.ExportParquet,
                enableNutrientPatches: config.EnableNutrientPatches,
                enableMycovariantDraft: config.EnableMycovariantDraft,
                startingPositionOverride: config.StartingPositionOverride);
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
                        var filteredStrategyPool = AIRoster.GetStrategiesByFilter(strategySet, config.StrategyFilter);
                        var strategies = SelectStrategies(
                            players,
                            strategySet,
                            strategyRng,
                            config.StrategySelectionPolicy,
                            config.StrategyFilter,
                            cycleIndex: stratumIndex - 1,
                            prefilteredStrategies: filteredStrategyPool);
                        var runMetadata = BuildRunMetadata(
                            config,
                            stratumExperimentId,
                            strategies,
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
                            exportParquet: config.ExportParquet,
                            enableNutrientPatches: config.EnableNutrientPatches,
                            enableMycovariantDraft: config.EnableMycovariantDraft,
                            startingPositionOverride: config.StartingPositionOverride);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Batch complete. Export root hint: SimulationParquet/{experimentId}__*");
        }

        private static List<IMutationSpendingStrategy> SelectStrategies(
            int count,
            StrategySetEnum strategySet,
            Random? rng,
            StrategySelectionPolicy selectionPolicy,
            StrategyCatalogFilter? filter,
            int cycleIndex,
            IReadOnlyList<IMutationSpendingStrategy>? prefilteredStrategies = null)
        {
            if (filter == null || filter.IsEmpty)
            {
                return AIRoster.GetStrategies(count, strategySet, rng, selectionPolicy, cycleIndex);
            }

            var sourceStrategies = prefilteredStrategies?.ToList() ?? AIRoster.GetStrategiesByFilter(strategySet, filter);
            if (sourceStrategies.Count == 0)
            {
                throw new InvalidOperationException($"No strategies matched the requested filters for set {strategySet}.");
            }

            if (count > sourceStrategies.Count)
            {
                throw new InvalidOperationException($"Requested {count} strategies, but only {sourceStrategies.Count} matched the requested filters for set {strategySet}.");
            }

            return selectionPolicy switch
            {
                StrategySelectionPolicy.RandomUnique => sourceStrategies.OrderBy(_ => rng?.Next() ?? Random.Shared.Next()).Take(count).ToList(),
                StrategySelectionPolicy.CoverageBalanced => sourceStrategies.OrderBy(_ => rng?.Next() ?? Random.Shared.Next()).Take(count).ToList(),
                StrategySelectionPolicy.StratifiedCycle => sourceStrategies.Skip(cycleIndex % sourceStrategies.Count).Concat(sourceStrategies.Take(cycleIndex % sourceStrategies.Count)).Take(count).ToList(),
                _ => sourceStrategies.Take(count).ToList()
            };
        }

        private static SimulationRunMetadata BuildRunMetadata(
            SimulationConfig config,
            string experimentId,
            IReadOnlyList<IMutationSpendingStrategy> selectedStrategies,
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
                StrategySelectionPolicy = config.StrategySelectionPolicy,
                StrategySelectionSource = config.ExplicitStrategyNames is { Count: > 0 }
                    ? StrategySelectionSource.ExplicitLineup
                    : StrategySelectionSource.Sampled,
                NumberOfPlayers = selectedStrategies.Count,
                NumberOfGamesRequested = config.NumberOfGames,
                BoardWidth = boardWidth,
                BoardHeight = boardHeight,
                SelectedStrategies = selectedStrategies
                    .Select((strategy, index) =>
                    {
                        var profile = AIRoster.GetStrategyProfile(strategySet, strategy.StrategyName);
                        return new SelectedStrategyMetadata
                        {
                            LineupOrder = index + 1,
                            StrategyName = strategy.StrategyName,
                            StrategyTheme = AIRoster.GetThemeForStrategy(strategy).ToString(),
                            StrategyStatus = AIRoster.GetStatusForStrategy(strategy, strategySet).ToString(),
                            StrategyPowerTier = profile?.PowerTier.ToString() ?? string.Empty,
                            StrategyRole = profile?.Role.ToString() ?? string.Empty,
                            StrategyLifecycle = profile?.Lifecycle.ToString() ?? string.Empty,
                            DifficultyBands = profile?.DifficultyBands.Select(x => x.ToString()).ToList() ?? new List<string>(),
                            StrategyPools = profile?.Pools.ToString() ?? string.Empty,
                            FavoredAgainst = profile?.FavoredAgainst.Select(FormatCounterTag).ToList() ?? new List<string>(),
                            WeakAgainst = profile?.WeakAgainst.Select(FormatCounterTag).ToList() ?? new List<string>(),
                            Notes = profile?.Notes ?? string.Empty,
                            Intent = profile?.Intent ?? string.Empty
                        };
                    })
                    .ToList()
            };
        }

        private static string FormatCounterTag(CounterTag counterTag)
        {
            var target = counterTag.StrategyName
                ?? counterTag.Archetype?.ToString()
                ?? "Unknown";

            if (string.IsNullOrWhiteSpace(counterTag.Reason))
            {
                return target;
            }

            return $"{target}: {counterTag.Reason}";
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
                EnableNutrientPatches = true,
                EnableMycovariantDraft = true,
                StartingPositionOverride = null,
                ExperimentId = "",
                PlayerCounts = null,
                BoardSizes = null,
                StrategySets = null,
                StrategyFilter = new StrategyCatalogFilter()
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
                    case "--archetypes":
                        if (i + 1 < args.Length)
                        {
                            var parsed = ParseCsvEnums<StrategyArchetype>(args[i + 1]);
                            if (parsed.Count == 0)
                            {
                                Console.WriteLine($"Invalid --archetypes value: {args[i + 1]}");
                                Console.WriteLine($"Valid values: {string.Join(", ", Enum.GetNames(typeof(StrategyArchetype)))}");
                                return null;
                            }

                            config.StrategyFilter.Archetypes = parsed;
                            i++;
                        }
                        break;
                    case "--power-tiers":
                        if (i + 1 < args.Length)
                        {
                            var parsed = ParseCsvEnums<StrategyPowerTier>(args[i + 1]);
                            if (parsed.Count == 0)
                            {
                                Console.WriteLine($"Invalid --power-tiers value: {args[i + 1]}");
                                Console.WriteLine($"Valid values: {string.Join(", ", Enum.GetNames(typeof(StrategyPowerTier)))}");
                                return null;
                            }

                            config.StrategyFilter.PowerTiers = parsed;
                            i++;
                        }
                        break;
                    case "--roles":
                        if (i + 1 < args.Length)
                        {
                            var parsed = ParseCsvEnums<StrategyRole>(args[i + 1]);
                            if (parsed.Count == 0)
                            {
                                Console.WriteLine($"Invalid --roles value: {args[i + 1]}");
                                Console.WriteLine($"Valid values: {string.Join(", ", Enum.GetNames(typeof(StrategyRole)))}");
                                return null;
                            }

                            config.StrategyFilter.Roles = parsed;
                            i++;
                        }
                        break;
                    case "--lifecycles":
                        if (i + 1 < args.Length)
                        {
                            var parsed = ParseCsvEnums<StrategyLifecycle>(args[i + 1]);
                            if (parsed.Count == 0)
                            {
                                Console.WriteLine($"Invalid --lifecycles value: {args[i + 1]}");
                                Console.WriteLine($"Valid values: {string.Join(", ", Enum.GetNames(typeof(StrategyLifecycle)))}");
                                return null;
                            }

                            config.StrategyFilter.Lifecycles = parsed;
                            i++;
                        }
                        break;
                    case "--difficulty-bands":
                        if (i + 1 < args.Length)
                        {
                            var parsed = ParseCsvEnums<DifficultyBand>(args[i + 1]);
                            if (parsed.Count == 0)
                            {
                                Console.WriteLine($"Invalid --difficulty-bands value: {args[i + 1]}");
                                Console.WriteLine($"Valid values: {string.Join(", ", Enum.GetNames(typeof(DifficultyBand)))}");
                                return null;
                            }

                            config.StrategyFilter.DifficultyBands = parsed;
                            i++;
                        }
                        break;
                    case "--pools":
                        if (i + 1 < args.Length)
                        {
                            var parsed = ParseCsvEnums<StrategyPool>(args[i + 1]);
                            if (parsed.Count == 0)
                            {
                                Console.WriteLine($"Invalid --pools value: {args[i + 1]}");
                                Console.WriteLine($"Valid values: {string.Join(", ", Enum.GetNames(typeof(StrategyPool)).Where(n => n != nameof(StrategyPool.None)))}");
                                return null;
                            }

                            config.StrategyFilter.Pools = parsed;
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
                    case "--no-nutrient-patches":
                    case "--disable-nutrient-patches":
                        config.EnableNutrientPatches = false;
                        break;
                    case "--no-mycovariants":
                    case "--disable-mycovariants":
                    case "--no-mycovariant-draft":
                        config.EnableMycovariantDraft = false;
                        break;
                    case "--starting-positions":
                        if (i + 1 < args.Length)
                        {
                            var parsed = ParseStartingPositions(args[i + 1]);
                            if (parsed.Count == 0)
                            {
                                Console.WriteLine($"Invalid --starting-positions value: {args[i + 1]}");
                                Console.WriteLine("Expected format: x1:y1,x2:y2,...");
                                return null;
                            }

                            config.StartingPositionOverride = parsed;
                            i++;
                        }
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

            if (config.StartingPositionOverride is { Count: > 0 } && config.StartingPositionOverride.Count != config.NumberOfPlayers)
            {
                Console.WriteLine($"--starting-positions count ({config.StartingPositionOverride.Count}) must match player count ({config.NumberOfPlayers}).");
                return null;
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

            if ((config.StrategyFilter?.IsEmpty ?? true) == false)
            {
                var filteredStrategies = AIRoster.GetStrategiesByFilter(config.StrategySet, config.StrategyFilter);
                if (filteredStrategies.Count == 0)
                {
                    Console.WriteLine($"No strategies matched the requested filters for set {config.StrategySet}.");
                    return null;
                }

                if (config.NumberOfPlayers > filteredStrategies.Count)
                {
                    Console.WriteLine($"Requested {config.NumberOfPlayers} players, but only {filteredStrategies.Count} strategies matched the requested filters for set {config.StrategySet}.");
                    return null;
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
            Console.WriteLine($"  -g, --games <number>     Number of games to play per matchup (default: {DefaultNumberOfSimulationGames})");
            Console.WriteLine($"  -p, --players <number>   Number of players/strategies to use (default: {DefaultNumberOfPlayers}, max: {MaxSupportedPlayers})");
            Console.WriteLine($"  -w, --width <number>     Board width (default: {GameBalance.BoardWidth})");
            Console.WriteLine($"  --height <number>        Board height (default: {GameBalance.BoardHeight})");
            Console.WriteLine("  -s, --strategy-set <set> Strategy set: Proven, Testing, Mycovariants, Campaign (default: Testing)");
            Console.WriteLine("  --player-counts <csv>    Batch mode list, e.g. 2,4,8");
            Console.WriteLine("  --board-sizes <csv>      Batch mode list, e.g. 80x80,160x160");
            Console.WriteLine("  --strategy-sets <csv>    Batch mode list, e.g. Testing,Proven,Mycovariants");
            Console.WriteLine("  --strategy-names <csv>   Explicit strategy names for single-run mode (overrides --players)");
            Console.WriteLine("  --archetypes <csv>       Filter roster by archetype metadata");
            Console.WriteLine("  --power-tiers <csv>      Filter roster by power tier metadata");
            Console.WriteLine("  --roles <csv>            Filter roster by role metadata");
            Console.WriteLine("  --lifecycles <csv>       Filter roster by lifecycle metadata");
            Console.WriteLine("  --difficulty-bands <csv> Filter roster by difficulty metadata");
            Console.WriteLine("  --pools <csv>            Filter roster by pool metadata");
            Console.WriteLine("  --seed <number>          Base seed for deterministic strategy/order/game seeds (default: 0)");
            Console.WriteLine("  --selection-policy <p>   Strategy sampler: RandomUnique, CoverageBalanced, StratifiedCycle (default: CoverageBalanced)");
            Console.WriteLine("  --experiment-id <id>     Identifier for this run's analytics artifacts");
            Console.WriteLine("  -o, --output <filename>  Specify output filename (default: auto-generated with timestamp)");
            Console.WriteLine("  --no-keyboard            Disable keyboard interruption (Q/Escape), useful for automation");
            Console.WriteLine("  --non-interactive        Alias for --no-keyboard");
            Console.WriteLine("  --parquet                Export canonical Parquet datasets (default: enabled)");
            Console.WriteLine("  --no-parquet             Disable Parquet export");
            Console.WriteLine("  --no-nutrient-patches    Disable nutrient patch placement");
            Console.WriteLine("  --no-mycovariants        Disable mycovariant drafting");
            Console.WriteLine("  --starting-positions     Override start positions as x1:y1,x2:y2,...");
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
            Console.WriteLine("  dotnet run --strategy-set Testing --roles Experimental --power-tiers Strong,Spike --games 50 --no-keyboard");
            Console.WriteLine("  dotnet run --experiment-id testA    # Tag outputs under experiment ID testA");
            Console.WriteLine("  dotnet run --width 50 --height 75   # Run with 50x75 board");
            Console.WriteLine("  dotnet run -w 200 -p 4              # Run 4 players on 200x100 board");
            Console.WriteLine("  dotnet run -o my_test.txt           # Run with custom output filename");
            Console.WriteLine("  dotnet run --rotate-slots --games 100 --no-keyboard  # Rotate slot assignment per game");
            Console.WriteLine("  dotnet run --games 100 --fixed-slots --no-nutrient-patches --no-mycovariants --no-keyboard");
            Console.WriteLine("  dotnet run --games 20 --starting-positions 136:95,92:126,37:123,24:65,68:34,123:37 --no-keyboard");
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
            public bool EnableNutrientPatches { get; set; }
            public bool EnableMycovariantDraft { get; set; }
            public List<(int x, int y)>? StartingPositionOverride { get; set; }
            public string ExperimentId { get; set; } = "";
            public List<int>? PlayerCounts { get; set; }
            public List<BoardSize>? BoardSizes { get; set; }
            public List<StrategySetEnum>? StrategySets { get; set; }
            public List<string>? ExplicitStrategyNames { get; set; }
            public StrategyCatalogFilter StrategyFilter { get; set; } = new();

            public bool IsBatchMode =>
                (PlayerCounts != null && PlayerCounts.Count > 0) ||
                (BoardSizes != null && BoardSizes.Count > 0) ||
                (StrategySets != null && StrategySets.Count > 0);
        }

        private static IReadOnlyList<TEnum> ParseCsvEnums<TEnum>(string csv)
            where TEnum : struct, Enum
        {
            var result = new List<TEnum>();
            var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                if (!Enum.TryParse<TEnum>(part, ignoreCase: true, out var value))
                {
                    return Array.Empty<TEnum>();
                }

                result.Add(value);
            }

            return result.Distinct().ToList();
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

        private static List<(int x, int y)> ParseStartingPositions(string csv)
        {
            var result = new List<(int x, int y)>();
            var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                var dims = part.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (dims.Length != 2 || !int.TryParse(dims[0], out var x) || !int.TryParse(dims[1], out var y))
                {
                    return new List<(int x, int y)>();
                }

                result.Add((x, y));
            }

            return result;
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
