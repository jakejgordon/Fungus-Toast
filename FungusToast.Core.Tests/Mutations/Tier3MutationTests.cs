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
        Assert.Contains("<b>Max Level Bonus:</b>", mutation.Description);
        Assert.Contains("Chemotactic Beacon", mutation.Description);
    }

    [Fact]
    public void ChemotacticBeacon_description_lists_putrefactive_mycotoxin_as_a_buff_source()
    {
        var mutation = RequireMutation(MutationIds.ChemotacticBeacon);

        Assert.Contains("Buffed by: Putrefactive Mycotoxin.", mutation.Description);
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
    public void CheckPutrefactiveMycotoxin_treats_max_level_chemotactic_beacon_as_a_range_two_toxin_source()
    {
        var board = new GameBoard(width: 7, height: 7, playerCount: 2);
        var defender = CreatePlayer(0);
        var attacker = CreatePlayer(1);
        board.Players.Add(defender);
        board.Players.Add(attacker);

        board.PlaceInitialSpore(playerId: defender.PlayerId, x: 3, y: 3);
        attacker.SetMutationLevel(MutationIds.PutrefactiveMycotoxin, newLevel: GameBalance.PutrefactiveMycotoxinMaxLevel, currentRound: 1);
        attacker.SetMutationLevel(MutationIds.ChemotacticBeacon, newLevel: 1, currentRound: 1);
        attacker.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: 1, duration: GameBalance.ChemotacticBeaconSurgeDuration);
        Assert.True(board.TryPlaceChemobeacon(attacker.PlayerId, tileId: board.GetTile(5, 5)!.TileId, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: GameBalance.ChemotacticBeaconSurgeDuration));

        var target = Assert.IsType<FungalCell>(board.GetTile(3, 3)?.FungalCell);
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
        Assert.Equal(GameBalance.PutrefactiveMycotoxinMaxLevel * GameBalance.PutrefactiveMycotoxinEffectPerLevel, chance, precision: 6);
        Assert.Equal(attacker.PlayerId, killerPlayerId);
        Assert.Equal(board.GetTile(5, 5)?.TileId, attackerTileId);
    }

    [Fact]
    public void CheckPutrefactiveMycotoxin_counts_diagonal_tiles_within_beacon_aura_range()
    {
        var board = new GameBoard(width: 7, height: 7, playerCount: 2);
        var defender = CreatePlayer(0);
        var attacker = CreatePlayer(1);
        board.Players.Add(defender);
        board.Players.Add(attacker);

        board.PlaceInitialSpore(playerId: defender.PlayerId, x: 3, y: 3);
        attacker.SetMutationLevel(MutationIds.PutrefactiveMycotoxin, newLevel: GameBalance.PutrefactiveMycotoxinMaxLevel, currentRound: 1);
        attacker.SetMutationLevel(MutationIds.ChemotacticBeacon, newLevel: 1, currentRound: 1);
        attacker.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: 1, duration: GameBalance.ChemotacticBeaconSurgeDuration);
        Assert.True(board.TryPlaceChemobeacon(attacker.PlayerId, tileId: board.GetTile(1, 1)!.TileId, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: GameBalance.ChemotacticBeaconSurgeDuration));

        var target = Assert.IsType<FungalCell>(board.GetTile(3, 3)?.FungalCell);
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
        Assert.Equal(GameBalance.PutrefactiveMycotoxinMaxLevel * GameBalance.PutrefactiveMycotoxinEffectPerLevel, chance, precision: 6);
        Assert.Equal(attacker.PlayerId, killerPlayerId);
        Assert.Equal(board.GetTile(1, 1)?.TileId, attackerTileId);
    }

    [Fact]
    public void CheckPutrefactiveMycotoxin_ignores_chemotactic_beacon_aura_below_max_level()
    {
        var board = new GameBoard(width: 7, height: 7, playerCount: 2);
        var defender = CreatePlayer(0);
        var attacker = CreatePlayer(1);
        board.Players.Add(defender);
        board.Players.Add(attacker);

        board.PlaceInitialSpore(playerId: defender.PlayerId, x: 3, y: 3);
        attacker.SetMutationLevel(MutationIds.PutrefactiveMycotoxin, newLevel: GameBalance.PutrefactiveMycotoxinMaxLevel - 1, currentRound: 1);
        attacker.SetMutationLevel(MutationIds.ChemotacticBeacon, newLevel: 1, currentRound: 1);
        attacker.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: 1, duration: GameBalance.ChemotacticBeaconSurgeDuration);
        Assert.True(board.TryPlaceChemobeacon(attacker.PlayerId, tileId: board.GetTile(5, 5)!.TileId, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: GameBalance.ChemotacticBeaconSurgeDuration));

        var target = Assert.IsType<FungalCell>(board.GetTile(3, 3)?.FungalCell);
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

        Assert.False(triggered);
        Assert.Equal(0f, chance, precision: 6);
        Assert.Null(killerPlayerId);
        Assert.Null(attackerTileId);
    }

    [Fact]
    public void CheckPutrefactiveMycotoxin_ignores_chemotactic_beacon_aura_beyond_range_two()
    {
        var board = new GameBoard(width: 8, height: 8, playerCount: 2);
        var defender = CreatePlayer(0);
        var attacker = CreatePlayer(1);
        board.Players.Add(defender);
        board.Players.Add(attacker);

        board.PlaceInitialSpore(playerId: defender.PlayerId, x: 3, y: 3);
        attacker.SetMutationLevel(MutationIds.PutrefactiveMycotoxin, newLevel: GameBalance.PutrefactiveMycotoxinMaxLevel, currentRound: 1);
        attacker.SetMutationLevel(MutationIds.ChemotacticBeacon, newLevel: 1, currentRound: 1);
        attacker.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: 1, duration: GameBalance.ChemotacticBeaconSurgeDuration);
        Assert.True(board.TryPlaceChemobeacon(attacker.PlayerId, tileId: board.GetTile(0, 0)!.TileId, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: GameBalance.ChemotacticBeaconSurgeDuration));

        var target = Assert.IsType<FungalCell>(board.GetTile(3, 3)?.FungalCell);
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

        Assert.False(triggered);
        Assert.Equal(0f, chance, precision: 6);
        Assert.Null(killerPlayerId);
        Assert.Null(attackerTileId);
    }

    [Fact]
    public void CheckPutrefactiveMycotoxin_combines_adjacent_and_beacon_sources_into_total_chance()
    {
        var board = new GameBoard(width: 7, height: 7, playerCount: 2);
        var defender = CreatePlayer(0);
        var attacker = CreatePlayer(1);
        board.Players.Add(defender);
        board.Players.Add(attacker);

        board.PlaceInitialSpore(playerId: defender.PlayerId, x: 3, y: 3);
        board.PlaceInitialSpore(playerId: attacker.PlayerId, x: 3, y: 4);
        attacker.SetMutationLevel(MutationIds.PutrefactiveMycotoxin, newLevel: GameBalance.PutrefactiveMycotoxinMaxLevel, currentRound: 1);
        attacker.SetMutationLevel(MutationIds.ChemotacticBeacon, newLevel: 1, currentRound: 1);
        attacker.ActiveSurges[MutationIds.ChemotacticBeacon] = new Player.ActiveSurgeInfo(MutationIds.ChemotacticBeacon, level: 1, duration: GameBalance.ChemotacticBeaconSurgeDuration);
        Assert.True(board.TryPlaceChemobeacon(attacker.PlayerId, tileId: board.GetTile(5, 5)!.TileId, mutationId: MutationIds.ChemotacticBeacon, turnsRemaining: GameBalance.ChemotacticBeaconSurgeDuration));

        var target = Assert.IsType<FungalCell>(board.GetTile(3, 3)?.FungalCell);
        var triggered = FungicideMutationProcessor.CheckPutrefactiveMycotoxin(
            target,
            board,
            board.Players,
            roll: GameBalance.PutrefactiveMycotoxinMaxLevel * GameBalance.PutrefactiveMycotoxinEffectPerLevel,
            out var chance,
            out var killerPlayerId,
            out var attackerTileId,
            rng: new Random(0),
            observer: new TestSimulationObserver());

        float singleSourceChance = GameBalance.PutrefactiveMycotoxinMaxLevel * GameBalance.PutrefactiveMycotoxinEffectPerLevel;

        Assert.True(triggered);
        Assert.Equal(singleSourceChance * 2f, chance, precision: 6);
        Assert.Equal(attacker.PlayerId, killerPlayerId);
        Assert.Equal(board.GetTile(5, 5)?.TileId, attackerTileId);
    }

    [Fact]
    public void AnabolicInversion_is_tier3_genetic_drift_and_requires_adaptive_expression_level_three()
    {
        var mutation = RequireMutation(MutationIds.AnabolicInversion);

        Assert.Equal(MutationCategory.GeneticDrift, mutation.Category);
        Assert.Equal(MutationTier.Tier3, mutation.Tier);
        Assert.Equal(MutationType.BonusMutationPointChance, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.AdaptiveExpression, prereq.MutationId);
        Assert.Equal(3, prereq.RequiredLevel);
        Assert.Contains("When you trail in living cells", mutation.Description);
        Assert.Contains("Max Level Bonus", mutation.Description);
        Assert.Contains("Mycotoxin Catabolism", mutation.Description);
    }

    [Fact]
    public void MimeticResilience_is_tier3_mycelial_surge_and_requires_homeostatic_harmony_five_and_mycotoxin_tracer_three()
    {
        var mutation = RequireMutation(MutationIds.MimeticResilience);

        Assert.Equal(MutationCategory.MycelialSurges, mutation.Category);
        Assert.Equal(MutationTier.Tier3, mutation.Tier);
        Assert.True(mutation.IsSurge);
        Assert.Equal(MutationType.MimeticResilience, mutation.Type);
        Assert.Equal(2, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.HomeostaticHarmony && p.RequiredLevel == 5);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.MycotoxinTracer && p.RequiredLevel == 3);
        Assert.Contains("20.0%+ more living cells", mutation.Description);
        Assert.Contains("1.0%+ board control", mutation.Description);
        Assert.DoesNotContain(" %", mutation.Description);
        Assert.Contains("Prefers infesting enemy cells over empty placement", mutation.Description);
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
    public void OnPostGrowthPhase_mimetic_resilience_prefers_enemy_living_cells_and_emits_resistance_batch_event()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2);
        var actingPlayer = CreatePlayer(0);
        var targetPlayer = CreatePlayer(1);
        board.Players.Add(actingPlayer);
        board.Players.Add(targetPlayer);

        SeedLivingCells(board, actingPlayer, new[] { 0, 1, 2, 3, 4 });
        actingPlayer.SetMutationLevel(MutationIds.MimeticResilience, newLevel: 1, currentRound: 1);
        actingPlayer.ActiveSurges[MutationIds.MimeticResilience] = new Player.ActiveSurgeInfo(MutationIds.MimeticResilience, level: 1, duration: GameBalance.MimeticResilienceSurgeDuration);

        // Keep exactly one enemy living target within the resistant source radius so the outcome is deterministic.
        SeedLivingCells(board, targetPlayer, new[] { 6, 20, 21, 22, 23, 24 });
        var resistantSource = Assert.IsType<FungalCell>(board.GetCell(6));
        resistantSource.MakeResistant();

        var contestedEnemyTileId = 12;
        var contestedEnemyCell = new FungalCell(ownerPlayerId: targetPlayer.PlayerId, tileId: contestedEnemyTileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        board.PlaceFungalCell(contestedEnemyCell);
        targetPlayer.AddControlledTile(contestedEnemyTileId);

        int? resistancePlayerId = null;
        GrowthSource? resistanceSourceType = null;
        IReadOnlyList<int>? resistanceTileIds = null;
        board.ResistanceAppliedBatch += (playerId, source, tileIds) =>
        {
            resistancePlayerId = playerId;
            resistanceSourceType = source;
            resistanceTileIds = tileIds;
        };

        MycelialSurgeMutationProcessor.OnPostGrowthPhase_MimeticResilience(
            board,
            board.Players,
            new AlwaysZeroRandom(),
            new TestSimulationObserver());

        var convertedCell = Assert.IsType<FungalCell>(board.GetCell(contestedEnemyTileId));
        Assert.Equal(actingPlayer.PlayerId, convertedCell.OwnerPlayerId);
        Assert.True(convertedCell.IsAlive);
        Assert.True(convertedCell.IsResistant);
        Assert.Equal(GrowthSource.MimeticResilience, convertedCell.SourceOfGrowth);
        Assert.Equal(actingPlayer.PlayerId, resistancePlayerId);
        Assert.Equal(GrowthSource.MimeticResilience, resistanceSourceType);
        Assert.Equal(new[] { contestedEnemyTileId }, resistanceTileIds);
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

    [Fact]
    public void IsCompetitiveAntagonismActive_requires_both_mutation_level_and_active_surge()
    {
        var player = CreatePlayer();

        Assert.False(MycelialSurgeMutationProcessor.IsCompetitiveAntagonismActive(player));

        player.SetMutationLevel(MutationIds.CompetitiveAntagonism, newLevel: 1, currentRound: 1);
        Assert.False(MycelialSurgeMutationProcessor.IsCompetitiveAntagonismActive(player));

        player.ActiveSurges[MutationIds.CompetitiveAntagonism] = new Player.ActiveSurgeInfo(MutationIds.CompetitiveAntagonism, level: 1, duration: GameBalance.CompetitiveAntagonismSurgeDuration);
        Assert.True(MycelialSurgeMutationProcessor.IsCompetitiveAntagonismActive(player));
    }

    [Fact]
    public void GetCompetitiveAntagonismLevel_returns_zero_without_active_surge_and_mutation_level_when_active()
    {
        var player = CreatePlayer();
        player.SetMutationLevel(MutationIds.CompetitiveAntagonism, newLevel: 2, currentRound: 1);

        Assert.Equal(0, MycelialSurgeMutationProcessor.GetCompetitiveAntagonismLevel(player));

        player.ActiveSurges[MutationIds.CompetitiveAntagonism] = new Player.ActiveSurgeInfo(MutationIds.CompetitiveAntagonism, level: 2, duration: GameBalance.CompetitiveAntagonismSurgeDuration);
        Assert.Equal(2, MycelialSurgeMutationProcessor.GetCompetitiveAntagonismLevel(player));
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
