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

public class Tier6MutationTests
{
    [Fact]
    public void CatabolicRebirth_is_tier6_cellular_resilience_and_has_expected_prerequisites()
    {
        var mutation = RequireMutation(MutationIds.CatabolicRebirth);

        Assert.Equal(MutationCategory.CellularResilience, mutation.Category);
        Assert.Equal(MutationTier.Tier6, mutation.Tier);
        Assert.Equal(MutationType.ToxinExpirationResurrection, mutation.Type);
        Assert.Equal(2, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.NecrohyphalInfiltration && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.AnabolicInversion && p.RequiredLevel == 1);
        Assert.Contains("Enemy toxins adjacent to your dead cells age twice as fast", mutation.Description);
    }

    [Fact]
    public void PutrefactiveCascade_is_tier6_fungicide_and_has_expected_prerequisites()
    {
        var mutation = RequireMutation(MutationIds.PutrefactiveCascade);

        Assert.Equal(MutationCategory.Fungicide, mutation.Category);
        Assert.Equal(MutationTier.Tier6, mutation.Tier);
        Assert.Equal(MutationType.PutrefactiveCascade, mutation.Type);
        Assert.Equal(2, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.PutrefactiveRejuvenation && p.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.ChemotacticBeacon && p.RequiredLevel == 1);
        Assert.Contains("Cascaded kills leave toxin tiles instead of dead cells", mutation.Description);
    }

    [Fact]
    public void OntogenicRegression_is_tier6_genetic_drift_and_has_expected_prerequisites()
    {
        var mutation = RequireMutation(MutationIds.OntogenicRegression);

        Assert.Equal(MutationCategory.GeneticDrift, mutation.Category);
        Assert.Equal(MutationTier.Tier6, mutation.Tier);
        Assert.Equal(MutationType.OntogenicRegression, mutation.Type);
        Assert.Equal(5, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.HyperadaptiveDrift && p.RequiredLevel == 2);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.MycelialBloom && p.RequiredLevel == 10);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.HomeostaticHarmony && p.RequiredLevel == 10);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.MycotoxinTracer && p.RequiredLevel == 10);
        Assert.Contains(mutation.Prerequisites, p => p.MutationId == MutationIds.MutatorPhenotype && p.RequiredLevel == 10);
        Assert.Contains("Triggers twice", mutation.Description);
    }

    [Fact]
    public void OnToxinExpired_catabolic_rebirth_revives_adjacent_own_dead_cell_when_roll_hits()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 2);
        var player = CreatePlayer(0);
        var enemy = CreatePlayer(1);
        board.Players.Add(player);
        board.Players.Add(enemy);
        player.SetMutationLevel(MutationIds.CatabolicRebirth, newLevel: GameBalance.CatabolicRebirthMaxLevel, currentRound: 1);

        CreateDeadCell(board, player, tileId: 4);
        ToxinHelper.ConvertToToxin(board, tileId: 1, toxinLifespan: 3, GrowthSource.Manual, enemy);

        var observer = new CountingObserver();

        CellularResilienceMutationProcessor.OnToxinExpired_CatabolicRebirth(
            new ToxinExpiredEventArgs(tileId: 1, toxinOwnerPlayerId: enemy.PlayerId),
            board,
            board.Players,
            new AlwaysZeroRandom(),
            observer);

        var reclaimedCell = Assert.IsType<FungalCell>(board.GetCell(4));
        Assert.True(reclaimedCell.IsAlive);
        Assert.Equal(player.PlayerId, reclaimedCell.OwnerPlayerId);
        Assert.Equal(GrowthSource.CatabolicRebirth, reclaimedCell.SourceOfGrowth);
        Assert.Equal(1, observer.CatabolicRebirthResurrections);
    }

    [Fact]
    public void OnToxinExpired_catabolic_rebirth_ignores_non_adjacent_dead_cells()
    {
        var board = new GameBoard(width: 4, height: 4, playerCount: 2);
        var player = CreatePlayer(0);
        var enemy = CreatePlayer(1);
        board.Players.Add(player);
        board.Players.Add(enemy);
        player.SetMutationLevel(MutationIds.CatabolicRebirth, newLevel: GameBalance.CatabolicRebirthMaxLevel, currentRound: 1);

        CreateDeadCell(board, player, tileId: 15);
        ToxinHelper.ConvertToToxin(board, tileId: 1, toxinLifespan: 3, GrowthSource.Manual, enemy);

        var observer = new CountingObserver();

        CellularResilienceMutationProcessor.OnToxinExpired_CatabolicRebirth(
            new ToxinExpiredEventArgs(tileId: 1, toxinOwnerPlayerId: enemy.PlayerId),
            board,
            board.Players,
            new AlwaysZeroRandom(),
            observer);

        Assert.True(board.GetCell(15)!.IsDead);
        Assert.Equal(player.PlayerId, board.GetCell(15)!.OwnerPlayerId);
        Assert.Equal(0, observer.CatabolicRebirthResurrections);
    }

    [Fact]
    public void TryApplyOntogenicRegression_awards_failure_bonus_when_proc_roll_misses()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var player = CreatePlayer(0);
        board.Players.Add(player);
        for (int i = 0; i < 5; i++) board.IncrementRound();
        player.SetMutationLevel(MutationIds.OntogenicRegression, newLevel: 1, currentRound: 1);

        var observer = new CountingObserver();
        int startingPoints = player.MutationPoints;

        GeneticDriftMutationProcessor.TryApplyOntogenicRegression(
            player,
            MutationRegistry.GetAll().ToList(),
            new AlwaysHighRandom(),
            board,
            observer);

        Assert.Equal(startingPoints + GameBalance.OntogenicRegressionFailureConsolationPoints, player.MutationPoints);
        Assert.Equal(GameBalance.OntogenicRegressionFailureConsolationPoints, observer.OntogenicRegressionFailureBonus);
        Assert.Equal(GameBalance.OntogenicRegressionFailureConsolationPoints, observer.MutationPointIncome);
    }

    [Fact]
    public void TryApplyOntogenicRegression_consumes_three_tier1_levels_and_upgrades_a_tier5_or_tier6_mutation()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var player = CreatePlayer(0);
        board.Players.Add(player);
        for (int i = 0; i < 5; i++) board.IncrementRound();
        player.SetMutationLevel(MutationIds.OntogenicRegression, newLevel: 1, currentRound: 1);
        player.SetMutationLevel(MutationIds.MycelialBloom, newLevel: 3, currentRound: 1);

        var allMutations = MutationRegistry.GetAll().ToList();
        var observer = new CountingObserver();

        GeneticDriftMutationProcessor.TryApplyOntogenicRegression(
            player,
            allMutations,
            new SequenceRandom(0.0, 0.0, 0.0),
            board,
            observer);

        Assert.Equal(0, player.GetMutationLevel(MutationIds.MycelialBloom));
        Assert.True(
            player.GetMutationLevel(MutationIds.NecrohyphalInfiltration) > 0 ||
            player.GetMutationLevel(MutationIds.NecrotoxicConversion) > 0 ||
            player.GetMutationLevel(MutationIds.PutrefactiveRejuvenation) > 0 ||
            player.GetMutationLevel(MutationIds.HyperadaptiveDrift) > 0 ||
            player.GetMutationLevel(MutationIds.CatabolicRebirth) > 0 ||
            player.GetMutationLevel(MutationIds.PutrefactiveCascade) > 0,
            "Expected Ontogenic Regression to upgrade a tier 5 or tier 6 mutation.");
        Assert.Equal(3, observer.OntogenicRegressionSourceLevelsLost);
        Assert.Equal(1, observer.OntogenicRegressionTargetLevelsGained);
    }

    [Fact]
    public void OnCellDeath_putrefactive_cascade_ignores_events_without_attacker_tile()
    {
        var board = new GameBoard(width: 4, height: 1, playerCount: 2);
        var killer = CreatePlayer(0);
        var enemy = CreatePlayer(1);
        board.Players.Add(killer);
        board.Players.Add(enemy);
        killer.SetMutationLevel(MutationIds.PutrefactiveCascade, newLevel: 1, currentRound: 1);

        PlaceLivingCell(board, killer, tileId: 0);
        var killedCell = CreateDeadCell(board, enemy, tileId: 1);
        PlaceLivingCell(board, enemy, tileId: 2);

        var observer = new CountingObserver();

        FungicideMutationProcessor.OnCellDeath_PutrefactiveCascade(
            new FungalCellDiedEventArgs(killedCell.TileId, enemy.PlayerId, DeathReason.PutrefactiveMycotoxin, killer.PlayerId, killedCell),
            board,
            board.Players,
            new AlwaysZeroRandom(),
            observer);

        Assert.True(board.GetCell(2)!.IsAlive);
        Assert.Equal(enemy.PlayerId, board.GetCell(2)!.OwnerPlayerId);
        Assert.Equal(0, observer.PutrefactiveCascadeKills);
    }

    [Fact]
    public void OnCellDeath_putrefactive_cascade_below_max_level_kills_next_enemy_cell_in_line()
    {
        var board = new GameBoard(width: 4, height: 1, playerCount: 2);
        var killer = CreatePlayer(0);
        var enemy = CreatePlayer(1);
        board.Players.Add(killer);
        board.Players.Add(enemy);
        killer.SetMutationLevel(MutationIds.PutrefactiveCascade, newLevel: 1, currentRound: 1);

        PlaceLivingCell(board, killer, tileId: 0);
        var killedCell = CreateDeadCell(board, enemy, tileId: 1);
        PlaceLivingCell(board, enemy, tileId: 2);

        var observer = new CountingObserver();

        FungicideMutationProcessor.OnCellDeath_PutrefactiveCascade(
            new FungalCellDiedEventArgs(killedCell.TileId, enemy.PlayerId, DeathReason.PutrefactiveMycotoxin, killer.PlayerId, killedCell, attackerTileId: 0),
            board,
            board.Players,
            new SequenceRandom(0.0, 0.999999),
            observer);

        Assert.True(board.GetCell(2)!.IsDead);
        Assert.Equal(enemy.PlayerId, board.GetCell(2)!.OwnerPlayerId);
        Assert.Equal(1, observer.PutrefactiveCascadeKills);
        Assert.Equal(0, observer.PutrefactiveCascadeToxified);
    }

    [Fact]
    public void OnCellDeath_putrefactive_cascade_at_max_level_toxifies_next_enemy_cell_in_line()
    {
        var board = new GameBoard(width: 4, height: 1, playerCount: 2);
        var killer = CreatePlayer(0);
        var enemy = CreatePlayer(1);
        board.Players.Add(killer);
        board.Players.Add(enemy);
        killer.SetMutationLevel(MutationIds.PutrefactiveCascade, newLevel: GameBalance.PutrefactiveCascadeMaxLevel, currentRound: 1);

        PlaceLivingCell(board, killer, tileId: 0);
        var killedCell = CreateDeadCell(board, enemy, tileId: 1);
        PlaceLivingCell(board, enemy, tileId: 2);

        var observer = new CountingObserver();

        FungicideMutationProcessor.OnCellDeath_PutrefactiveCascade(
            new FungalCellDiedEventArgs(killedCell.TileId, enemy.PlayerId, DeathReason.PutrefactiveMycotoxin, killer.PlayerId, killedCell, attackerTileId: 0),
            board,
            board.Players,
            new SequenceRandom(0.0, 0.999999),
            observer);

        Assert.True(board.GetCell(2)!.IsToxin);
        Assert.Equal(killer.PlayerId, board.GetCell(2)!.OwnerPlayerId);
        Assert.Equal(1, observer.PutrefactiveCascadeKills);
        Assert.Equal(1, observer.PutrefactiveCascadeToxified);
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

    private sealed class AlwaysHighRandom : Random
    {
        protected override double Sample() => 0.999999;
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
        public int CatabolicRebirthResurrections { get; private set; }
        public int OntogenicRegressionFailureBonus { get; private set; }
        public int MutationPointIncome { get; private set; }
        public int OntogenicRegressionSourceLevelsLost { get; private set; }
        public int OntogenicRegressionTargetLevelsGained { get; private set; }
        public int PutrefactiveCascadeKills { get; private set; }
        public int PutrefactiveCascadeToxified { get; private set; }

        public void RecordCatabolicRebirthResurrection(int playerId, int resurrectedCells) => CatabolicRebirthResurrections += resurrectedCells;

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
        public void RecordCreepingMoldToxinJump(int playerId) { }
        public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount) { }
        public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount) { }
        public void RecordTendrilGrowth(int playerId, DiagonalDirection value) { }
        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints) { }
        public void RecordNutrientPatchesPlaced(int count) { }
        public void RecordNutrientPatchConsumed(int playerId, int nutrientTileId, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount) { }
        public void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions) { }
        public void RecordRegenerativeHyphaeReclaim(int playerId) { }
        public void ReportSporicidalSporeDrop(int playerId, int count) { }
        public void ReportNecrosporeDrop(int playerId, int count) { }
        public void RecordNecrophyticBloomPatchCreation(int playerId, int createdPatchCount) { }
        public void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped) { }
        public void RecordMutationPointIncome(int playerId, int newMutationPoints) => MutationPointIncome += newMutationPoints;
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
        public void RecordPutrefactiveCascadeKills(int playerId, int cascadeKills) => PutrefactiveCascadeKills += cascadeKills;
        public void RecordPutrefactiveCascadeToxified(int playerId, int toxified) => PutrefactiveCascadeToxified += toxified;
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
        public void RecordOntogenicRegressionEffect(int playerId, string sourceMutationName, int sourceLevelsLost, string targetMutationName, int targetLevelsGained)
        {
            OntogenicRegressionSourceLevelsLost += sourceLevelsLost;
            OntogenicRegressionTargetLevelsGained += targetLevelsGained;
        }
        public void RecordOntogenicRegressionFailureBonus(int playerId, int bonusPoints) => OntogenicRegressionFailureBonus += bonusPoints;
        public void RecordCompetitiveAntagonismTargeting(int playerId, int targetsAffected) { }
        public void RecordOntogenicRegressionSacrifices(int playerId, int cellsKilled, int levelsOffset) { }
        public void RecordMycelialCrescendoSurge(int playerId, string surgeName) { }
    }
}
