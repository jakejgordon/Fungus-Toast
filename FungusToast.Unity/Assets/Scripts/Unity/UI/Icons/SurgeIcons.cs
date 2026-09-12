using System;
using FungusToast.Core.Mutations;
using UnityEngine;

namespace FungusToast.Unity.UI.Icons
{
    /// <summary>
    /// Drawings for the Mycelial Surge icons. Each icon is a small diagram of what the surge
    /// does on the board, built from <see cref="IconGlyphs"/>. Every surge keeps the Mycelial
    /// Surges category tint as its background so it reads as "a mutation" next to adaptation
    /// and mycovariant icons; the accent varies per surge so the six stay distinct at 28px.
    ///
    /// This class is pure drawing (no textures, no sprites) so the <c>tools/icon-preview</c>
    /// harness can render it outside Unity; <see cref="MutationTree.SurgeArtRepository"/> owns
    /// the sprite cache.
    /// </summary>
    public static class SurgeIcons
    {
        /// <summary>Frame width in logical units: the same 5% of the icon the old 40px icons used.</summary>
        public const float FrameThickness = 5f;

        public static readonly int[] SurgeMutationIds =
        {
            MutationIds.HyphalSurge,
            MutationIds.NecroticClearance,
            MutationIds.ChemotacticBeacon,
            MutationIds.MimeticResilience,
            MutationIds.CompetitiveAntagonism,
            MutationIds.ChitinFortification
        };

        public static Color Background => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.Surface.PanelPrimary, 0.4f);

        public static bool HasDedicatedIcon(int mutationId) => Array.IndexOf(SurgeMutationIds, mutationId) >= 0;

        public static Color Accent(int mutationId)
        {
            Color raw = mutationId switch
            {
                MutationIds.HyphalSurge => UIStyleTokens.Accent.Lichen,
                MutationIds.NecroticClearance => UIStyleTokens.Accent.Spore,
                MutationIds.ChemotacticBeacon => UIStyleTokens.State.Warning,
                MutationIds.MimeticResilience => UIStyleTokens.State.Info,
                MutationIds.CompetitiveAntagonism => UIStyleTokens.State.Danger,
                MutationIds.ChitinFortification => UIStyleTokens.Accent.Hyphae,
                _ => UIStyleTokens.Text.Secondary
            };
            return Color.Lerp(raw, UIStyleTokens.Text.Primary, 0.28f);
        }

        public static void Draw(IconCanvas canvas, int mutationId)
        {
            Color background = Background;
            Color accent = Accent(mutationId);
            Color highlight = Color.Lerp(accent, Color.white, 0.32f);

            canvas.Fill(background);
            canvas.Frame(accent, FrameThickness);
            DrawFamilyMark(canvas, background, accent);

            switch (mutationId)
            {
                case MutationIds.HyphalSurge:
                    DrawAutolyticSurge(canvas, accent, highlight, background);
                    break;
                case MutationIds.NecroticClearance:
                    DrawNecroticClearance(canvas, accent, highlight);
                    break;
                case MutationIds.ChemotacticBeacon:
                    DrawChemotacticBeacon(canvas, accent, highlight);
                    break;
                case MutationIds.MimeticResilience:
                    DrawMimeticResilience(canvas, accent, highlight);
                    break;
                case MutationIds.CompetitiveAntagonism:
                    DrawCompetitiveAntagonism(canvas, accent, highlight, background);
                    break;
                case MutationIds.ChitinFortification:
                    DrawChitinFortification(canvas, accent);
                    break;
                default:
                    DrawFallback(canvas, accent, highlight);
                    break;
            }
        }

        /// <summary>Surge family tell: a small chevron notched into the top of the frame.</summary>
        private static void DrawFamilyMark(IconCanvas canvas, Color background, Color accent)
        {
            canvas.FillRect(41f, 0f, 18f, 11f, background, 3f);
            IconGlyphs.SurgeChevron(canvas, 50f, 6f, 11f, accent);
        }

        /// <summary>Cells stacking upward while the one at the base dissolves: growth paid for in cells.</summary>
        private static void DrawAutolyticSurge(IconCanvas canvas, Color accent, Color highlight, Color background)
        {
            IconGlyphs.DissolvingCell(canvas, 50f, 79f, 16f, accent);
            canvas.FillCircle(40f, 88f, 1.8f, accent);
            canvas.FillCircle(61f, 90f, 1.5f, accent);
            canvas.FillCircle(58f, 70f, 1.5f, accent);

            IconGlyphs.LivingCell(canvas, 50f, 57f, 16f, accent);
            IconGlyphs.LivingCell(canvas, 31f, 40f, 16f, accent);
            IconGlyphs.LivingCell(canvas, 69f, 40f, 16f, accent);
            canvas.Arrow(50f, 47f, 50f, 17f, 4f, 11f, highlight);
        }

        /// <summary>A dead cell between yours and a rival's, swept away before the rival can claim it.</summary>
        private static void DrawNecroticClearance(IconCanvas canvas, Color accent, Color highlight)
        {
            IconGlyphs.LivingCell(canvas, 24f, 54f, 18f, accent);
            IconGlyphs.DeadCell(canvas, 50f, 54f, 18f, IconGlyphs.Palette.Dead);
            IconGlyphs.EnemyCell(canvas, 76f, 54f, 18f, IconGlyphs.Palette.Enemy);

            canvas.Line(38f, 72f, 63f, 36f, 4.5f, highlight);
            canvas.FillCircle(66f, 27f, 2.2f, highlight);
            canvas.FillCircle(73f, 33f, 1.8f, highlight);
            canvas.FillCircle(59f, 24f, 1.6f, highlight);
        }

        /// <summary>Spore, guide line, growth along it to the marker, then a clockwise spiral around it.</summary>
        private static void DrawChemotacticBeacon(IconCanvas canvas, Color accent, Color highlight)
        {
            const float sporeX = 23f, sporeY = 77f;
            const float targetX = 66f, targetY = 34f;

            canvas.DashedLine(sporeX, sporeY, targetX, targetY, 2.5f, 4f, 3f, Color.Lerp(accent, highlight, 0.5f));
            IconGlyphs.Spore(canvas, sporeX, sporeY, 7f, accent, highlight);

            for (int i = 0; i < 3; i++)
            {
                float t = 0.32f + 0.17f * i;
                IconGlyphs.LivingCell(canvas, sporeX + (targetX - sporeX) * t, sporeY + (targetY - sporeY) * t, 10f, accent);
            }

            canvas.Ring(targetX, targetY, 9f, 3f, accent);
            canvas.FillCircle(targetX, targetY, 2.6f, highlight);

            const float spiralStartRadius = 12f, spiralEndRadius = 21f;
            const float spiralStart = 150f, spiralEnd = 440f;
            canvas.Spiral(targetX, targetY, spiralStartRadius, spiralEndRadius, spiralStart, spiralEnd, 3f, highlight);
            float endAngle = spiralEnd * (float)Math.PI / 180f;
            canvas.ArrowHead(
                targetX + (float)Math.Cos(endAngle) * spiralEndRadius,
                targetY + (float)Math.Sin(endAngle) * spiralEndRadius,
                -(float)Math.Sin(endAngle), (float)Math.Cos(endAngle), 8f, highlight);
        }

        /// <summary>A rival's resistant cell copied into one of your own.</summary>
        private static void DrawMimeticResilience(IconCanvas canvas, Color accent, Color highlight)
        {
            IconGlyphs.EnemyResistantCell(canvas, 70f, 58f, 24f, IconGlyphs.Palette.Enemy);
            IconGlyphs.ResistantCell(canvas, 30f, 58f, 24f, accent, IconGlyphs.Palette.Mark);

            const float x0 = 70f, y0 = 41f, cx = 50f, cy = 12f, x1 = 32f, y1 = 41f;
            canvas.Bezier(x0, y0, cx, cy, x1, y1, 3.5f, highlight);
            float tx = x1 - cx, ty = y1 - cy;
            float length = (float)Math.Sqrt(tx * tx + ty * ty);
            canvas.ArrowHead(x1, y1 + 1f, tx / length, ty / length, 9f, highlight);
        }

        /// <summary>Toxin crosshair over the biggest rival colony; the small one is left alone.</summary>
        private static void DrawCompetitiveAntagonism(IconCanvas canvas, Color accent, Color highlight, Color background)
        {
            Color mass = Color.Lerp(IconGlyphs.Palette.Enemy, background, 0.45f);
            const float cx = 63f, cy = 47f;
            foreach (var (dx, dy) in new[] { (-9f, -9f), (9f, -9f), (-9f, 9f), (9f, 9f) })
            {
                IconGlyphs.LivingCell(canvas, cx + dx, cy + dy, 15f, mass);
            }

            canvas.Ring(cx, cy, 24f, 3f, accent);
            foreach (var (dx, dy) in new[] { (0f, -1f), (1f, 0f), (0f, 1f), (-1f, 0f) })
            {
                canvas.Line(cx + dx * 19f, cy + dy * 19f, cx + dx * 29f, cy + dy * 29f, 3f, highlight);
            }

            IconGlyphs.Toxin(canvas, cx, cy, 8f, IconGlyphs.Palette.Toxin);
            IconGlyphs.EnemyCell(canvas, 22f, 77f, 12f, mass);
        }

        /// <summary>A block of your cells with several of them freshly plated with resistance.</summary>
        private static void DrawChitinFortification(IconCanvas canvas, Color accent)
        {
            Color mark = UIStyleTokens.Surface.Canvas;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    float x = 30f + col * 20f;
                    float y = 32f + row * 20f;
                    bool resistant = (row == 0 && col == 0) || (row == 1 && col == 2) || (row == 2 && col == 1);
                    if (resistant)
                    {
                        IconGlyphs.ResistantCell(canvas, x, y, 15f, accent, mark);
                    }
                    else
                    {
                        IconGlyphs.LivingCell(canvas, x, y, 15f, accent);
                    }
                }
            }
        }

        private static void DrawFallback(IconCanvas canvas, Color accent, Color highlight)
        {
            canvas.Ring(50f, 50f, 22f, 5f, accent);
            canvas.FillCircle(50f, 50f, 8f, highlight);
        }
    }
}
