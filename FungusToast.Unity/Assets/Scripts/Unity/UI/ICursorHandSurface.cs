namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Opt-in for a clickable surface that is not a <see cref="UnityEngine.UI.Selectable"/>
    /// but whose primary action is a click, so <see cref="CursorManager"/> shows the hand
    /// over it. The rule the cursor follows is "hand = this control's primary action is
    /// available right now": a mold icon's only action is pinning the inspector, so it
    /// implements this; a mutation card's primary action is buying, so it relies on its
    /// button's interactable state instead and a locked card stays bare even though it
    /// can still be pinned.
    /// </summary>
    public interface ICursorHandSurface
    {
        bool ShowsHandCursor { get; }
    }
}
