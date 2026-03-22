using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System.Reflection;

namespace FungusToast.Core.Tests.Mutations;

public class Tier1MycotoxinTracerTests
{
    [Fact]
    public void MycotoxinTracer_is_a_root_fungicide_mutation_with_no_prerequisites()
    {
        var mutation = RequireMutation(MutationIds.MycotoxinTracer);

        Assert.Equal(MutationCategory.Fungicide, mutation.Category);
        Assert.Equal(MutationTier.Tier1, mutation.Tier);
        Assert.Equal(MutationType.FungicideToxinSpores, mutation.Type);
        Assert.Empty(mutation.Prerequisites);
        Assert.Contains(mutation.Id, MutationRegistry.Roots.Keys);
    }

    [Fact]
    public void MycotoxinTracer_root_upgrade_is_allowed_on_round_one_when_points_are_available()
    {
        var player = CreatePlayer(mutationPoints: 10);
        var mutation = RequireMutation(MutationIds.MycotoxinTracer);

        var canUpgrade = player.CanUpgrade(mutation, currentRound: 1);

        Assert.True(canUpgrade, $"Expected root mutation {mutation.Name} to be upgradable without prerequisites.");
    }

    [Fact]
    public void Upgrading_mycotoxin_tracer_sets_prereq_met_round_for_mycotoxin_potentiation_when_threshold_is_reached()
    {
        var player = CreatePlayer(mutationPoints: 99);
        var observer = new TestSimulationObserver();
        var tracer = RequireMutation(MutationIds.MycotoxinTracer);

        player.SetMutationLevel(tracer.Id, newLevel: 4, currentRound: 1);
        var upgraded = player.TryUpgradeMutation(tracer, observer, currentRound: 2);

        Assert.True(upgraded);
        Assert.Equal(5, player.GetMutationLevel(tracer.Id));
        Assert.Equal(2, Assert.IsType<PlayerMutation>(player.PlayerMutations[MutationIds.MycotoxinPotentiation]).PrereqMetRound);
    }

    [Fact]
    public void ApplyMycotoxinTracer_returns_zero_when_player_has_no_tracer_level()
    {
        var board = CreateBoardWithTwoPlayers(out var player0, out var player1);
        board.PlaceInitialSpore(playerId: 1, x: 2, y: 2);
        var observer = new TestSimulationObserver();
        var context = new DecayPhaseContext(board, new List<Player> { player0, player1 });

        var placed = FungicideMutationProcessor.ApplyMycotoxinTracer(
            player0,
            board,
            failedGrowthsThisRound: 10,
            livingCells: 1,
            opponentCount: 1,
            allPlayers: new List<Player> { player0, player1 },
            rng: new Random(123),
            observer,
            context);

        Assert.Equal(0, placed);
    }

    [Fact]
    public void CalculateMycotoxinTracerToxinCount_is_capped_by_board_size_limit()
    {
        var board = new GameBoard(width: 6, height: 6, playerCount: 1); // 36 tiles => cap 0 by current divisor logic
        var player = CreatePlayer();
        int level = 50;
        int failedGrowths = 1000;
        int opponentCount = 8;
        int livingCells = 1;

        int count = InvokeCalculateMycotoxinTracerToxinCount(player, board, level, failedGrowths, opponentCount, livingCells, new Random(123));

        Assert.Equal(board.TotalTiles / GameBalance.MycotoxinTracerMaxToxinsDivisor, count);
    }

    [Fact]
    public void ApplyMycotoxinTracer_places_no_more_toxins_than_available_candidate_tiles()
    {
        var board = CreateBoardWithTwoPlayers(out var player0, out var player1);
        player0.SetMutationLevel(MutationIds.MycotoxinTracer, newLevel: 50, currentRound: 1);
        board.PlaceInitialSpore(playerId: 1, x: 2, y: 2);
        board.PlaceInitialSpore(playerId: 1, x: 4, y: 4);
        var observer = new TestSimulationObserver();
        var context = new DecayPhaseContext(board, new List<Player> { player0, player1 });
        int candidateCount = ToxinHelper.FindMycotoxinTargetTiles(board, player0).Count;

        var placed = FungicideMutationProcessor.ApplyMycotoxinTracer(
            player0,
            board,
            failedGrowthsThisRound: 1000,
            livingCells: 1,
            opponentCount: 1,
            allPlayers: new List<Player> { player0, player1 },
            rng: new Random(123),
            observer,
            context);

        Assert.InRange(placed, 0, candidateCount);
    }

    private static int InvokeCalculateMycotoxinTracerToxinCount(Player player, GameBoard board, int level, int failedGrowthsThisRound, int opponentCount, int livingCells, Random rng)
    {
        var method = typeof(FungicideMutationProcessor).GetMethod(
            "CalculateMycotoxinTracerToxinCount",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(null, new object[] { player, board, level, failedGrowthsThisRound, opponentCount, livingCells, rng });
        return Assert.IsType<int>(result);
    }

    private static Player CreatePlayer(int playerId = 0, int mutationPoints = 0)
    {
        return new Player(playerId: playerId, playerName: $"P{playerId}", playerType: PlayerTypeEnum.AI)
        {
            MutationPoints = mutationPoints
        };
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
    }

    private static GameBoard CreateBoardWithTwoPlayers(out Player player0, out Player player1)
    {
        var board = new GameBoard(width: 8, height: 8, playerCount: 2);
        player0 = CreatePlayer(0);
        player1 = CreatePlayer(1);
        board.Players.Add(player0);
        board.Players.Add(player1);
        return board;
    }
}
