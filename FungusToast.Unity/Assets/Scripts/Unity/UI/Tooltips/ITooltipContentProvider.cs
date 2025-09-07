using System;

namespace FungusToast.Unity.UI.Tooltips
{
    /// <summary>
    /// Implement on a MonoBehaviour to supply dynamic tooltip text just-in-time when shown.
    /// </summary>
    public interface ITooltipContentProvider
    {
        string GetTooltipText();
    }
}
