namespace FungusToast.Core.Core.Metrics
{
    public interface IGrowthObserver
    {
        void RecordCreepingMoldMove(int playerId);
        void RecordNecrohyphalInfiltration(int playerId, int necrohyphalInfiltrationCount);
        void RecordNecrohyphalInfiltrationCascade(int playerId, int cascadeCount);
        void RecordToxinCatabolism(int playerId, int toxinsCatabolized, int catabolizedMutationPoints);
    }
}