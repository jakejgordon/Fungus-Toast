namespace FungusToast.Core.Metrics
{
    public interface ISporeDropObserver
    {
        void ReportSporocidalSporeDrop(int playerId, int count);
        void ReportNecrosporeDrop(int playerId, int count);
        void ReportNecrophyticBloomSporeDrop(int playerId, int sporesDropped, int successfulReclaims);
    }
}
