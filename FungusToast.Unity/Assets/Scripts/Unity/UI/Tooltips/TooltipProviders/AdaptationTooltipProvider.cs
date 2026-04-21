using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.UI.Tooltips;

namespace FungusToast.Unity.UI.Tooltips.TooltipProviders
{
    public class AdaptationTooltipProvider : UnityEngine.MonoBehaviour, ITooltipContentProvider
    {
        private AdaptationDefinition adaptation;
        private PlayerAdaptation playerAdaptation;

        public void Initialize(AdaptationDefinition definition)
        {
            adaptation = definition;
            playerAdaptation = null;
        }

        public void Initialize(PlayerAdaptation ownedAdaptation)
        {
            playerAdaptation = ownedAdaptation;
            adaptation = ownedAdaptation?.Adaptation;
        }

        public string GetTooltipText()
        {
            if (adaptation == null)
            {
                return "<b>Adaptation</b>\nUnset";
            }

            string adaptationType = adaptation.IsStartingAdaptation ? "Starting Adaptation" : "Adaptation";
            int boardWidth = FungusToast.Unity.GameManager.Instance?.Board?.Width ?? FungusToast.Core.Config.GameBalance.BoardWidth;
            string description = playerAdaptation != null
                ? AdaptationRepository.GetTooltipDescription(playerAdaptation, boardWidth)
                : AdaptationRepository.GetTooltipDescription(adaptation, boardWidth);
            return $"<b>{adaptation.Name}</b>\n<i>{adaptationType}</i>\n\n{description}";
        }
    }

    public class MycovariantTooltipProvider : UnityEngine.MonoBehaviour, ITooltipContentProvider
    {
        private Mycovariant mycovariant;

        public void Initialize(Mycovariant definition)
        {
            mycovariant = definition;
        }

        public string GetTooltipText()
        {
            if (mycovariant == null)
            {
                return "<b>Mycovariant</b>\nUnset";
            }

            return $"<b>{mycovariant.Name}</b>\n<i>Mycovariant · {mycovariant.Category}</i>\n\n{mycovariant.Description}";
        }
    }

    public class BoardOverlayLegendTooltipProvider : UnityEngine.MonoBehaviour, ITooltipContentProvider
    {
        private BoardOverlayLegendType overlayType;

        public void Initialize(BoardOverlayLegendType type)
        {
            overlayType = type;
        }

        public string GetTooltipText()
        {
            return overlayType switch
            {
                BoardOverlayLegendType.ResistanceShield => "<b>Resistance Shield</b>\n<i>Board Overlay</i>\n\nShows a resistant cell. Resistant cells cannot be killed, displaced, or lost to random decay.",
                BoardOverlayLegendType.Toxin => "<b>Toxin</b>\n<i>Board Overlay</i>\n\nShows a toxin cell occupying the tile as a poisonous hazard.",
                BoardOverlayLegendType.DeadCell => "<b>Dead Cell</b>\n<i>Board Overlay</i>\n\nShows a dead cell that no longer grows, but may still matter to reclaim effects.",
                BoardOverlayLegendType.Chemobeacon => "<b>Chemobeacon</b>\n<i>Board Overlay</i>\n\nShows an active chemobeacon that attracts growth and blocks normal occupation while it remains on the board.",
                BoardOverlayLegendType.AdaptogenPatch => BuildNutrientPatchTooltip(NutrientPatch.CreateAdaptogenCluster(clusterId: 0, clusterTileCount: 4)),
                BoardOverlayLegendType.SporemealPatch => BuildNutrientPatchTooltip(NutrientPatch.CreateSporemealCluster(clusterId: 0, clusterTileCount: 4)),
                BoardOverlayLegendType.HypervariationPatch => BuildNutrientPatchTooltip(NutrientPatch.CreateHypervariationCluster(clusterId: 0, clusterTileCount: 4)),
                _ => "<b>Board Overlay</b>\nUnset"
            };
        }

        private static string BuildNutrientPatchTooltip(NutrientPatch nutrientPatch)
        {
            if (nutrientPatch == null)
            {
                return "<b>Nutrient Patch</b>\nUnset";
            }

            return $"<b>{nutrientPatch.DisplayName}</b>\n<i>Board Overlay</i>\n\n{nutrientPatch.Description}";
        }
    }
}
