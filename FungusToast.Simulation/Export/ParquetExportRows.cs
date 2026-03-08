namespace FungusToast.Simulation.Export
{
    public sealed class GameExportRow
    {
        public string ExperimentId { get; set; } = string.Empty;
        public DateTime RunTimestampUtc { get; set; }
        public int GameIndex { get; set; }
        public int GameSeed { get; set; }
        public string StrategySet { get; set; } = string.Empty;
        public string SlotAssignmentPolicy { get; set; } = string.Empty;
        public int BoardWidth { get; set; }
        public int BoardHeight { get; set; }
        public int PlayerCount { get; set; }
        public int TurnsPlayed { get; set; }
        public int WinnerPlayerId { get; set; }
        public int ToxicTileCount { get; set; }
        public bool ParityAllPassed { get; set; }
    }

    public sealed class PlayerExportRow
    {
        public string ExperimentId { get; set; } = string.Empty;
        public int GameIndex { get; set; }
        public int GameSeed { get; set; }
        public int PlayerId { get; set; }
        public int AssignedSlot { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public string StrategyTheme { get; set; } = string.Empty;
        public string DominantOpponentTheme { get; set; } = string.Empty;
        public string OpponentThemeSet { get; set; } = string.Empty;
        public int UniqueOpponentThemes { get; set; }
        public bool IsWinner { get; set; }
        public int LivingCells { get; set; }
        public int DeadCells { get; set; }
        public int EndGameToxinCells { get; set; }
        public int MutationPointIncome { get; set; }
        public int TotalMutationPointsSpent { get; set; }
        public int BankedPoints { get; set; }
        public float EffectiveGrowthChance { get; set; }
        public float EffectiveSelfDeathChance { get; set; }
        public float OffensiveDecayModifier { get; set; }
        public float? AvgAIScoreAtDraft { get; set; }
    }

    public sealed class MutationExportRow
    {
        public string ExperimentId { get; set; } = string.Empty;
        public int GameIndex { get; set; }
        public int GameSeed { get; set; }
        public int PlayerId { get; set; }
        public string StrategyName { get; set; } = string.Empty;
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
