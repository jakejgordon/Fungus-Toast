using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using System.Reflection;

namespace FungusToast.Core.Tests.Mutations;

public class Tier4MutationTests
{
    [Fact]
    public void RegenerativeHyphae_is_tier4_growth_and_requires_necrosporulation_two_and_mycotropic_induction_one()
    {
        var mutation = RequireMutation(MutationIds.RegenerativeHyphae);

        Assert.Equal(MutationCategory.Growth, mutation.Category);
        Assert.Equal(MutationTier.Tier4, mutation.Tier);
        Assert.Equal(MutationType.ReclaimOwnDeadCells, mutation.Type);
        Assert.Equal(2, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.Necrosporulation && p.RequiredLevel == 2);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.MycotropicInduction && p.RequiredLevel == 1);
        Assert.Contains("Only one attempt can be made on each dead cell per round", mutation.Description);
    }

    [Fact]
    public void HypersystemicRegeneration_is_tier7_cellular_resilience_and_requires_catabolic_rebirth_one_and_mycotropic_induction_one()
    {
        var mutation = RequireMutation(MutationIds.HypersystemicRegeneration);

        Assert.Equal(MutationCategory.CellularResilience, mutation.Category);
        Assert.Equal(MutationTier.Tier7, mutation.Tier);
        Assert.Equal(MutationType.HypersystemicRegeneration, mutation.Type);
        Assert.Equal(2, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.CatabolicRebirth && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.MycotropicInduction && p.RequiredLevel == 1);
        Assert.Contains("Regenerative Hyphae can also reclaim diagonally adjacent cells", mutation.Description);
    }

    [Fact]
    public void OnPostGrowthPhase_regenerative_hyphae_reclaims_adjacent_own_dead_cells_but_not_enemy_dead_cells()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);
        var player = CreatePlayer(0);
        var enemy = CreatePlayer(1);
        board.Players.Add(player);
        board.Players.Add(enemy);

        player.SetMutationLevel(MutationIds.RegenerativeHyphae, newLevel: 5, currentRound: 1);

        PlaceLivingCell(board, player, tileId: 4);
        CreateDeadCell(board, player, tileId: 1);
        CreateDeadCell(board, enemy, tileId: 7);

        var observer = new CountingObserver();

        CellularResilienceMutationProcessor.OnPostGrowthPhase_RegenerativeHyphae(board, board.Players, new AlwaysZeroRandom(), observer);

        var reclaimedCell = Assert.IsType<FungalCell>(board.GetCell(1));
        Assert.True(reclaimedCell.IsAlive);
        Assert.Equal(player.PlayerId, reclaimedCell.OwnerPlayerId);
        Assert.Equal(GrowthSource.RegenerativeHyphae, reclaimedCell.SourceOfGrowth);

        var enemyDeadCell = Assert.IsType<FungalCell>(board.GetCell(7));
        Assert.True(enemyDeadCell.IsDead);
        Assert.Equal(enemy.PlayerId, enemyDeadCell.OwnerPlayerId);
        Assert.Equal(1, observer.RegenerativeHyphaeReclaims);
    }

    [Fact]
    public void OnPostGrowthPhase_regenerative_hyphae_only_attempts_each_dead_cell_once_even_when_multiple_living_neighbors_touch_it()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var player = CreatePlayer(0);
        board.Players.Add(player);

        player.SetMutationLevel(MutationIds.RegenerativeHyphae, newLevel: 5, currentRound: 1);
        player.AddMycovariant(new Mycovariant { Id = MycovariantIds.ReclamationRhizomorphsId, Name = "Reclamation Rhizomorphs", AutoMarkTriggered = true });

        PlaceLivingCell(board, player, tileId: 1);
        PlaceLivingCell(board, player, tileId: 3);
        PlaceLivingCell(board, player, tileId: 5);
        PlaceLivingCell(board, player, tileId: 7);
        CreateDeadCell(board, player, tileId: 4);

        var observer = new CountingObserver();
        var rng = new CountingSequenceRandom(0.999999, 0.0, 0.999999);

        CellularResilienceMutationProcessor.OnPostGrowthPhase_RegenerativeHyphae(board, board.Players, rng, observer);

        var deadCell = Assert.IsType<FungalCell>(board.GetCell(4));
        Assert.True(deadCell.IsDead);
        Assert.Equal(3, rng.NextDoubleCallCount);
        Assert.Equal(0, observer.RegenerativeHyphaeReclaims);
    }

    [Fact]
    public void OnPostGrowthPhase_hypersystemic_regeneration_at_max_level_reclaims_diagonal_dead_cells_and_tracks_diagonal_reclaim()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var player = CreatePlayer(0);
        board.Players.Add(player);

        player.SetMutationLevel(MutationIds.RegenerativeHyphae, newLevel: 5, currentRound: 1);
        player.SetMutationLevel(MutationIds.HypersystemicRegeneration, newLevel: GameBalance.HypersystemicRegenerationMaxLevel, currentRound: 1);

        PlaceLivingCell(board, player, tileId: 0);
        CreateDeadCell(board, player, tileId: 4);

        var observer = new CountingObserver();

        CellularResilienceMutationProcessor.OnPostGrowthPhase_RegenerativeHyphae(board, board.Players, new AlwaysZeroRandom(), observer);

        var reclaimedCell = Assert.IsType<FungalCell>(board.GetCell(4));
        Assert.True(reclaimedCell.IsAlive);
        Assert.Equal(player.PlayerId, reclaimedCell.OwnerPlayerId);
        Assert.Equal(1, observer.RegenerativeHyphaeReclaims);
        Assert.Equal(1, observer.HypersystemicDiagonalReclaims);
        Assert.Equal(1, observer.HypersystemicResistanceApplications);
        Assert.True(reclaimedCell.IsResistant);
    }

    [Fact]
    public void OnPostGrowthPhase_hypersystemic_regeneration_below_max_level_does_not_reclaim_diagonal_dead_cells()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var player = CreatePlayer(0);
        board.Players.Add(player);

        player.SetMutationLevel(MutationIds.RegenerativeHyphae, newLevel: 5, currentRound: 1);
        player.SetMutationLevel(MutationIds.HypersystemicRegeneration, newLevel: GameBalance.HypersystemicRegenerationMaxLevel - 1, currentRound: 1);

        PlaceLivingCell(board, player, tileId: 0);
        CreateDeadCell(board, player, tileId: 4);

        var observer = new CountingObserver();

        CellularResilienceMutationProcessor.OnPostGrowthPhase_RegenerativeHyphae(board, board.Players, new AlwaysZeroRandom(), observer);

        var deadCell = Assert.IsType<FungalCell>(board.GetCell(4));
        Assert.True(deadCell.IsDead);
        Assert.Equal(0, observer.RegenerativeHyphaeReclaims);
        Assert.Equal(0, observer.HypersystemicDiagonalReclaims);
    }

    [Fact]
    public void CreepingMold_is_tier4_growth_and_requires_mycotropic_induction_three()
    {
        var mutation = RequireMutation(MutationIds.CreepingMold);

        Assert.Equal(MutationCategory.Growth, mutation.Category);
        Assert.Equal(MutationTier.Tier4, mutation.Tier);
        Assert.Equal(MutationType.CreepingMovementOnFailedGrowth, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.MycotropicInduction, prereq.MutationId);
        Assert.Equal(3, prereq.RequiredLevel);
        Assert.Contains("Max Level Bonus", mutation.Description);
        Assert.Contains("jump over one blocking toxin", mutation.Description);
    }

    [Fact]
    public void ReclaimCellHelper_succeeds_with_hypersystemic_boosted_regenerative_chance_where_base_regenerative_chance_would_fail()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var player = CreatePlayer(0);
        board.Players.Add(player);
        CreateDeadCell(board, player, tileId: 4);

        float baseChance = 5 * GameBalance.RegenerativeHyphaeReclaimChance;
        float boostedChance = baseChance * (1f + (GameBalance.HypersystemicRegenerationMaxLevel * GameBalance.HypersystemicRegenerationEffectivenessBonus));
        const double rollBetweenBaseAndBoosted = 0.153;

        Assert.True(rollBetweenBaseAndBoosted > baseChance);
        Assert.True(rollBetweenBaseAndBoosted < boostedChance);

        bool reclaimed = ReclaimCellHelper.TryReclaimDeadCell(
            board,
            player,
            tileId: 4,
            boostedChance,
            new CountingSequenceRandom(rollBetweenBaseAndBoosted),
            GrowthSource.RegenerativeHyphae,
            new CountingObserver());

        Assert.True(reclaimed);
        Assert.True(board.GetCell(4)!.IsAlive);
    }

    [Fact]
    public void TryCreepingMoldMove_moves_into_open_target_when_roll_hits_and_target_is_not_more_enclosed()
    {
        var board = new GameBoard(width: 4, height: 4, playerCount: 1);
        var player = CreatePlayer(0);
        board.Players.Add(player);
        player.SetMutationLevel(MutationIds.CreepingMold, newLevel: 1, currentRound: 1);

        var sourceCell = PlaceLivingCell(board, player, tileId: board.GetTile(0, 1)!.TileId);
        var sourceTile = board.GetTileById(sourceCell.TileId)!;
        var targetTile = board.GetTile(1, 1)!;

        var moved = GrowthMutationProcessor.TryCreepingMoldMove(
            player,
            sourceCell,
            sourceTile,
            targetTile,
            new AlwaysZeroRandom(),
            board,
            new CountingObserver());

        Assert.True(moved);
        Assert.Null(board.GetCell(sourceTile.TileId));
        var movedCell = Assert.IsType<FungalCell>(board.GetCell(targetTile.TileId));
        Assert.Equal(player.PlayerId, movedCell.OwnerPlayerId);
        Assert.Equal(GrowthSource.CreepingMold, movedCell.SourceOfGrowth);
    }

    [Fact]
    public void TryCreepingMoldMove_at_max_level_can_jump_over_adjacent_toxin_into_open_tile_beyond()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2);
        var player = CreatePlayer(0);
        var enemy = CreatePlayer(1);
        board.Players.Add(player);
        board.Players.Add(enemy);
        player.SetMutationLevel(MutationIds.CreepingMold, newLevel: GameBalance.CreepingMoldMaxLevel, currentRound: 1);

        var sourceCell = PlaceLivingCell(board, player, tileId: board.GetTile(1, 2)!.TileId);
        var sourceTile = board.GetTileById(sourceCell.TileId)!;
        var toxinTile = board.GetTile(2, 2)!;
        var landingTile = board.GetTile(3, 2)!;
        ToxinHelper.ConvertToToxin(board, toxinTile.TileId, GrowthSource.Manual, enemy);

        var observer = new CountingObserver();
        var moved = GrowthMutationProcessor.TryCreepingMoldMove(
            player,
            sourceCell,
            sourceTile,
            toxinTile,
            new AlwaysZeroRandom(),
            board,
            observer);

        Assert.True(moved);
        Assert.Null(board.GetCell(sourceTile.TileId));
        var movedCell = Assert.IsType<FungalCell>(board.GetCell(landingTile.TileId));
        Assert.Equal(player.PlayerId, movedCell.OwnerPlayerId);
        Assert.Equal(GrowthSource.CreepingMold, movedCell.SourceOfGrowth);
        Assert.Equal(1, observer.CreepingMoldToxinJumps);
    }

    [Fact]
    public void SporicidalBloom_is_tier4_fungicide_and_requires_putrefactive_mycotoxin_one_and_mycelial_bloom_seven()
    {
        var mutation = RequireMutation(MutationIds.SporicidalBloom);

        Assert.Equal(MutationCategory.Fungicide, mutation.Category);
        Assert.Equal(MutationTier.Tier4, mutation.Tier);
        Assert.Equal(MutationType.FungicideSporeDrop, mutation.Type);
        Assert.Equal(2, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.PutrefactiveMycotoxin && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.MycelialBloom && p.RequiredLevel == 7);
        Assert.Contains("Removes 25% of empty tiles", mutation.Description);
    }

    [Fact]
    public void OnDecayPhase_sporicidal_bloom_kills_and_toxifies_the_only_available_enemy_cell()
    {
        var board = new GameBoard(width: 4, height: 4, playerCount: 2);
        var player = CreatePlayer(0);
        var enemy = CreatePlayer(1);
        board.Players.Add(player);
        board.Players.Add(enemy);
        player.SetMutationLevel(MutationIds.SporicidalBloom, newLevel: 1, currentRound: 1);

        for (int tileId = 0; tileId <= 12; tileId++)
        {
            PlaceLivingCell(board, player, tileId);
        }

        PlaceLivingCell(board, enemy, tileId: 13);
        CreateDeadCell(board, player, tileId: 14);
        CreateDeadCell(board, player, tileId: 15);

        var observer = new CountingObserver();

        FungicideMutationProcessor.OnDecayPhase_SporicidalBloom(
            board,
            board.Players,
            new AlwaysZeroRandom(),
            observer,
            new DecayPhaseContext(board, board.Players));

        var targetCell = Assert.IsType<FungalCell>(board.GetCell(13));
        Assert.True(targetCell.IsToxin);
        Assert.Equal(player.PlayerId, targetCell.OwnerPlayerId);
        Assert.Equal(1, observer.SporicidalReports);
        Assert.Equal(1, observer.LastSporicidalDropCount);
    }

    [Fact]
    public void OnDecayPhase_sporicidal_bloom_toxifies_the_only_available_empty_tile()
    {
        var board = new GameBoard(width: 4, height: 4, playerCount: 1);
        var player = CreatePlayer(0);
        board.Players.Add(player);
        player.SetMutationLevel(MutationIds.SporicidalBloom, newLevel: 1, currentRound: 1);

        for (int tileId = 0; tileId <= 11; tileId++)
        {
            PlaceLivingCell(board, player, tileId);
        }

        CreateDeadCell(board, player, tileId: 12);
        CreateDeadCell(board, player, tileId: 13);
        CreateDeadCell(board, player, tileId: 14);
        // tile 15 remains the only available empty tile

        var observer = new CountingObserver();

        FungicideMutationProcessor.OnDecayPhase_SporicidalBloom(
            board,
            board.Players,
            new AlwaysZeroRandom(),
            observer,
            new DecayPhaseContext(board, board.Players));

        var targetCell = Assert.IsType<FungalCell>(board.GetCell(15));
        Assert.True(targetCell.IsToxin);
        Assert.Equal(player.PlayerId, targetCell.OwnerPlayerId);
        Assert.Equal(1, observer.SporicidalReports);
        Assert.Equal(1, observer.LastSporicidalDropCount);
    }

    [Fact]
    public void ApplySporicidalBloomMaxLevelBonus_removes_one_quarter_of_empty_tiles_and_keeps_enemy_tiles()
    {
        var board = new GameBoard(width: 3, height: 2, playerCount: 2);
        var player = CreatePlayer(0);
        var enemy = CreatePlayer(1);
        board.Players.Add(player);
        board.Players.Add(enemy);

        PlaceLivingCell(board, enemy, tileId: 0);
        PlaceLivingCell(board, enemy, tileId: 1);
        // tiles 2,3,4,5 remain empty => 4 empties, so max-level bonus should remove exactly 1

        var availableTiles = board.AllTiles().ToList();
        var method = typeof(FungicideMutationProcessor).GetMethod("ApplySporicidalBloomMaxLevelBonus", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = Assert.IsType<List<BoardTile>>(method!.Invoke(null, new object[] { availableTiles, new AlwaysZeroRandom() }));

        Assert.Equal(5, result.Count);
        Assert.Contains(result, t => t.TileId == 0);
        Assert.Contains(result, t => t.TileId == 1);
        Assert.Equal(3, result.Count(t => !t.IsOccupiedForSporePlacement));
    }

    [Fact]
    public void ApplyCompetitiveAntagonismSporicidalBloomTargeting_prioritizes_larger_colony_tiles_and_prunes_most_smaller_colony_tiles()
    {
        var board = new GameBoard(width: 6, height: 6, playerCount: 3);
        var current = CreatePlayer(0);
        var larger = CreatePlayer(1);
        var smaller = CreatePlayer(2);
        board.Players.AddRange(new[] { current, larger, smaller });

        // Current colony: 12 living cells.
        for (int tileId = 0; tileId <= 11; tileId++)
        {
            PlaceLivingCell(board, current, tileId);
        }

        // Larger colony: 13 living cells, four of which are included in the candidate pool.
        for (int tileId = 12; tileId <= 24; tileId++)
        {
            PlaceLivingCell(board, larger, tileId);
        }

        // Smaller colony: 4 living cells, all included in the candidate pool.
        PlaceLivingCell(board, smaller, tileId: 25);
        PlaceLivingCell(board, smaller, tileId: 26);
        PlaceLivingCell(board, smaller, tileId: 27);
        PlaceLivingCell(board, smaller, tileId: 28);

        var availableTiles = new List<BoardTile>
        {
            board.GetTileById(12)!,
            board.GetTileById(13)!,
            board.GetTileById(14)!,
            board.GetTileById(15)!,
            board.GetTileById(25)!,
            board.GetTileById(26)!,
            board.GetTileById(27)!,
            board.GetTileById(28)!,
            board.GetTileById(29)!,
            board.GetTileById(30)!,
            board.GetTileById(31)!
        };

        var method = typeof(FungicideMutationProcessor).GetMethod("ApplyCompetitiveAntagonismSporicidalBloomTargeting", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = Assert.IsType<List<BoardTile>>(method!.Invoke(null, new object[] { availableTiles, current, board.Players, board, new AlwaysZeroRandom(), new DecayPhaseContext(board, board.Players) }));

        Assert.Equal(new[] { 12, 13, 14, 15 }, result.Take(4).Select(t => t.TileId).ToArray());
        Assert.Single(new[] { 25, 26, 27, 28 }.Where(id => result.Any(t => t.TileId == id)));
    }

    private static Mutation RequireMutation(int mutationId)
    {
        var mutation = MutationRegistry.GetById(mutationId);
        return Assert.IsType<Mutation>(mutation);
    }

    private static Player CreatePlayer(int playerId = 0)
    {
        return new Player(playerId: playerId, playerName: $"P{playerId}", playerType: PlayerTypeEnum.AI);
    }

    private static FungalCell PlaceLivingCell(GameBoard board, Player player, int tileId)
    {
        var cell = new FungalCell(ownerPlayerId: player.PlayerId, tileId: tileId, source: GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        board.PlaceFungalCell(cell);
        player.AddControlledTile(tileId);
        return cell;
    }

    private static void CreateDeadCell(GameBoard board, Player player, int tileId)
    {
        var cell = PlaceLivingCell(board, player, tileId);
        board.KillFungalCell(cell, DeathReason.Unknown);
    }

    private sealed class AlwaysZeroRandom : Random
    {
        protected override double Sample() => 0.0;
    }

    private sealed class AlwaysHighRandom : Random
    {
        protected override double Sample() => 0.999999;
    }

    private sealed class CountingSequenceRandom : Random
    {
        private readonly Queue<double> values;

        public CountingSequenceRandom(params double[] values)
        {
            this.values = new Queue<double>(values);
        }

        public int NextDoubleCallCount { get; private set; }

        protected override double Sample()
        {
            NextDoubleCallCount++;
            return values.Count > 0 ? values.Dequeue() : 0.999999;
        }
    }

    private sealed class CountingObserver : ISimulationObserver
    {
        public int RegenerativeHyphaeReclaims { get; private set; }
        public int HypersystemicResistanceApplications { get; private set; }
        public int HypersystemicDiagonalReclaims { get; private set; }
        public int CreepingMoldToxinJumps { get; private set; }
        public int SporicidalReports { get; private set; }
        public int LastSporicidalDropCount { get; private set; }

        public void RecordRegenerativeHyphaeReclaim(int playerId) => RegenerativeHyphaeReclaims++;
        public void RecordCreepingMoldToxinJump(int playerId) => CreepingMoldToxinJumps++;
        public void ReportSporicidalSporeDrop(int playerId, int count)
        {
            SporicidalReports++;
            LastSporicidalDropCount = count;
        }

        public void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int pointsPerUpgrade) { }
        public void RecordApicalYieldBonus(int playerId, string mutationName, int bonusPoints) { }
        public void RecordMutationUpgradeEvent(int playerId, int mutationId, string mutationName, MutationTier mutationTier, int oldLevel, int newLevel, int round, int mutationPointsBefore, int mutationPointsAfter, int pointsSpent, string upgradeSource) { }
        public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned) { }
        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned) { }
        public void RecordAdaptiveExpressionBonus(int playerId, int bonus) { }
        public void RecordAnabolicInversionBonus(int playerId, int bonus) { }
        public void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1) { }
        public void RecordAttributedKill(int playerId, DeathReason reason, int killCount = 1) { }
        public void RecordCreepingMoldMove(int playerId) { }
        public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount) { }
        public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount) { }
        public void RecordTendrilGrowth(int playerId, DiagonalDirection value) { }
        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints) { }
        public void RecordNutrientPatchesPlaced(int count) { }
        public void RecordNutrientPatchConsumed(int playerId, int nutrientTileId, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount) { }
        public void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions) { }
        public void RecordCatabolicRebirthResurrection(int playerId, int resurrectedCells) { }
        public void ReportNecrosporeDrop(int playerId, int count) { }
        public void ReportNecrophyticBloomSporeDrop(int playerId, int sporesDropped, int successfulReclaims) { }
        public void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped) { }
        public void RecordMutationPointIncome(int playerId, int newMutationPoints) { }
        public void RecordBankedPoints(int playerId, int pointsBanked) { }
        public void RecordHyphalSurgeGrowth(int playerId) { }
        public void RecordDirectedVectorGrowth(int playerId, int cellsPlaced) { }
        public void ReportJettingMyceliumInfested(int playerId, int infested) { }
        public void ReportJettingMyceliumReclaimed(int playerId, int reclaimed) { }
        public void ReportJettingMyceliumCatabolicGrowth(int playerId, int catabolicGrowth) { }
        public void ReportJettingMyceliumAlreadyOwned(int playerId, int alreadyOwned) { }
        public void ReportJettingMyceliumInvalid(int playerId, int invalid) { }
        public void ReportJettingMyceliumColonized(int playerId, int colonized) { }
        public void ReportJettingMyceliumToxified(int playerId, int toxified) { }
        public void ReportJettingMyceliumPoisoned(int playerId, int poisoned) { }
        public void ReportDirectedVectorInfested(int playerId, int infested) { }
        public void ReportDirectedVectorReclaimed(int playerId, int reclaimed) { }
        public void ReportDirectedVectorCatabolicGrowth(int playerId, int catabolicGrowth) { }
        public void ReportDirectedVectorAlreadyOwned(int playerId, int alreadyOwned) { }
        public void ReportDirectedVectorColonized(int playerId, int colonized) { }
        public void ReportDirectedVectorInvalid(int playerId, int invalid) { }
        public void RecordStandardGrowth(int playerId) { }
        public void RecordNeutralizingMantleEffect(int playerId, int toxinsNeutralized) { }
        public void RecordBastionedCells(int playerId, int count) { }
        public void RecordCatabolicRebirthAgedToxin(int playerId, int toxinsAged) { }
        public void RecordSurgicalInoculationDrop(int playerId, int count) { }
        public void RecordPutrefactiveRejuvenationGrowthCyclesReduced(int playerId, int totalCyclesReduced) { }
        public void RecordPerimeterProliferatorGrowth(int playerId) { }
        public void RecordHyphalResistanceTransfer(int playerId, int count) { }
        public void RecordSeptalAlarmResistance(int playerId, int count) { }
        public void RecordEnduringToxaphoresExtendedCycles(int playerId, int cycles) { }
        public void RecordEnduringToxaphoresExistingExtensions(int playerId, int cycles) { }
        public void RecordReclamationRhizomorphsSecondAttempt(int playerId, int count) { }
        public void RecordNecrophoricAdaptationReclamation(int playerId, int count) { }
        public void RecordBallistosporeDischarge(int playerId, int count) { }
        public void RecordChitinFortificationCellsFortified(int playerId, int count) { }
        public void RecordPutrefactiveCascadeKills(int playerId, int cascadeKills) { }
        public void RecordPutrefactiveCascadeToxified(int playerId, int toxified) { }
        public void RecordMimeticResilienceInfestations(int playerId, int infestations) { }
        public void RecordMimeticResilienceDrops(int playerId, int drops) { }
        public void RecordCytolyticBurstToxins(int playerId, int toxinsCreated) { }
        public void RecordCytolyticBurstKills(int playerId, int cellsKilled) { }
        public void RecordChemotacticMycotoxinsRelocations(int playerId, int relocations) { }
        public void RecordVesicleBurstEffect(int playerId, int poisonedCells, int toxifiedTiles) { }
        public void RecordHypersystemicRegenerationResistance(int playerId) => HypersystemicResistanceApplications++;
        public void RecordHypersystemicDiagonalReclaim(int playerId) => HypersystemicDiagonalReclaims++;
        public void RecordMutatorPhenotypeUpgrade(int playerId, string mutationName) { }
        public void RecordSpecificMutationUpgrade(int playerId, string mutationName) { }
        public void RecordRetrogradeBloomUpgrade(int playerId, string evolvedMutationName, string devolvedMutationSummary, int devolvedPoints) { }
        public void RecordOntogenicRegressionEffect(int playerId, string sourceMutationName, int sourceLevelsLost, string targetMutationName, int targetLevelsGained) { }
        public void RecordOntogenicRegressionFailureBonus(int playerId, int bonusPoints) { }
        public void RecordCompetitiveAntagonismTargeting(int playerId, int targetsAffected) { }
        public void RecordOntogenicRegressionSacrifices(int playerId, int cellsKilled, int levelsOffset) { }
    }
}
