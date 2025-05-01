using System.Collections.Generic;
using UnityEngine;
using FungusToast.Core.Players;

namespace FungusToast.UI
{
    public class UI_PlayerBinder : MonoBehaviour
    {
        private Dictionary<Player, Sprite> playerMoldIcons = new();

        /// <summary>
        /// Assigns a mold icon sprite to a player.
        /// </summary>
        public void AssignIcon(Player player, Sprite sprite)
        {
            if (player == null || sprite == null)
            {
                Debug.LogWarning("⚠️ Cannot assign icon: player or sprite is null.");
                return;
            }

            playerMoldIcons[player] = sprite;
        }

        /// <summary>
        /// Retrieves the icon assigned to a player. Logs an error if not found.
        /// </summary>
        public Sprite GetIcon(Player player)
        {
            if (player == null)
            {
                Debug.LogError("❌ GetIcon called with null player.");
                return null;
            }

            if (playerMoldIcons.TryGetValue(player, out var sprite))
            {
                return sprite;
            }

            Debug.LogError($"❌ No icon found for PlayerId {player.PlayerId} in UI_PlayerBinder.");
            return null;
        }
    }
}
