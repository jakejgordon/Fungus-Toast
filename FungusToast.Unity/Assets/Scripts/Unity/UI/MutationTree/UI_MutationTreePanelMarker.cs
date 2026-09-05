using UnityEngine;

namespace FungusToast.Unity.UI.MutationTree
{
    // Marks the mutation tree's root panel GameObject so UI_MutationManager can
    // locate it via FindAnyObjectByType instead of an Inspector-wired reference
    // that crosses from GameUIManager's organizational hierarchy into the
    // Canvas's visual one. Carries no behavior of its own.
    public class UI_MutationTreePanelMarker : MonoBehaviour
    {
    }
}
