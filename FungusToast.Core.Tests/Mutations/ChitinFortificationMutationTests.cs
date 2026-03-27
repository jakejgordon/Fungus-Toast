using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Growth;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class ChitinFortificationMutationTests
{
    [Fact]
    public void ChitinFortification_is_tier2_mycelial_surge_and_requires_homeostatic_harmony_five()
    {
        var mutation = RequireMutation(MutationIds.ChitinFortification);

        Assert.Equal(MutationCategory.MycelialSurges, mutation.Category);
        Assert.Equal(MutationTier.Tier2, mutation.Tier);
        Assert.True(mutation.IsSurge);
        Assert.Equal(MutationType.ChitinFortification, mutation.Type);
        var prereq = Assert.Single(mutation.Prerequisites);
        Assert.Equal(MutationIds.HomeostaticHarmony, prereq.MutationId);
        Assert.Equal(5, prereq.RequiredLevel);
        Assert.Contains("random living cell(s) per level gain Resistant", mutation.Description);
        Assert.Contains("Resistant living cells cannot be killed or replaced", mutation.Description);
    }

    [Fact]
    public void OnPreGrowthPhase_chitin_fortification_fortifies_only_non_resistant_living_cells_and_records_batch_event()
    {
        var board = new GameBoard(width: 4, height: 4, playerCount: 1);
        var player = CreatePlayer(0);
        board.Players.Add(player);

        player.SetMutationLevel(MutationIds.ChitinFortification, newLevel: 1, currentRound: 1);
        player.ActiveSurges[MutationIds.ChitinFortification] = new Player.ActiveSurgeInfo(MutationIds.ChitinFortification, level: 1, duration: GameBalance.ChitinFortificationSurgeDuration);

        var preResistant = PlaceLivingCell(board, player, tileId: 0);
        preResistant.MakeResistant();
        var fortifyA = PlaceLivingCell(board, player, tileId: 1);
        var fortifyB = PlaceLivingCell(board, player, tileId: 2);
        var fortifyC = PlaceLivingCell(board, player, tileId: 3);
        var fortifyD = PlaceLivingCell(board, player, tileId: 4);

        int? resistancePlayerId = null;
        GrowthSource? resistanceSource = null;
        IReadOnlyList<int>? fortifiedTileIds = null;
        board.ResistanceAppliedBatch += (playerId, source, tileIds) =>
        {
            resistancePlayerId = playerId;
            resistanceSource = source;
            fortifiedTileIds = tileIds;
        };

        var observer = new CountingObserver();

        MycelialSurgeMutationProcessor.OnPreGrowthPhase_ChitinFortification(
            board,
            board.Players,
            new AlwaysZeroRandom(),
            observer);

        Assert.True(preResistant.IsResistant);
        Assert.True(fortifyA.IsResistant);
        Assert.True(fortifyB.IsResistant);
        Assert.True(fortifyC.IsResistant);
        Assert.True(fortifyD.IsResistant);
        Assert.Equal(player.PlayerId, resistancePlayerId);
        Assert.Equal(GrowthSource.ChitinFortification, resistanceSource);
        Assert.NotNull(fortifiedTileIds);
        Assert.Equal(new[] { 1, 2, 3, 4 }, fortifiedTileIds!.OrderBy(id => id).ToArray());
        Assert.Equal(4, observer.ChitinFortificationCellsFortified);
    }

    [Fact]
    public void OnPreGrowthPhase_chitin_fortification_does_nothing_without_active_surge()
    {
        var board = new GameBoard(width: 3, height: 3, playerCount: 1);
        var player = CreatePlayer(0);
        board.Players.Add(player);

        player.SetMutationLevel(MutationIds.ChitinFortification, newLevel: 2, currentRound: 1);
        var livingA = PlaceLivingCell(board, player, tileId: 0);
        var livingB = PlaceLivingCell(board, player, tileId: 1);

        var observer = new CountingObserver();

        MycelialSurgeMutationProcessor.OnPreGrowthPhase_ChitinFortification(
            board,
            board.Players,
            new AlwaysZeroRandom(),
            observer);

        Assert.False(livingA.IsResistant);
        Assert.False(livingB.IsResistant);
        Assert.Equal(0, observer.ChitinFortificationCellsFortified);
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

    private sealed class AlwaysZeroRandom : Random
    {
        protected override double Sample() => 0.0;
    }

    private sealed class CountingObserver : ISimulationObserver
    {
        public int ChitinFortificationCellsFortified { get; private set; }

        public void RecordChitinFortificationCellsFortified(int playerId, int count) => ChitinFortificationCellsFortified += count;

        public void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int pointsPerUpgrade) { }
        public void RecordApicalYieldBonus(int playerId, string mutationName, int bonusPoints) { }
        public void RecordMutationUpgradeEvent(int playerId, int mutationId, string mutationName, MutationTier mutationTier, int oldLevel, int newLevel, int round, int mutationPointsBefore, int mutationPointsAfter, int pointsSpent, string upgradeSource) { }
        public void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned) { }
        public void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned) { }
        public void RecordAdaptiveExpressionBonus(int playerId, int bonus) { }
        public void RecordAnabolicInversionBonus(int playerId, int bonus) { }
        public void RecordCellDeath(int playerId, FungusToast.Core.Death.DeathReason reason, int deathCount = 1) { }
        public void RecordAttributedKill(int playerId, FungusToast.Core.Death.DeathReason reason, int killCount = 1) { }
        public void RecordCreepingMoldMove(int playerId) { }
        public void RecordCreepingMoldToxinJump(int playerId) { }
        public void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount) { }
        public void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount) { }
        public void RecordTendrilGrowth(int playerId, DiagonalDirection value) { }
        public void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints) { }
        public void RecordNutrientPatchesPlaced(int count) { }
        public void RecordNutrientPatchConsumed(int playerId, int nutrientTileId, NutrientPatchType patchType, NutrientRewardType rewardType, int rewardAmount) { }
        public void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions) { }
        public void RecordCatabolicRebirthResurrection(int playerId, int resurrectedCells) { }
        public void RecordRegenerativeHyphaeReclaim(int playerId) { }
        public void ReportSporicidalSporeDrop(int playerId, int count) { }
        public void ReportNecrosporeDrop(int playerId, int count) { }
        public void RecordNecrophyticBloomPatchCreation(int playerId, int createdPatchCount) { }
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
    }
}
