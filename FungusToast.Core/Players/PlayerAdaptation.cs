using FungusToast.Core.Campaign;

namespace FungusToast.Core.Players
{
    public class PlayerAdaptation
    {
        public int PlayerId { get; }
        public AdaptationDefinition Adaptation { get; }
        public bool HasTriggered { get; private set; }
        public bool HasRuntimeValue { get; private set; }
        public int RuntimeValue { get; private set; }

        public PlayerAdaptation(int playerId, AdaptationDefinition adaptation)
        {
            PlayerId = playerId;
            Adaptation = adaptation;
        }

        public void MarkTriggered()
        {
            HasTriggered = true;
        }

        public void SetRuntimeValue(int value)
        {
            RuntimeValue = value;
            HasRuntimeValue = true;
        }

        public void ClearRuntimeValue()
        {
            RuntimeValue = 0;
            HasRuntimeValue = false;
        }
    }
}