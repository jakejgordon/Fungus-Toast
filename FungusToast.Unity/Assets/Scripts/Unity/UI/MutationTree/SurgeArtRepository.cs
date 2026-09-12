using System.Collections.Generic;
using FungusToast.Core.Mutations;
using FungusToast.Unity.UI.Icons;
using UnityEngine;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Sprite cache for the Mycelial Surge icons. Only surges get art: they are the one
    /// transient mutation state that has to be recognisable outside the tree (sidebar, player
    /// inspector), and sharing one glyph across every surface is what lets a player connect the
    /// tree card to the countdown elsewhere. The drawings live in <see cref="SurgeIcons"/>.
    /// </summary>
    public static class SurgeArtRepository
    {
        private static readonly Dictionary<int, Sprite> Cache = new();

        public static Sprite GetIcon(Mutation mutation)
        {
            return GetIcon(mutation != null ? mutation.Id : -1);
        }

        public static Sprite GetIcon(int mutationId)
        {
            if (Cache.TryGetValue(mutationId, out var cached))
            {
                return cached;
            }

            var sprite = BuildIcon(mutationId);
            Cache[mutationId] = sprite;
            return sprite;
        }

        private static Sprite BuildIcon(int mutationId)
        {
#if UNITY_EDITOR
            if (!SurgeIcons.HasDedicatedIcon(mutationId))
            {
                Debug.LogWarning($"SurgeArtRepository: no dedicated icon for mutation {mutationId}; drawing the fallback. Add a case to SurgeIcons.");
            }
#endif
            var canvas = new IconCanvas();
            SurgeIcons.Draw(canvas, mutationId);
            return canvas.ToSprite($"SurgeIcon_{mutationId}");
        }
    }
}
