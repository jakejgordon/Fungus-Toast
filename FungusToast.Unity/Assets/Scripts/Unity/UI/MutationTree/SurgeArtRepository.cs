using System.Collections.Generic;
using FungusToast.Core.Mutations;
using UnityEngine;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Procedural icons for the Mycelial Surge mutations, mirroring
    /// <see cref="FungusToast.Unity.UI.Campaign.AdaptationArtRepository"/>. Only surges get art:
    /// they are the one transient mutation state that has to be recognisable outside the tree
    /// (sidebar, player inspector), and sharing one glyph across every surface is what lets a
    /// player connect the tree card to the countdown elsewhere. Every icon keeps the Mycelial
    /// Surges category tint as its background so it reads as "a mutation", not an adaptation or
    /// mycovariant; the accent colour varies per surge so the six stay distinct at 28px.
    /// </summary>
    public static class SurgeArtRepository
    {
        private const int IconSize = ProceduralIconUtility.DefaultIconSize;
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
            var background = Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.Surface.PanelPrimary, 0.4f);
            var accent = Color.Lerp(ResolveAccent(mutationId), UIStyleTokens.Text.Primary, 0.28f);

            return ProceduralIconUtility.CreateSprite(
                $"SurgeIcon_{mutationId}",
                background,
                accent,
                (texture, drawAccent, highlight) =>
                {
                    switch (mutationId)
                    {
                        case MutationIds.HyphalSurge:
                            DrawAutolyticSurge(texture, drawAccent, highlight);
                            break;
                        case MutationIds.NecroticClearance:
                            DrawNecroticClearance(texture, drawAccent, highlight);
                            break;
                        case MutationIds.ChemotacticBeacon:
                            DrawChemotacticBeacon(texture, drawAccent, highlight);
                            break;
                        case MutationIds.MimeticResilience:
                            DrawMimeticResilience(texture, drawAccent, highlight);
                            break;
                        case MutationIds.CompetitiveAntagonism:
                            DrawCompetitiveAntagonism(texture, drawAccent, highlight);
                            break;
                        case MutationIds.ChitinFortification:
                            DrawChitinFortification(texture, drawAccent, highlight);
                            break;
                        default:
                            DrawFallback(texture, drawAccent, highlight);
                            break;
                    }
                },
                IconSize);
        }

        private static Color ResolveAccent(int mutationId)
        {
            return mutationId switch
            {
                MutationIds.HyphalSurge => UIStyleTokens.Accent.Lichen,
                MutationIds.NecroticClearance => UIStyleTokens.Accent.Spore,
                MutationIds.ChemotacticBeacon => UIStyleTokens.State.Warning,
                MutationIds.MimeticResilience => UIStyleTokens.State.Info,
                MutationIds.CompetitiveAntagonism => UIStyleTokens.State.Danger,
                MutationIds.ChitinFortification => UIStyleTokens.Accent.Hyphae,
                _ => UIStyleTokens.Text.Secondary
            };
        }

        /// <summary>Rising burst over a dissolving base: growth bought with the colony's own cells.</summary>
        private static void DrawAutolyticSurge(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.DrawLine(texture, 20, 30, 20, 9, accent, 2);
            ProceduralIconUtility.DrawLine(texture, 20, 9, 13, 16, accent, 2);
            ProceduralIconUtility.DrawLine(texture, 20, 9, 27, 16, accent, 2);
            ProceduralIconUtility.DrawLine(texture, 12, 24, 16, 18, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 28, 24, 24, 18, highlight, 1);
            ProceduralIconUtility.FillCircle(texture, 12, 32, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 20, 34, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 28, 32, 2, highlight);
        }

        /// <summary>A dead cell struck through: corpses removed before rivals can reclaim them.</summary>
        private static void DrawNecroticClearance(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.DrawRing(texture, 20, 20, 10, 2, accent);
            ProceduralIconUtility.FillCircle(texture, 20, 20, 5, Color.Lerp(accent, UIStyleTokens.Surface.Canvas, 0.55f));
            ProceduralIconUtility.DrawLine(texture, 12, 12, 28, 28, highlight, 2);
            ProceduralIconUtility.FillCircle(texture, 9, 30, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 31, 10, 2, highlight);
        }

        /// <summary>Target rings with a growth line running in from the corner.</summary>
        private static void DrawChemotacticBeacon(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.DrawLine(texture, 6, 34, 20, 20, accent, 2);
            ProceduralIconUtility.DrawRing(texture, 24, 16, 9, 2, accent);
            ProceduralIconUtility.DrawRing(texture, 24, 16, 4, 1, highlight);
            ProceduralIconUtility.FillCircle(texture, 24, 16, 2, highlight);
            ProceduralIconUtility.FillCircle(texture, 6, 34, 3, highlight);
        }

        /// <summary>A rival's outlined shield beside the colony's filled copy of it.</summary>
        private static void DrawMimeticResilience(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.FillShield(texture, 13, 20, 7, 10, accent);
            ProceduralIconUtility.FillShield(texture, 13, 20, 4, 6, Color.Lerp(accent, UIStyleTokens.Surface.Canvas, 0.5f));
            ProceduralIconUtility.FillShield(texture, 27, 20, 7, 10, highlight);
            ProceduralIconUtility.DrawLine(texture, 17, 8, 23, 8, highlight, 1);
            ProceduralIconUtility.FillCircle(texture, 23, 8, 2, highlight);
        }

        /// <summary>Crosshair over the largest colony, the small one left alone.</summary>
        private static void DrawCompetitiveAntagonism(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.FillCircle(texture, 25, 17, 7, accent);
            ProceduralIconUtility.DrawRing(texture, 25, 17, 10, 1, highlight);
            ProceduralIconUtility.DrawLine(texture, 25, 4, 25, 30, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 12, 17, 38, 17, highlight, 1);
            ProceduralIconUtility.FillCircle(texture, 10, 30, 3, Color.Lerp(accent, UIStyleTokens.Surface.Canvas, 0.35f));
        }

        /// <summary>A plated shield: resistance laid down in permanent layers.</summary>
        private static void DrawChitinFortification(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.FillShield(texture, 20, 20, 10, 13, accent);
            ProceduralIconUtility.DrawLine(texture, 11, 15, 29, 15, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 12, 21, 28, 21, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 14, 27, 26, 27, highlight, 1);
            ProceduralIconUtility.DrawLine(texture, 20, 8, 20, 32, highlight, 1);
        }

        private static void DrawFallback(Texture2D texture, Color accent, Color highlight)
        {
            ProceduralIconUtility.DrawRing(texture, 20, 20, 9, 2, accent);
            ProceduralIconUtility.FillCircle(texture, 20, 20, 3, highlight);
        }
    }
}
