using System;
using FungusToast.Unity.UI.Tooltips;
using UnityEngine;

namespace FungusToast.Unity.UI.Tooltips.TooltipProviders
{
    /// <summary>
    /// Supplies per-mold starting adaptation tooltip text to the auto-resolved TooltipTrigger.
    /// Add this before TooltipTrigger so Awake() can discover it as the dynamic provider.
    /// </summary>
    public class MoldButtonTooltipProvider : MonoBehaviour, ITooltipContentProvider
    {
        private string tooltipText;
        private Func<string> tooltipResolver;

        public void Initialize(string text)
        {
            tooltipText = text;
            tooltipResolver = null;
        }

        public void Initialize(Func<string> resolver)
        {
            tooltipResolver = resolver;
            tooltipText = null;
        }

        public string GetTooltipText() => tooltipResolver?.Invoke() ?? tooltipText ?? string.Empty;
    }
}
