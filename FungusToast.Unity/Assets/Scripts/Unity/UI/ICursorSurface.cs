namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Lets a UI surface tell <see cref="CursorManager"/> which hardware cursor belongs
    /// over it. The manager walks up from the top raycast hit and the nearest surface with
    /// an opinion wins; an interactable <see cref="UnityEngine.UI.Selectable"/> counts as
    /// asking for the hand without implementing this. Return null to defer to whatever is
    /// further up (and finally to the override stack or the arrow).
    ///
    /// The rule each cursor follows:
    /// hand = this control's primary action is available right now (a mold icon whose only
    /// action is pinning the inspector; a mutation card relies on its button's interactable
    /// state instead, so a locked card stays bare even though it can still be pinned);
    /// move = this surface goes where you drag it (a <see cref="DraggableCard"/>);
    /// arrow = hover is the whole interaction (an icon tile that only shows a tooltip), which
    /// also stops a draggable ancestor's move cursor from showing over it.
    /// </summary>
    public interface ICursorSurface
    {
        CursorKind? PreferredCursor { get; }
    }
}
