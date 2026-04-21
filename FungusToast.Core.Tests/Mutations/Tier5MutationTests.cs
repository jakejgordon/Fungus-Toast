using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Events;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class Tier5MutationTests
{
    [Fact]
    public void NecrohyphalInfiltration_is_tier5_cellular_resilience_and_has_expected_prerequisites()
    {
        var mutation = RequireMutation(MutationIds.NecrohyphalInfiltration);

        Assert.Equal(MutationCategory.CellularResilience, mutation.Category);
        Assert.Equal(MutationTier.Tier5, mutation.Tier);
        Assert.Equal(MutationType.NecrohyphalInfiltration, mutation.Type);
        Assert.Equal(2, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.RegenerativeHyphae && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.MycotoxinPotentiation && p.RequiredLevel == 1);
        Assert.Contains("chain into another adjacent dead cell", mutation.Description);
    }

    [Fact]
    public void NecrotoxicConversion_is_tier5_fungicide_and_has_expected_prerequisites()
    {
        var mutation = RequireMutation(MutationIds.NecrotoxicConversion);

        Assert.Equal(MutationCategory.Fungicide, mutation.Category);
        Assert.Equal(MutationTier.Tier5, mutation.Tier);
        Assert.Equal(MutationType.NecrotoxicConversion, mutation.Type);
        Assert.Equal(2, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.SporicidalBloom && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.MutatorPhenotype && p.RequiredLevel == 5);
        Assert.Contains("instantly reclaim any cell killed by your toxin effects", mutation.Description);
    }

    [Fact]
    public void PutrefactiveRejuvenation_is_tier5_fungicide_and_has_expected_prerequisites()
    {
        var mutation = RequireMutation(MutationIds.PutrefactiveRejuvenation);

        Assert.Equal(MutationCategory.Fungicide, mutation.Category);
        Assert.Equal(MutationTier.Tier5, mutation.Tier);
        Assert.Equal(MutationType.PutrefactiveRejuvenation, mutation.Type);
        Assert.Equal(2, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.PutrefactiveMycotoxin && p.RequiredLevel == 2);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.ChronoresilientCytoplasm && p.RequiredLevel == 1);
        Assert.Contains("Rejuvenation radius is doubled", mutation.Description);
    }

    [Fact]
    public void HyperadaptiveDrift_is_tier5_genetic_drift_and_has_expected_prerequisites()
    {
        var mutation = RequireMutation(MutationIds.HyperadaptiveDrift);

        Assert.Equal(MutationCategory.GeneticDrift, mutation.Category);
        Assert.Equal(MutationTier.Tier5, mutation.Tier);
        Assert.Equal(MutationType.FreeMutationUpgrade, mutation.Type);
        Assert.Equal(5, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.NecrophyticBloom && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.MutatorPhenotype && p.RequiredLevel == GameBalance.MutatorPhenotypeMaxLevel - 2);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.MycotoxinPotentiation && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.AdaptiveExpression && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.ChronoresilientCytoplasm && p.RequiredLevel == 1);
        Assert.Contains("Also upgrades an additional Tier 1 mutation", mutation.Description);
    }

    [Fact]
    public void TryApplyMutatorPhenotype_with_hyperadaptive_drift_can_target_a_higher_tier_mutation()
    {
        var player = CreatePlayer();
        var observer = new CountingObserver();
        var allMutations = MutationRegistry.GetAll().ToList();

        player.SetMutationLevel(MutationIds.MutatorPhenotype, newLevel: 10, currentRound: 1);
        player.SetMutationLevel(MutationIds.HyperadaptiveDrift, newLevel: 1, currentRound: 1);

        // Make one tier-2 and one tier-3 mutation eligible while leaving tier-1 roots available too.
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 10, currentRound: 1);
        player.SetMutationLevel(MutationIds.TendrilNorthwest, newLevel: 1, currentRound: 1);
        player.SetMutationLevel(MutationIds.TendrilNortheast, newLevel: 1, currentRound: 1);
        player.SetMutationLevel(MutationIds.TendrilSoutheast, newLevel: 1, currentRound: 1);
        player.SetMutationLevel(MutationIds.TendrilSouthwest, newLevel: 1, currentRound: 1);

        GeneticDriftMutationProcessor.TryApplyMutatorPhenotype(
            player,
            allMutations,
            new SequenceRandom(0.0, 0.0, 0.0),
            currentRound: 2,
            observer);

        Assert.True(
            player.GetMutationLevel(MutationIds.TendrilNorthwest) > 1 ||
            player.GetMutationLevel(MutationIds.MycotropicInduction) > 0 ||
            player.GetMutationLevel(MutationIds.RegenerativeHyphae) > 0,
            "Expected Hyperadaptive Drift to upgrade an eligible tier 2-4 mutation when the higher-tier roll hits.");
        Assert.True(observer.HyperadaptivePointsEarned > 0);
    }

    [Fact]
    public void TryApplyMutatorPhenotype_with_hyperadaptive_tier1_fallback_can_apply_bonus_extra_upgrades()
    {
        var player = CreatePlayer();
        var observer = new CountingObserver();
        var allMutations = MutationRegistry.GetAll().ToList();

        player.SetMutationLevel(MutationIds.MutatorPhenotype, newLevel: 10, currentRound: 1);
        player.SetMutationLevel(MutationIds.HyperadaptiveDrift, newLevel: 1, currentRound: 1);

        GeneticDriftMutationProcessor.TryApplyMutatorPhenotype(
            player,
            allMutations,
            new SequenceRandom(0.0, 0.99, 0.0, 0.0),
            currentRound: 2,
            observer);

        int rootLevels = player.GetMutationLevel(MutationIds.MycelialBloom)
                       + player.GetMutationLevel(MutationIds.HomeostaticHarmony)
                       + player.GetMutationLevel(MutationIds.MycotoxinTracer);

        Assert.Equal(GameBalance.HyperadaptiveDriftBonusTierOneMutationFreeUpgradeTimes, rootLevels);
        Assert.True(observer.HyperadaptivePointsEarned > 0);
    }

    [Fact]
    public void TryApplyMutatorPhenotype_at_max_hyperadaptive_drift_applies_additional_tier1_bonus_upgrade()
    {
        var player = CreatePlayer();
        var observer = new CountingObserver();
        var allMutations = MutationRegistry.GetAll().ToList();

        player.SetMutationLevel(MutationIds.MutatorPhenotype, newLevel: 10, currentRound: 1);
        player.SetMutationLevel(MutationIds.HyperadaptiveDrift, newLevel: GameBalance.HyperadaptiveDriftMaxLevel, currentRound: 1);

        GeneticDriftMutationProcessor.TryApplyMutatorPhenotype(
            player,
            allMutations,
            new SequenceRandom(0.0, 0.99, 0.0, 0.0, 0.0),
            currentRound: 2,
            observer);

        int rootLevels = player.GetMutationLevel(MutationIds.MycelialBloom)
                       + player.GetMutationLevel(MutationIds.HomeostaticHarmony)
                       + player.GetMutationLevel(MutationIds.MycotoxinTracer);

        Assert.Equal(GameBalance.HyperadaptiveDriftBonusTierOneMutationFreeUpgradeTimes + 1, rootLevels);
        Assert.True(observer.HyperadaptivePointsEarned > 0);
    }

    [Fact]
    public void TryNecrohyphalInfiltration_reclaims_adjacent_dead_enemy_cells_and_cascades_when_rolls_hit()
    {
        var board = new GameBoard(width: 4, height: 3, playerCount: 2);
        var owner = CreatePlayer(0);
        var enemy = CreatePlayer(1);
        board.Players.Add(owner);
        board.Players.Add(enemy);
        owner.SetMutationLevel(MutationIds.NecrohyphalInfiltration, newLevel: GameBalance.NecrohyphalInfiltrationMaxLevel, currentRound: 1);

        var sourceCell = PlaceLivingCell(board, owner, tileId: 4);
        var sourceTile = board.GetTileById(sourceCell.TileId)!;
        CreateDeadCell(board, enemy, tileId: 5);
        CreateDeadCell(board, enemy, tileId: 6);

        var observer = new CountingObserver();
        var infiltrated = CellularResilienceMutationProcessor.TryNecrohyphalInfiltration(
            board,
            sourceTile,
            sourceCell,
            owner,
            new AlwaysZeroRandom(),
            observer);

        Assert.True(infiltrated);
        Assert.True(board.GetCell(5)!.IsAlive);
        Assert.Equal(owner.PlayerId, board.GetCell(5)!.OwnerPlayerId);
        Assert.True(board.GetCell(6)!.IsAlive);
        Assert.Equal(owner.PlayerId, board.GetCell(6)!.OwnerPlayerId);
        Assert.Equal(1, observer.NecrohyphalInfiltrations);
        Assert.Equal(1, observer.NecrohyphalCascades);
    }

    [Fact]
    public void TryNecrohyphalInfiltration_returns_false_when_no_adjacent_dead_enemy_cells_exist()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);
        var owner = CreatePlayer(0);
        var enemy = CreatePlayer(1);
        board.Players.Add(owner);
        board.Players.Add(enemy);
        owner.SetMutationLevel(MutationIds.NecrohyphalInfiltration, newLevel: 1, currentRound: 1);

        var sourceCell = PlaceLivingCell(board, owner, tileId: 4);
        var sourceTile = board.GetTileById(sourceCell.TileId)!;
        PlaceLivingCell(board, enemy, tileId: 1);

        var observer = new CountingObserver();
        var infiltrated = CellularResilienceMutationProcessor.TryNecrohyphalInfiltration(
            board,
            sourceTile,
            sourceCell,
            owner,
            new AlwaysZeroRandom(),
            observer);

        Assert.False(infiltrated);
        Assert.Equal(0, observer.NecrohyphalInfiltrations);
        Assert.Equal(0, observer.NecrohyphalCascades);
    }

    [Fact]
    public void OnCellDeath_necrotoxic_conversion_reclaims_toxin_kill_when_roll_hits()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);
        var killer = CreatePlayer(0);
        var victim = CreatePlayer(1);
        board.Players.Add(killer);
        board.Players.Add(victim);
        killer.SetMutationLevel(MutationIds.NecrotoxicConversion, newLevel: GameBalance.NecrotoxicConversionMaxLevel, currentRound: 1);

        var deadCell = CreateDeadCell(board, victim, tileId: 4);
        var observer = new CountingObserver();

        FungicideMutationProcessor.OnCellDeath_NecrotoxicConversion(
            new FungalCellDiedEventArgs(deadCell.TileId, victim.PlayerId, DeathReason.SporicidalBloom, killer.PlayerId, deadCell),
            board,
            board.Players,
            new AlwaysZeroRandom(),
            observer);

        var reclaimedCell = Assert.IsType<FungalCell>(board.GetCell(4));
        Assert.True(reclaimedCell.IsAlive);
        Assert.Equal(killer.PlayerId, reclaimedCell.OwnerPlayerId);
        Assert.Equal(GrowthSource.NecrotoxicConversion, reclaimedCell.SourceOfGrowth);
        Assert.Equal(1, observer.NecrotoxicConversions);
    }

    [Fact]
    public void OnCellDeath_putrefactive_rejuvenation_reduces_age_of_friendly_living_cells_within_base_radius()
    {
        var board = new GameBoard(width: 7, height: 7, playerCount: 2);
        var killer = CreatePlayer(0);
        var victim = CreatePlayer(1);
        board.Players.Add(killer);
        board.Players.Add(victim);
        killer.SetMutationLevel(MutationIds.PutrefactiveRejuvenation, newLevel: 2, currentRound: 1);

        var nearCell = PlaceLivingCell(board, killer, tileId: board.GetTile(4, 3)!.TileId);
        var farCell = PlaceLivingCell(board, killer, tileId: board.GetTile(6, 6)!.TileId);
        nearCell.SetGrowthCycleAge(20);
        farCell.SetGrowthCycleAge(20);

        var poisonedTarget = CreateDeadCell(board, victim, tileId: board.GetTile(3, 3)!.TileId);
        var observer = new CountingObserver();

        FungicideMutationProcessor.OnCellDeath_PutrefactiveRejuvenation(
            new FungalCellDiedEventArgs(poisonedTarget.TileId, victim.PlayerId, DeathReason.PutrefactiveMycotoxin, killer.PlayerId, poisonedTarget),
            board,
            board.Players,
            observer);

        Assert.Equal(8, nearCell.GrowthCycleAge);
        Assert.Equal(20, farCell.GrowthCycleAge);
        Assert.Equal(12, observer.PutrefactiveRejuvenationGrowthCyclesReduced);
    }

    [Fact]
    public void OnCellDeath_putrefactive_rejuvenation_at_max_level_uses_extended_radius()
    {
        var board = new GameBoard(width: 11, height: 11, playerCount: 2);
        var killer = CreatePlayer(0);
        var victim = CreatePlayer(1);
        board.Players.Add(killer);
        board.Players.Add(victim);
        killer.SetMutationLevel(MutationIds.PutrefactiveRejuvenation, newLevel: GameBalance.PutrefactiveRejuvenationMaxLevel, currentRound: 1);

        var withinExtendedRadius = PlaceLivingCell(board, killer, tileId: board.GetTile(9, 5)!.TileId);
        var outsideExtendedRadius = PlaceLivingCell(board, killer, tileId: board.GetTile(10, 10)!.TileId);
        withinExtendedRadius.SetGrowthCycleAge(30);
        outsideExtendedRadius.SetGrowthCycleAge(30);

        var poisonedTarget = CreateDeadCell(board, victim, tileId: board.GetTile(5, 5)!.TileId);
        var observer = new CountingObserver();

        FungicideMutationProcessor.OnCellDeath_PutrefactiveRejuvenation(
            new FungalCellDiedEventArgs(poisonedTarget.TileId, victim.PlayerId, DeathReason.PutrefactiveMycotoxin, killer.PlayerId, poisonedTarget),
            board,
            board.Players,
            observer);

        Assert.Equal(6, withinExtendedRadius.GrowthCycleAge);
        Assert.Equal(30, outsideExtendedRadius.GrowthCycleAge);
        Assert.Equal(24, observer.PutrefactiveRejuvenationGrowthCyclesReduced);
    }

    [Fact]
    public void OnCellDeath_necrotoxic_conversion_ignores_non_toxin_deaths()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);
        var killer = CreatePlayer(0);
        var victim = CreatePlayer(1);
        board.Players.Add(killer);
        board.Players.Add(victim);
        killer.SetMutationLevel(MutationIds.NecrotoxicConversion, newLevel: GameBalance.NecrotoxicConversionMaxLevel, currentRound: 1);

        var deadCell = CreateDeadCell(board, victim, tileId: 4);
        var observer = new CountingObserver();

        FungicideMutationProcessor.OnCellDeath_NecrotoxicConversion(
            new FungalCellDiedEventArgs(deadCell.TileId, victim.PlayerId, DeathReason.Age, killer.PlayerId, deadCell),
            board,
            board.Players,
            new AlwaysZeroRandom(),
            observer);

        Assert.True(board.GetCell(4)!.IsDead);
        Assert.Equal(victim.PlayerId, board.GetCell(4)!.OwnerPlayerId);
        Assert.Equal(0, observer.NecrotoxicConversions);
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

    private static FungalCell CreateDeadCell(GameBoard board, Player player, int tileId)
    {
        var cell = PlaceLivingCell(board, player, tileId);
        board.KillFungalCell(cell, DeathReason.Unknown);
        return Assert.IsType<FungalCell>(board.GetCell(tileId));
    }

    private sealed class AlwaysZeroRandom : Random
    {
        protected override double Sample() => 0.0;
    }

    private sealed class SequenceRandom : Random
    {
        private readonly Queue<double> values;

        public SequenceRandom(params double[] values)
        {
            this.values = new Queue<double>(values);
        }

        protected override double Sample()
        {
            return values.Count > 0 ? values.Dequeue() : 0.0;
        }
    }

    private sealed class CountingObserver : ISimulationObserver
    {
        public int NecrohyphalInfiltrations { get; private set; }
        public int NecrohyphalCascades { get; private set; }
        public int NecrotoxicConversions { get; private set; }
        public int PutrefactiveRejuvenationGrowthCyclesReduced { get; private set; }
        public int HyperadaptivePointsEarned { get; private set; }

        public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount) => NecrohyphalInfiltrations += necrohyphalInfiltrationCount;
        public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount) => NecrohyphalCascades += cascadeCount;
        public void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions) => NecrotoxicConversions += necrotoxicConversions;

        public void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int pointsPerUpgrade) { }
        public void RecordApicalYieldBonus(int playerId, string mutationName, int bonusPoints) { }
        public void RecordMutationUpgradeEvent(int playerId, int mutationId, string mutationName, MutationTier mutationTier, int oldLevel, int newLevel, int round, int mutationPointsBefore, int mutationPointsAfter, int pointsSpent, string upgradeSource) { }
        public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned) { }
        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned) => HyperadaptivePointsEarned += freePointsEarned;
        public void RecordAdaptiveExpressionBonus(int playerId, int bonus) { }
        public void RecordAnabolicInversionBonus(int playerId, int bonus) { }
        public void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1) { }
        public void RecordAttributedKill(int playerId, DeathReason reason, int killCount = 1) { }
        public void RecordCreepingMoldMove(int playerId) { }
        public void RecordCreepingMoldToxinJump(int playerId) { }
        public void RecordTendrilGrowth(int playerId, DiagonalDirection value) { }
        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints) { }
        public void RecordNutrientPatchesPlaced(int count) { }
        public void RecordNutrientPatchConsumed(int playerId, int nutrientTileId, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount) { }
        public void RecordCatabolicRebirthResurrection(int playerId, int resurrectedCells) { }
        public void RecordRegenerativeHyphaeReclaim(int playerId) { }
        public void ReportSporicidalSporeDrop(int playerId, int count) { }
        public void ReportNecrosporeDrop(int playerId, int count) { }
        public void RecordNecrophyticBloomPatchCreation(int playerId, int createdPatchCount) { }
        public void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped) { }
        public void RecordMutationPointIncome(int playerId, int newMutationPoints) { }
        public void RecordPrimePulseTriggered(int playerId, int triggerRound, int mutationPointsAwarded) { }
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
        public void RecordPutrefactiveRejuvenationGrowthCyclesReduced(int playerId, int totalCyclesReduced) => PutrefactiveRejuvenationGrowthCyclesReduced += totalCyclesReduced;
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
        public void RecordHypersystemicRegenerationResistance(int playerId) { }
        public void RecordHypersystemicDiagonalReclaim(int playerId) { }
        public void RecordMutatorPhenotypeUpgrade(int playerId, string mutationName) { }
        public void RecordSpecificMutationUpgrade(int playerId, string mutationName) { }
        public void RecordRetrogradeBloomUpgrade(int playerId, string evolvedMutationName, string devolvedMutationSummary, int devolvedPoints) { }
        public void RecordOntogenicRegressionEffect(int playerId, string sourceMutationName, int sourceLevelsLost, string targetMutationName, int targetLevelsGained) { }
        public void RecordOntogenicRegressionFailureBonus(int playerId, int bonusPoints) { }
        public void RecordCompetitiveAntagonismTargeting(int playerId, int targetsAffected) { }
        public void RecordOntogenicRegressionSacrifices(int playerId, int cellsKilled, int levelsOffset) { }
        public void RecordMycelialCrescendoSurge(int playerId, string surgeName) { }
    }
}
