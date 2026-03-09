using FungusToast.Core.Campaign;

namespace FungusToast.Core.Players
{
    public class PlayerAdaptation
    {
        public int PlayerId { get; }
        public AdaptationDefinition Adaptation { get; }
        public bool HasTriggered { get; private set; }

        public PlayerAdaptation(int playerId, AdaptationDefinition adaptation)
        {
            PlayerId = playerId;
            Adaptation = adaptation;
        }

        public void MarkTriggered()
        {
            HasTriggered = true;
        }
    }
}