using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class Tier3MutationTests
{
    [Fact]
    public void Necrosporulation_is_tier3_cellular_resilience_and_requires_chronoresilient_cytoplasm_level_five()
    {
        var mutation = RequireMutation(MutationIds.Necrosporulation);

        Assert.Equal(MutationCategory.CellularResilience, mutation.Category);
        Assert.Equal(MutationTier.Tier3, mutation.Tier);
        Assert.Equal(MutationType.Necrosporulation, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.ChronoresilientCytoplasm, prereq.MutationId);
        Assert.Equal(5, prereq.RequiredLevel);
    }

    [Fact]
    public void TryTriggerSporeOnDeath_spawns_a_necrospore_on_an_open_tile_when_roll_hits()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var player = CreatePlayer();
        board.Players.Add(player);
        board.PlaceInitialSpore(playerId: 0, x: 1, y: 1);
        player.SetMutationLevel(MutationIds.Necrosporulation, newLevel: 5, currentRound: 1);
        var observer = new TestSimulationObserver();

        board.TryTriggerSporeOnDeath(player, new AlwaysZeroRandom(), observer);

        var livingCells = board.GetAllCellsOwnedBy(player.PlayerId).Where(c => c.IsAlive).ToList();
        Assert.Equal(2, livingCells.Count);
        Assert.Contains(livingCells, cell => cell.SourceOfGrowth == GrowthSource.Necrosporulation);
    }

    [Fact]
    public void MycotropicInduction_is_tier3_growth_and_requires_all_four_tendrils()
    {
        var mutation = RequireMutation(MutationIds.MycotropicInduction);

        Assert.Equal(MutationCategory.Growth, mutation.Category);
        Assert.Equal(MutationTier.Tier3, mutation.Tier);
        Assert.Equal(MutationType.TendrilDirectionalMultiplier, mutation.Type);
        Assert.Equal(4, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.TendrilNorthwest && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.TendrilNortheast && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.TendrilSoutheast && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.TendrilSouthwest && p.RequiredLevel == 1);
    }

    [Fact]
    public void GetTendrilDiagonalGrowthMultiplier_includes_mycotropic_induction_levels()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.MycotropicInduction, newLevel: 3, currentRound: 1);

        var multiplier = GrowthMutationProcessor.GetTendrilDiagonalGrowthMultiplier(player);

        Assert.Equal(1f + (3 * GameBalance.MycotropicInductionEffectPerLevel), multiplier, precision: 6);
    }

    [Fact]
    public void PutrefactiveMycotoxin_is_tier3_fungicide_and_requires_mycotoxin_potentiation_level_one()
    {
        var mutation = RequireMutation(MutationIds.PutrefactiveMycotoxin);

        Assert.Equal(MutationCategory.Fungicide, mutation.Category);
        Assert.Equal(MutationTier.Tier3, mutation.Tier);
        Assert.Equal(MutationType.AdjacentFungicide, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.MycotoxinPotentiation, prereq.MutationId);
        Assert.Equal(1, prereq.RequiredLevel);
    }

    [Fact]
    public void CheckPutrefactiveMycotoxin_returns_combined_adjacent_chance_and_identifies_the_only_attacker()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);
        var defender = CreatePlayer(0);
        var attacker = CreatePlayer(1);
        board.Players.Add(defender);
        board.Players.Add(attacker);

        board.PlaceInitialSpore(playerId: defender.PlayerId, x: 1, y: 1);
        board.PlaceInitialSpore(playerId: attacker.PlayerId, x: 1, y: 2);
        attacker.SetMutationLevel(MutationIds.PutrefactiveMycotoxin, newLevel: 2, currentRound: 1);

        var target = Assert.IsType<FungalCell>(board.GetTile(1, 1)?.FungalCell);
        var triggered = FungicideMutationProcessor.CheckPutrefactiveMycotoxin(
            target,
            board,
            board.Players,
            roll: 0.0,
            out var chance,
            out var killerPlayerId,
            out var attackerTileId,
            rng: new Random(0),
            observer: new TestSimulationObserver());

        Assert.True(triggered);
        Assert.Equal(2 * GameBalance.PutrefactiveMycotoxinEffectPerLevel, chance, precision: 6);
        Assert.Equal(attacker.PlayerId, killerPlayerId);
        Assert.Equal(board.GetTile(1, 2)?.TileId, attackerTileId);
    }

    [Fact]
    public void CompetitiveAntagonism_is_tier3_mycelial_surge_and_requires_mycotoxin_tracer_level_fifteen()
    {
        var mutation = RequireMutation(MutationIds.CompetitiveAntagonism);

        Assert.Equal(MutationCategory.MycelialSurges, mutation.Category);
        Assert.Equal(MutationTier.Tier3, mutation.Tier);
        Assert.True(mutation.IsSurge);
        Assert.Equal(MutationType.CompetitiveAntagonism, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.MycotoxinTracer, prereq.MutationId);
        Assert.Equal(15, prereq.RequiredLevel);
    }

    [Fact]
    public void GetCompetitiveAntagonismTargets_returns_only_larger_colonies_in_descending_size_order()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 4);
        var current = CreatePlayer(0);
        var larger = CreatePlayer(1);
        var smaller = CreatePlayer(2);
        var largest = CreatePlayer(3);
        board.Players.AddRange(new[] { current, larger, smaller, largest });

        SeedLivingCells(board, current, new[] { 0, 1, 2 });
        SeedLivingCells(board, larger, new[] { 5, 6, 7, 8 });
        SeedLivingCells(board, smaller, new[] { 10, 11 });
        SeedLivingCells(board, largest, new[] { 15, 16, 17, 18, 19 });

        var targets = MycelialSurgeMutationProcessor.GetCompetitiveAntagonismTargets(current, board.Players, board);

        Assert.Equal(new[] { largest.PlayerId, larger.PlayerId }, targets.Select(p => p.PlayerId).ToArray());
    }

    private static void SeedLivingCells(GameBoard board, Player player, IEnumerable<int> tileIds)
    {
        foreach (var tileId in tileIds)
        {
            var cell = new FungalCell(ownerPlayerId: player.PlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
            board.PlaceFungalCell(cell);
            player.AddControlledTile(tileId);
        }
    }

    private static Player CreatePlayer(int playerId = 0)
    {
        return new Player(playerId: playerId, playerName: $"P{playerId}", playerType: PlayerTypeEnum.AI);
    }

    private sealed class AlwaysZeroRandom : Random
    {
        protected override double Sample() => 0.0;
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
    }
}
