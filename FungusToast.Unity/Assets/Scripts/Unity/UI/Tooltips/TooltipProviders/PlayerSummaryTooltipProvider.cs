#nullable enable

using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Players;
using FungusToast.Unity.UI.PlayerInspector;

namespace FungusToast.Unity.UI.Tooltips.TooltipProviders
{
    /// <summary>
    /// Supplies tooltip content for a player's mold icon in the player summary row.
    /// Content is derived by <see cref="PlayerInspectorContent"/> so this hover surface and any
    /// future docked player inspector stay in sync. When Development Testing is enabled the
    /// AI's declared tuning parameters are appended.
    /// </summary>
    public class PlayerSummaryTooltipProvider : MonoBehaviour, ITooltipContentProvider
    {
        private Player? player;

        /// <summary>
        /// Initialize this provider with the player whose data to display.
        /// </summary>
        public void Initialize(Player targetPlayer)
        {
            player = targetPlayer;
        }

        public string GetTooltipText()
        {
            var manager = GameManager.Instance;
            var sections = new List<PlayerInspectorSection>(
                PlayerInspectorContent.BuildSummarySections(player, manager));

            if (PlayerInspectorContent.IsDevelopmentTestingEnabled(manager))
            {
                sections.AddRange(PlayerInspectorContent.BuildDevelopmentSections(player));
            }

            return PlayerInspectorMarkup.Render(sections);
        }
    }
}
