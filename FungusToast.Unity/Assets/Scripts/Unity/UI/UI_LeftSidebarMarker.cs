using UnityEngine;

namespace FungusToast.Unity.UI
{
    // Marks the left sidebar's root GameObject so GameUIManager can locate it
    // via FindAnyObjectByType instead of an Inspector-wired reference. The
    // GameObject has no bespoke script of its own (just layout components),
    // so nothing else could type-safely target it. Carries no behavior.
    public class UI_LeftSidebarMarker : MonoBehaviour
    {
    }
}
