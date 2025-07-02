using System;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Central location for UI effect timing/delay constants used in mycovariant effect coroutines.
    /// </summary>
    public static class UIEffectConstants
    {
        /// <summary>
        /// Delay (in seconds) after AI resolves Jetting Mycelium before continuing.
        /// </summary>
        public const float JettingMyceliumAIDelaySeconds = 0.6f;

        /// <summary>
        /// Delay (in seconds) after AI resolves Mycelial Bastion before continuing.
        /// </summary>
        public const float MycelialBastionAIDelaySeconds = 0.6f;

        /// <summary>
        /// Delay (in seconds) after AI resolves Surgical Inoculation before continuing.
        /// </summary>
        public const float SurgicalInoculationAIDelaySeconds = 0.6f;
    }
} 