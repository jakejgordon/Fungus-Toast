using System.Collections;
using UnityEngine;

namespace FungusToast.Unity.UI.Onboarding
{
    /// <summary>Uses the shared draggable HUD card; retires before other round-one teaching starts.</summary>
    public static class RotIntroCoachmark
    {
        private const float Width = 620f;
        private const float MinimumHeight = 180f;
        private const float ContentPadding = 20f;
        private const float TitleFontSize = 28f;
        private const float BodyFontSize = 21f;
        private const float TitleRowHeight = 42f;

        public static IEnumerator Show(Canvas rootCanvas)
        {
            if (rootCanvas == null || NewPlayerTooltipCatalog.HasBeenSeen(NewPlayerTooltipId.RotIntro)) yield break;
            bool dismissed = false;
            var card = CoachmarkLayoutUtility.BuildCard(
                "UI_RotIntroCoachmark", rootCanvas.transform,
                new Vector2(Width, MinimumHeight), () => dismissed = true,
                TitleFontSize, BodyFontSize, pivot: new Vector2(0.5f, 0.5f),
                titleRowHeight: TitleRowHeight, contentInset: ContentPadding);
            try
            {
                card.Root.anchoredPosition = Vector2.zero;
                card.Show(NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.RotIntro));
                float bodyHeight = card.Body.GetPreferredValues(card.Body.text, Width - ContentPadding * 2f, 0f).y;
                float height = CoachmarkLayoutUtility.TitleTopInset + TitleRowHeight + 6f + bodyHeight
                    + ContentPadding + CoachmarkLayoutUtility.BodyBottomExtraInset;
                card.Root.sizeDelta = new Vector2(Width, Mathf.Max(MinimumHeight, height));
                while (!dismissed && card.Root != null) yield return null;
                if (dismissed) NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.RotIntro);
            }
            finally
            {
                card.HideImmediate();
                if (card.Root != null) Object.Destroy(card.Root.gameObject);
            }
        }
    }
}
