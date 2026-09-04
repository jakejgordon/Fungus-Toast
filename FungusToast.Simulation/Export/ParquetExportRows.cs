namespace FungusToast.Simulation.Export
{
    public sealed class LivingCellSourceExportRow
    {
        public string ExperimentId { get; set; } = string.Empty;
        public int GameIndex { get; set; }
        public int GameSeed { get; set; }
        public string RandomStreamContractVersion { get; set; } = string.Empty;
        public int PlayerId { get; set; }
        public int AssignedSlot { get; set; }
        public int SelectedLineupOrder { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public string StrategyId { get; set; } = string.Empty;
        public string StrategyDefinitionFingerprint { get; set; } = string.Empty;
        public string StrategyTheme { get; set; } = string.Empty;
        public string GrowthSource { get; set; } = string.Empty;
        public string GrowthSourceDisplayName { get; set; } = string.Empty;
        public int LivingCellCount { get; set; }
    }

    public sealed class GameExportRow
    {
        public string ExperimentId { get; set; } = string.Empty;
        public string ConditionId { get; set; } = string.Empty;
        public string ConditionFingerprint { get; set; } = string.Empty;
        public DateTime RunTimestampUtc { get; set; }
        public int GameIndex { get; set; }
        public int GameSeed { get; set; }
        public string RandomStreamContractVersion { get; set; } = string.Empty;
        public string StrategySet { get; set; } = string.Empty;
        public string StrategySelectionPolicy { get; set; } = string.Empty;
        public string StrategySelectionSource { get; set; } = string.Empty;
        public string SelectedStrategyLineup { get; set; } = string.Empty;
        public string AssignedStrategyLineup { get; set; } = string.Empty;
        public string SelectedStrategyIds { get; set; } = string.Empty;
        public string AssignedStrategyIds { get; set; } = string.Empty;
        public string SlotAssignmentPolicy { get; set; } = string.Empty;
        public int BoardWidth { get; set; }
        public int BoardHeight { get; set; }
        public string BoardGeometryId { get; set; } = string.Empty;
        public string BoardGeometryFingerprint { get; set; } = string.Empty;
        public int BlockedTileCount { get; set; }
        public string BlockedTileIds { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public bool NutrientPatchesEnabled { get; set; }
        public bool MycovariantDraftEnabled { get; set; }
        public string StartingPositionMode { get; set; } = string.Empty;
        public string ConfiguredStartingPositions { get; set; } = string.Empty;
        public string ConfiguredPreferredPositionPools { get; set; } = string.Empty;
        public string ConfiguredStartingAdaptations { get; set; } = string.Empty;
        public string ActualStartingPositions { get; set; } = string.Empty;
        public string ActualStartingAdaptations { get; set; } = string.Empty;
        public int TurnsPlayed { get; set; }
        public int WinnerPlayerId { get; set; }
        public string WinnerPlayerIds { get; set; } = string.Empty;
        public int ToxicTileCount { get; set; }
        public int NutrientPatchCount { get; set; }
        public bool ParityAllPassed { get; set; }
    }

    public sealed class PlayerExportRow
    {
        public string ExperimentId { get; set; } = string.Empty;
        public string ConditionId { get; set; } = string.Empty;
        public string ConditionFingerprint { get; set; } = string.Empty;
        public string InputSchemaVersion { get; set; } = string.Empty;
        public string StrategySet { get; set; } = string.Empty;
        public string StrategySelectionPolicy { get; set; } = string.Empty;
        public string SlotAssignmentPolicy { get; set; } = string.Empty;
        public string StartingPositionMode { get; set; } = string.Empty;
        public int GameIndex { get; set; }
        public int GameSeed { get; set; }
        public string RandomStreamContractVersion { get; set; } = string.Empty;
        public int PlayerId { get; set; }
        public int AssignedSlot { get; set; }
        public int SelectedLineupOrder { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public string StrategyId { get; set; } = string.Empty;
        public string StrategyDefinitionFingerprint { get; set; } = string.Empty;
        public string StrategyTheme { get; set; } = string.Empty;
        public string StrategyStatus { get; set; } = string.Empty;
        public int StartingX { get; set; }
        public int StartingY { get; set; }
        public string StartingAdaptationIds { get; set; } = string.Empty;
        public int BoardWidth { get; set; }
        public int BoardHeight { get; set; }
        public string BoardGeometryId { get; set; } = string.Empty;
        public string BoardGeometryFingerprint { get; set; } = string.Empty;
        public int BlockedTileCount { get; set; }
        public int PlayerCount { get; set; }
        public bool NutrientPatchesEnabled { get; set; }
        public bool MycovariantDraftEnabled { get; set; }
        public string DominantOpponentTheme { get; set; } = string.Empty;
        public string OpponentThemeSet { get; set; } = string.Empty;
        public int UniqueOpponentThemes { get; set; }
        public bool IsWinner { get; set; }
        public double WinCredit { get; set; }
        public string OutcomeStatus { get; set; } = string.Empty;
        public int LivingCells { get; set; }
        public int TotalLivingCells { get; set; }
        public int FinalRank { get; set; }
        public int PlayersTiedAtFinalRank { get; set; }
        public int DeadCells { get; set; }
        public int EndGameToxinCells { get; set; }
        public int NutrientClaims { get; set; }
        public int NutrientMutationPointsEarned { get; set; }
        public float AvgNutrientClusterSize { get; set; }
        public int MutationPointIncome { get; set; }
        public int TotalMutationPointsSpent { get; set; }
        public int BankedPoints { get; set; }
        public float EffectiveGrowthChance { get; set; }
        public float EffectiveSelfDeathChance { get; set; }
        public int FilamentOverdriveTriggers { get; set; }
        public int FilamentOverdriveBonusCells { get; set; }
        public int FilamentOverdriveSourceDeaths { get; set; }
        public int FilamentOverdriveNetImmediateCells { get; set; }
        public float? AvgAIScoreAtDraft { get; set; }
    }

    public sealed class MutationExportRow
    {
        public string ExperimentId { get; set; } = string.Empty;
        public int GameIndex { get; set; }
        public int GameSeed { get; set; }
        public int PlayerId { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public string StrategyId { get; set; } = string.Empty;
        public string StrategyDefinitionFingerprint { get; set; } = string.Empty;
        public string StrategyTheme { get; set; } = string.Empty;
        public int MutationId { get; set; }
        public string MutationName { get; set; } = string.Empty;
        public string MutationTier { get; set; } = string.Empty;
        public string MutationCategory { get; set; } = string.Empty;
        public int MutationLevel { get; set; }
        public int? FirstUpgradeRound { get; set; }
    }

    public sealed class MycovariantExportRow
    {
        public string ExperimentId { get; set; } = string.Empty;
        public int GameIndex { get; set; }
        public int GameSeed { get; set; }
        public int PlayerId { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public string StrategyId { get; set; } = string.Empty;
        public string StrategyDefinitionFingerprint { get; set; } = string.Empty;
        public string StrategyTheme { get; set; } = string.Empty;
        public int MycovariantId { get; set; }
        public string MycovariantName { get; set; } = string.Empty;
        public string MycovariantType { get; set; } = string.Empty;
        public bool IsUniversal { get; set; }
        public bool Triggered { get; set; }
        public float? AIScoreAtDraft { get; set; }
        public string EffectType { get; set; } = string.Empty;
        public int EffectValue { get; set; }
    }

    public sealed class MutationUpgradeEventExportRow
    {
        public string ExperimentId { get; set; } = string.Empty;
        public int GameIndex { get; set; }
        public int GameSeed { get; set; }
        public int PlayerId { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public string StrategyId { get; set; } = string.Empty;
        public string StrategyDefinitionFingerprint { get; set; } = string.Empty;
        public string StrategyTheme { get; set; } = string.Empty;
        public int Round { get; set; }
        public int MutationId { get; set; }
        public string MutationName { get; set; } = string.Empty;
        public string MutationTier { get; set; } = string.Empty;
        public int OldLevel { get; set; }
        public int NewLevel { get; set; }
        public int MutationPointsBefore { get; set; }
        public int MutationPointsAfter { get; set; }
        public int PointsSpent { get; set; }
        public string UpgradeSource { get; set; } = string.Empty;
    }
}
