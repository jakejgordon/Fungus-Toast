// Assets/TooltipProbe.cs
using FungusToast.Unity.UI.Tooltips;
using System.Linq;
using UnityEngine;

public class TooltipProbe : MonoBehaviour
{
    [ContextMenu("Probe HomeostaticHarmonyTooltipProvider")]
    void ProbeNow()
    {
        var providers = FindObjectsByType<HomeostaticHarmonyTooltipProvider>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        var activeCount = providers.Count(p => p.isActiveAndEnabled);
        Debug.Log($"Found {providers.Length} HomeostaticHarmonyTooltipProvider in scene. Active: {activeCount}");
    }
}
