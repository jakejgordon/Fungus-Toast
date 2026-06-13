using TMPro;

namespace FungusToast.Unity.UI
{
    internal static class TMPOverflowUtility
    {
        public static void SetSafeEllipsis(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            // LiberationSans SDF currently emits TMP ellipsis glyph warnings on some runtime-created labels.
            // Centralizing the fallback keeps those warnings out of layout / startup code paths.
            text.overflowMode = TextOverflowModes.Truncate;
        }

        public static float GetPreferredWidthWithoutEllipsis(TMP_Text text, string value = null)
        {
            if (text == null)
            {
                return 0f;
            }

            TextOverflowModes originalOverflow = text.overflowMode;
            if (originalOverflow == TextOverflowModes.Ellipsis)
            {
                text.overflowMode = TextOverflowModes.Truncate;
            }

            try
            {
                return text.GetPreferredValues(value ?? text.text).x;
            }
            finally
            {
                text.overflowMode = originalOverflow;
            }
        }
    }
}
