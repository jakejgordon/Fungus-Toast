namespace FungusToast.Core.Core.Metrics
{
    public interface IGrowthObserver
    {
        void RecordCreepingMoldMove(int playerId);
        void RecordToxinCatabolism(int playerId, int count);
    }
}