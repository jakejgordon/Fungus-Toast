namespace FungusToast.Core.Core.Metrics
{
    public interface IGrowthObserver
    {
        void RecordCreepingMoldMove(int playerId);
    }
}