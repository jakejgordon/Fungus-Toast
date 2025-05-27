namespace FungusToast.Core.Phases
{
    public interface IGrowthObserver
    {
        void RecordCreepingMoldMove(int playerId);
    }
}