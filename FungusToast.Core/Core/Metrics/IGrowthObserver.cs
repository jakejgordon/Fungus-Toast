using FungusToast.Core.Growth;

namespace FungusToast.Core.Core.Metrics
{
    public interface IGrowthObserver
    {
        void RecordCreepingMoldMove(int playerId);
        void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount);
        void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount);
        void RecordTendrilGrowth(int playerId, DiagonalDirection value);
        void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints);
    }
}