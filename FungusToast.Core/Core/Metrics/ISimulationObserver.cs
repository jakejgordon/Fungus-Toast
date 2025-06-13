using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;

namespace FungusToast.Core.Metrics
{
    public interface ISimulationObserver
    {
        void RecordMutatorPhenotypeMutationPointsEarned(int playerId, int freePointsEarned);
        void RecordHyperadaptiveDriftMutationPointsEarned(int playerId, int freePointsEarned);
        void RecordAdaptiveExpressionBonus(int playerId, int bonus);

        void RecordCellDeath(int playerId, DeathReason reason, int deathCount = 1);
        void RecordCreepingMoldMove(int playerId);
        void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount);
        void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount);
        void RecordTendrilGrowth(int playerId, DiagonalDirection value);
        void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints);
        void RecordNecrotoxicConversionReclaim(int playerId, int necrotoxicConversions);

        void ReportSporocidalSporeDrop(int playerId, int count);
        void ReportNecrosporeDrop(int playerId, int count);
        void ReportNecrophyticBloomSporeDrop(int playerId, int sporesDropped, int successfulReclaims);
        void ReportMycotoxinTracerSporeDrop(int playerId, int sporesDropped);
        void RecordMutationPointIncome(int playerId, int newMutationPoints);
        void RecordMutationPointsSpent(int playerId, MutationTier mutationTier, int pointsPerUpgrade);
        void RecordHyphalSurgeGrowth(int playerId);
    }
}