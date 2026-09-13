using System;
using System.Collections.Generic;
using UnityEngine;

namespace FungusToast.Unity.UI.Icons
{
    /// <summary>
    /// Drawings for the Adaptation icons, keyed by <c>AdaptationDefinition.IconId</c>. Each icon
    /// is a small diagram of what the adaptation does, built from <see cref="IconGlyphs"/>.
    /// Adaptations keep their per-adaptation background and accent colours from the original
    /// icon set, and are the one family drawn with rounded frame corners.
    ///
    /// Pure drawing (no textures) so the <c>tools/icon-preview</c> harness can render it;
    /// <c>AdaptationArtRepository</c> owns the sprite cache.
    /// </summary>
    public static class AdaptationIcons
    {
        public const float FrameThickness = 5f;
        public const float FrameCornerRadius = 14f;

        private static readonly Dictionary<string, Action<IconCanvas, Palette>> Drawers = new()
        {
            ["conidial_relay"] = DrawConidialRelay,
            ["hyphal_economy"] = DrawHyphalEconomy,
            ["mycotoxic_halo"] = DrawMycotoxicHalo,
            ["mycotoxic_lash"] = DrawMycotoxicLash,
            ["retrograde_bloom"] = DrawRetrogradeBloom,
            ["aegis_hyphae"] = DrawAegisHyphae,
            ["saprophage_ring"] = DrawSaprophageRing,
            ["marginal_clamp"] = DrawMarginalClamp,
            ["apical_yield"] = DrawApicalYield,
            ["crustal_callus"] = DrawCrustalCallus,
            ["distal_spore"] = DrawDistalSpore,
            ["ascus_primacy"] = DrawAscusPrimacy,
            ["spore_salvo"] = DrawSporeSalvo,
            ["hyphal_bridge"] = DrawHyphalBridge,
            ["vesicle_burst"] = DrawVesicleBurst,
            ["rhizomorphic_hunger"] = DrawRhizomorphicHunger,
            ["mycelial_crescendo"] = DrawMycelialCrescendo,
            ["ossified_advance"] = DrawOssifiedAdvance,
            ["conidia_ascent"] = DrawConidiaAscent,
            ["hyphal_priming"] = DrawHyphalPriming,
            ["tropic_lysis"] = DrawTropicLysis,
            ["prime_pulse"] = DrawPrimePulse,
            ["hyphal_echo"] = DrawHyphalEcho,
            ["oblique_filament"] = DrawObliqueFilament,
            ["thanatrophic_rebound"] = DrawThanatrophicRebound,
            ["toxin_primacy"] = DrawToxinPrimacy,
            ["centripetal_germination"] = DrawCentripetalGermination,
            ["signal_economy"] = DrawSignalEconomy,
            ["liminal_sporemeal"] = DrawLiminalSporemeal,
            ["putrefactive_resilience"] = DrawPutrefactiveResilience,
            ["compound_reserve"] = DrawCompoundReserve
        };

        /// <summary>The colours handed to every drawer.</summary>
        public readonly struct Palette
        {
            public Palette(Color background, Color accent)
            {
                Background = background;
                Accent = accent;
                Highlight = Color.Lerp(accent, Color.white, 0.32f);
                Mark = IconGlyphs.MarkFor(accent);
            }

            public Color Background { get; }
            public Color Accent { get; }
            public Color Highlight { get; }
            public Color Mark { get; }
        }

        public static IEnumerable<string> KnownIconIds => Drawers.Keys;

        public static bool HasDedicatedIcon(string iconId) => iconId != null && Drawers.ContainsKey(iconId);

        public static Color Background(string iconId) => Color.Lerp(ResolveBackground(iconId), UIStyleTokens.Text.Secondary, 0.24f);

        public static Color Accent(string iconId) => Color.Lerp(ResolveAccent(iconId), UIStyleTokens.Text.Primary, 0.28f);

        public static void Draw(IconCanvas canvas, string iconId)
        {
            var palette = new Palette(Background(iconId), Accent(iconId));
            canvas.Fill(palette.Background);
            canvas.Frame(palette.Accent, FrameThickness, FrameCornerRadius);

            if (iconId != null && Drawers.TryGetValue(iconId, out var drawer))
            {
                drawer(canvas, palette);
            }
            else
            {
                DrawFallback(canvas, palette);
            }
        }

        // ------------------------------------------------------------------ drawings

        /// <summary>The starting spore takes flight to a fresh tile.</summary>
        private static void DrawConidialRelay(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Spore(canvas, 28f, 70f, 8f, p.Accent, p.Highlight);
            IconGlyphs.FlightArc(canvas, 36f, 62f, 68f, 34f, 12f, 2.5f, p.Highlight);
            IconGlyphs.EmptyTile(canvas, 72f, 30f, 16f, IconGlyphs.Palette.Faint);
            IconGlyphs.Spore(canvas, 72f, 30f, 6f, p.Highlight, p.Accent);
        }

        /// <summary>A surge with a minus: surges cost less.</summary>
        private static void DrawHyphalEconomy(IconCanvas canvas, Palette p)
        {
            IconGlyphs.SurgeChevron(canvas, 42f, 52f, 44f, p.Accent);
            canvas.Line(66f, 52f, 84f, 52f, 5f, p.Highlight);
        }

        /// <summary>A toxin's halo reaching the four cells around it.</summary>
        private static void DrawMycotoxicHalo(IconCanvas canvas, Palette p)
        {
            canvas.Ring(50f, 52f, 19f, 3f, p.Highlight);
            foreach (var (x, y) in new[] { (24f, 52f), (76f, 52f), (50f, 26f), (50f, 78f) })
            {
                IconGlyphs.EnemyCell(canvas, x, y, 12f, IconGlyphs.Palette.Enemy);
                IconGlyphs.KillMark(canvas, x, y, 3.5f, 2.5f, IconGlyphs.Palette.Enemy);
            }

            IconGlyphs.Toxin(canvas, 50f, 52f, 10f, IconGlyphs.Palette.Toxin);
        }

        /// <summary>A fresh toxin whipping the first enemy cell beside it.</summary>
        private static void DrawMycotoxicLash(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Toxin(canvas, 30f, 60f, 11f, IconGlyphs.Palette.Toxin);
            canvas.Bezier(38f, 50f, 52f, 20f, 66f, 46f, 3.5f, p.Highlight);
            canvas.ArrowHead(69f, 52f, 0.45f, 0.89f, 9f, p.Highlight);
            IconGlyphs.EnemyCell(canvas, 72f, 64f, 16f, IconGlyphs.Palette.Enemy);
            IconGlyphs.KillMark(canvas, 72f, 64f, 5f, 3f, IconGlyphs.Palette.Enemy);
        }

        /// <summary>Tier ladder: the bottom rungs lose levels while the top rung blooms.</summary>
        private static void DrawRetrogradeBloom(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Ladder(canvas, 30f, 52f, 80f, 13f, 5, IconGlyphs.Palette.Faint);
            canvas.Line(30f, 28f, 52f, 28f, 3f, p.Highlight);
            canvas.Ring(64f, 80f, 3.5f, 2f, IconGlyphs.Palette.Enemy);
            canvas.Ring(64f, 67f, 3.5f, 2f, IconGlyphs.Palette.Enemy);
            canvas.Arrow(76f, 74f, 76f, 38f, 3f, 8f, p.Highlight);
            canvas.FillCircle(64f, 28f, 5f, UIStyleTokens.State.Warning);
            IconGlyphs.Sparkle(canvas, 64f, 28f, 10f, 2f, p.Highlight);
        }

        /// <summary>The first cells grown each round come out shielded.</summary>
        private static void DrawAegisHyphae(IconCanvas canvas, Palette p)
        {
            IconGlyphs.ResistantCell(canvas, 26f, 46f, 15f, p.Accent, p.Mark);
            IconGlyphs.ResistantCell(canvas, 42f, 46f, 15f, p.Accent, p.Mark);
            IconGlyphs.LivingCell(canvas, 58f, 46f, 15f, p.Accent);
            IconGlyphs.LivingCell(canvas, 74f, 46f, 15f, p.Accent);
            canvas.Arrow(20f, 70f, 82f, 70f, 3f, 9f, p.Highlight);
        }

        /// <summary>A dead cell beside a resistant one is eaten, leaving the tile empty.</summary>
        private static void DrawSaprophageRing(IconCanvas canvas, Palette p)
        {
            IconGlyphs.ResistantCell(canvas, 30f, 52f, 18f, p.Accent, p.Mark);
            IconGlyphs.DissolvingCell(canvas, 56f, 52f, 18f, IconGlyphs.Palette.Dead);
            canvas.FillCircle(60f, 40f, 1.8f, IconGlyphs.Palette.Dead);
            canvas.FillCircle(66f, 44f, 1.6f, IconGlyphs.Palette.Dead);
            canvas.Arrow(68f, 52f, 76f, 52f, 2.5f, 6f, p.Highlight);
            IconGlyphs.EmptyTile(canvas, 82f, 52f, 10f, IconGlyphs.Palette.Faint);
        }

        /// <summary>Border threats on the crust cleared the moment your cell grows beside them.</summary>
        private static void DrawMarginalClamp(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Crust(canvas, CrustEdge.Bottom, 10f, 9f, Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Text.Primary, 0.2f));
            IconGlyphs.LivingCell(canvas, 30f, 66f, 16f, p.Accent);
            IconGlyphs.EnemyCell(canvas, 52f, 66f, 16f, IconGlyphs.Palette.Enemy);
            IconGlyphs.KillMark(canvas, 52f, 66f, 6f, 3f, p.Highlight);
            IconGlyphs.Toxin(canvas, 74f, 66f, 8f, IconGlyphs.Palette.Toxin);
            IconGlyphs.KillMark(canvas, 74f, 66f, 8f, 3f, p.Highlight);
            canvas.Line(44f, 50f, 84f, 50f, 3f, p.Highlight);
            canvas.Line(44f, 50f, 44f, 56f, 3f, p.Highlight);
            canvas.Line(84f, 50f, 84f, 56f, 3f, p.Highlight);
        }

        /// <summary>A maxed mutation bar spilling mutation points from its top.</summary>
        private static void DrawApicalYield(IconCanvas canvas, Palette p)
        {
            canvas.FillRect(30f, 26f, 18f, 58f, p.Accent, 4f);
            canvas.FillRect(30f, 26f, 18f, 8f, p.Highlight, 4f);
            canvas.FillCircle(62f, 24f, 4f, UIStyleTokens.State.Warning);
            canvas.FillCircle(74f, 34f, 4f, UIStyleTokens.State.Warning);
            canvas.FillCircle(66f, 46f, 4f, UIStyleTokens.State.Warning);
            canvas.Line(74f, 58f, 74f, 72f, 4f, p.Highlight);
            canvas.Line(67f, 65f, 81f, 65f, 4f, p.Highlight);
        }

        /// <summary>Cells that reach the crust harden.</summary>
        private static void DrawCrustalCallus(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Crust(canvas, CrustEdge.Right, 10f, 9f, Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Text.Primary, 0.2f));
            IconGlyphs.LivingCell(canvas, 44f, 52f, 13f, p.Accent);
            foreach (float y in new[] { 30f, 52f, 74f })
            {
                IconGlyphs.ResistantCell(canvas, 68f, y, 13f, p.Accent, p.Mark);
            }
        }

        /// <summary>A resistant cell arching from the spore into the far corner.</summary>
        private static void DrawDistalSpore(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Spore(canvas, 22f, 78f, 7f, p.Accent, p.Highlight);
            IconGlyphs.FlightArc(canvas, 28f, 72f, 72f, 28f, 16f, 2.5f, p.Highlight);
            IconGlyphs.ResistantCell(canvas, 76f, 24f, 14f, p.Accent, p.Mark);
        }

        /// <summary>A raised draft card wearing a crown: you pick first.</summary>
        private static void DrawAscusPrimacy(IconCanvas canvas, Palette p)
        {
            Color back = Color.Lerp(p.Accent, p.Background, 0.55f);
            canvas.FillRect(24f, 48f, 32f, 40f, back, 4f);
            canvas.FillRect(34f, 42f, 32f, 40f, Color.Lerp(p.Accent, p.Background, 0.3f), 4f);
            canvas.FillRect(44f, 30f, 32f, 42f, p.Highlight, 4f);
            canvas.StrokeRect(44f, 30f, 32f, 42f, 2.5f, p.Accent, 4f);
            canvas.FillPolygon(new[]
            {
                new Vector2(50f, 24f), new Vector2(53f, 14f), new Vector2(58f, 20f), new Vector2(60f, 11f),
                new Vector2(62f, 20f), new Vector2(67f, 14f), new Vector2(70f, 24f)
            }, UIStyleTokens.State.Warning);
        }

        /// <summary>The starting spore firing a toxin beside each enemy spore.</summary>
        private static void DrawSporeSalvo(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Spore(canvas, 50f, 54f, 8f, p.Accent, p.Highlight);
            foreach (var (ex, ey, tx, ty) in new[] { (22f, 26f, 32f, 36f), (80f, 30f, 70f, 40f), (72f, 80f, 62f, 70f) })
            {
                canvas.DashedLine(50f, 54f, tx, ty, 2f, 3.5f, 3f, p.Highlight);
                IconGlyphs.EnemySpore(canvas, ex, ey, 6f, IconGlyphs.Palette.Enemy);
                IconGlyphs.Toxin(canvas, tx, ty, 5f, IconGlyphs.Palette.Toxin);
            }
        }

        /// <summary>Four cells dropped at even intervals between your spore and the nearest rival's.</summary>
        private static void DrawHyphalBridge(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Spore(canvas, 16f, 52f, 7f, p.Accent, p.Highlight);
            IconGlyphs.EnemySpore(canvas, 84f, 52f, 7f, IconGlyphs.Palette.Enemy);
            for (int k = 1; k <= 4; k++)
            {
                IconGlyphs.LivingCell(canvas, 16f + 68f * k / 5f, 52f, 11f, p.Accent);
            }
        }

        /// <summary>An expired toxin popping into the tiles around it.</summary>
        private static void DrawVesicleBurst(IconCanvas canvas, Palette p)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = (22.5f + 45f * i) * (float)Math.PI / 180f;
                float cos = (float)Math.Cos(angle), sin = (float)Math.Sin(angle);
                canvas.Line(50f + cos * 14f, 52f + sin * 14f, 50f + cos * 21f, 52f + sin * 21f, 2.5f, p.Highlight);
            }

            IconGlyphs.Toxin(canvas, 50f, 52f, 10f, IconGlyphs.Palette.Toxin);
            IconGlyphs.EnemyCell(canvas, 50f, 24f, 12f, IconGlyphs.Palette.Enemy);
            IconGlyphs.EmptyTile(canvas, 78f, 52f, 12f, IconGlyphs.Palette.Faint);
            IconGlyphs.DeadCell(canvas, 22f, 52f, 12f, IconGlyphs.Palette.Dead);
            foreach (var (x, y) in new[] { (50f, 24f), (78f, 52f), (22f, 52f) })
            {
                canvas.FillCircle(x, y, 2.8f, IconGlyphs.Palette.Toxin);
            }
        }

        /// <summary>Roots converging on a nutrient patch that counts as one tile bigger.</summary>
        private static void DrawRhizomorphicHunger(IconCanvas canvas, Palette p)
        {
            IconGlyphs.NutrientPatch(canvas, 58f, 52f, 16f, IconGlyphs.Palette.Nutrient);
            canvas.Arrow(18f, 30f, 40f, 44f, 3f, 8f, p.Accent);
            canvas.Arrow(18f, 74f, 40f, 60f, 3f, 8f, p.Accent);
            IconGlyphs.EmptyTile(canvas, 82f, 52f, 12f, p.Highlight);
        }

        /// <summary>A surge lit up on its own, twice.</summary>
        private static void DrawMycelialCrescendo(IconCanvas canvas, Palette p)
        {
            IconGlyphs.SurgeChevron(canvas, 50f, 52f, 46f, p.Accent);
            IconGlyphs.Sparkle(canvas, 24f, 28f, 7f, 2.5f, p.Highlight);
            IconGlyphs.Sparkle(canvas, 76f, 28f, 7f, 2.5f, p.Highlight);
        }

        /// <summary>A resistant cell pushing harder in all four orthogonal directions.</summary>
        private static void DrawOssifiedAdvance(IconCanvas canvas, Palette p)
        {
            foreach (var (dx, dy) in new[] { (0f, -1f), (1f, 0f), (0f, 1f), (-1f, 0f) })
            {
                canvas.Arrow(50f + dx * 15f, 52f + dy * 15f, 50f + dx * 34f, 52f + dy * 34f, 4f, 9f, p.Highlight);
            }

            IconGlyphs.ResistantCell(canvas, 50f, 52f, 22f, p.Accent, p.Mark);
        }

        /// <summary>A 3x3 block dissolving and a fresh 2x2 colony landing elsewhere.</summary>
        private static void DrawConidiaAscent(IconCanvas canvas, Palette p)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    IconGlyphs.DissolvingCell(canvas, 20f + col * 12f, 56f + row * 12f, 9f, p.Accent);
                }
            }

            IconGlyphs.FlightArc(canvas, 46f, 50f, 68f, 32f, 12f, 2.5f, p.Highlight);
            foreach (var (x, y) in new[] { (72f, 24f), (84f, 24f), (72f, 36f), (84f, 36f) })
            {
                IconGlyphs.LivingCell(canvas, x, y, 9.5f, p.Accent);
            }
        }

        /// <summary>Free levels stamped onto a Tier 2 rung.</summary>
        private static void DrawHyphalPriming(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Ladder(canvas, 28f, 52f, 80f, 13f, 5, IconGlyphs.Palette.Faint);
            canvas.Line(28f, 67f, 52f, 67f, 3.5f, p.Highlight);
            canvas.FillCircle(64f, 67f, 4.5f, UIStyleTokens.State.Warning);
            canvas.FillCircle(76f, 67f, 4.5f, UIStyleTokens.State.Warning);
        }

        /// <summary>Everything within reach of the spore swept clear after a draft.</summary>
        private static void DrawTropicLysis(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Radius(canvas, 50f, 54f, 28f, 2.5f, p.Highlight);
            IconGlyphs.EnemyCell(canvas, 32f, 40f, 10f, IconGlyphs.Palette.Enemy);
            IconGlyphs.KillMark(canvas, 32f, 40f, 6f, 2.5f, p.Highlight);
            IconGlyphs.Toxin(canvas, 66f, 38f, 5f, IconGlyphs.Palette.Toxin);
            IconGlyphs.KillMark(canvas, 66f, 38f, 6f, 2.5f, p.Highlight);
            IconGlyphs.DissolvingCell(canvas, 64f, 70f, 10f, IconGlyphs.Palette.Dead);
            IconGlyphs.KillMark(canvas, 64f, 70f, 6f, 2.5f, p.Highlight);
            IconGlyphs.Spore(canvas, 50f, 54f, 8f, p.Accent, p.Highlight);
        }

        /// <summary>Three pulse beats on a timeline; one of them pays out.</summary>
        private static void DrawPrimePulse(IconCanvas canvas, Palette p)
        {
            canvas.Line(16f, 62f, 84f, 62f, 3f, IconGlyphs.Palette.Faint);
            canvas.Ring(30f, 62f, 4.5f, 2.5f, p.Accent);
            canvas.Ring(70f, 62f, 4.5f, 2.5f, p.Accent);
            canvas.FillCircle(50f, 62f, 7f, UIStyleTokens.State.Warning);
            canvas.Line(50f, 52f, 50f, 40f, 3f, p.Highlight);
            IconGlyphs.Pips(canvas, 50f, 32f, 3, 3f, 8f, p.Highlight);
        }

        /// <summary>A surge with echo rings: it lingers a round longer.</summary>
        private static void DrawHyphalEcho(IconCanvas canvas, Palette p)
        {
            IconGlyphs.SurgeChevron(canvas, 42f, 52f, 40f, p.Accent);
            canvas.Arc(42f, 52f, 27f, -48f, 48f, 3f, p.Highlight);
            canvas.Arc(42f, 52f, 36f, -38f, 38f, 2.5f, Color.Lerp(p.Highlight, p.Background, 0.4f));
        }

        /// <summary>Diagonal growth up, orthogonal growth down.</summary>
        private static void DrawObliqueFilament(IconCanvas canvas, Palette p)
        {
            foreach (var (dx, dy) in new[] { (0f, -1f), (1f, 0f), (0f, 1f), (-1f, 0f) })
            {
                canvas.DashedLine(50f + dx * 12f, 52f + dy * 12f, 50f + dx * 26f, 52f + dy * 26f, 2.5f, 3f, 2.5f, IconGlyphs.Palette.Faint);
            }

            const float d = 0.7071f;
            foreach (var (dx, dy) in new[] { (-d, -d), (d, -d), (d, d), (-d, d) })
            {
                canvas.Arrow(50f + dx * 13f, 52f + dy * 13f, 50f + dx * 34f, 52f + dy * 34f, 4f, 9f, p.Highlight);
            }

            IconGlyphs.LivingCell(canvas, 50f, 52f, 16f, p.Accent);
        }

        /// <summary>A dying cell bouncing straight back as a resistant one.</summary>
        private static void DrawThanatrophicRebound(IconCanvas canvas, Palette p)
        {
            IconGlyphs.LivingCell(canvas, 28f, 56f, 16f, p.Accent);
            IconGlyphs.KillMark(canvas, 28f, 56f, 5f, 3f, IconGlyphs.Palette.Enemy);
            canvas.Bezier(36f, 46f, 50f, 14f, 66f, 46f, 3.5f, p.Highlight);
            canvas.ArrowHead(68f, 50f, 0.45f, 0.89f, 9f, p.Highlight);
            IconGlyphs.ResistantCell(canvas, 72f, 58f, 16f, p.Accent, p.Mark);
        }

        /// <summary>Mycotoxin Tracer with its levels pre-filled.</summary>
        private static void DrawToxinPrimacy(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Toxin(canvas, 38f, 54f, 13f, IconGlyphs.Palette.Toxin);
            canvas.StrokeRect(64f, 24f, 14f, 58f, 2.5f, p.Accent, 4f);
            canvas.FillRect(64f, 47f, 14f, 35f, p.Highlight, 4f);
        }

        /// <summary>The starting spore pulled in from the edge toward the board's centre.</summary>
        private static void DrawCentripetalGermination(IconCanvas canvas, Palette p)
        {
            canvas.StrokeRect(14f, 14f, 72f, 72f, 2f, IconGlyphs.Palette.Faint, 2f);
            canvas.FillCircle(50f, 50f, 2.5f, IconGlyphs.Palette.Faint);
            canvas.DashedRing(22f, 78f, 6f, 2f, 3f, 2.5f, p.Highlight);
            canvas.Arrow(28f, 72f, 36f, 64f, 3f, 7f, p.Highlight);
            IconGlyphs.Spore(canvas, 42f, 58f, 7f, p.Accent, p.Highlight);
        }

        /// <summary>Two small surges with a minus: Tier 2 surges cost less.</summary>
        private static void DrawSignalEconomy(IconCanvas canvas, Palette p)
        {
            IconGlyphs.SurgeChevron(canvas, 30f, 52f, 28f, p.Accent);
            IconGlyphs.SurgeChevron(canvas, 52f, 52f, 28f, p.Accent);
            canvas.Line(68f, 52f, 84f, 52f, 5f, p.Highlight);
        }

        /// <summary>A nutrient patch laid down against the edge nearest your spore.</summary>
        private static void DrawLiminalSporemeal(IconCanvas canvas, Palette p)
        {
            IconGlyphs.Crust(canvas, CrustEdge.Bottom, 10f, 9f, Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Text.Primary, 0.2f));
            IconGlyphs.Spore(canvas, 30f, 58f, 7f, p.Accent, p.Highlight);
            IconGlyphs.NutrientPatch(canvas, 64f, 62f, 14f, IconGlyphs.Palette.Nutrient);
        }

        /// <summary>A cell shielded against an incoming toxin.</summary>
        private static void DrawPutrefactiveResilience(IconCanvas canvas, Palette p)
        {
            IconGlyphs.LivingCell(canvas, 38f, 52f, 22f, p.Accent);
            IconGlyphs.Shield(canvas, 54f, 52f, 17f, p.Highlight);
            IconGlyphs.Toxin(canvas, 78f, 44f, 8f, IconGlyphs.Palette.Toxin);
            canvas.Line(68f, 56f, 64f, 66f, 2.5f, p.Highlight);
            canvas.Line(70f, 58f, 74f, 68f, 2.5f, p.Highlight);
        }

        /// <summary>Banked mutation points with a bonus point on top.</summary>
        private static void DrawCompoundReserve(IconCanvas canvas, Palette p)
        {
            canvas.FillRect(30f, 70f, 40f, 11f, p.Accent, 5f);
            canvas.FillRect(30f, 56f, 40f, 11f, p.Accent, 5f);
            canvas.FillRect(30f, 42f, 40f, 11f, p.Accent, 5f);
            canvas.FillCircle(50f, 28f, 6f, UIStyleTokens.State.Warning);
            canvas.Line(60f, 28f, 72f, 28f, 3f, p.Highlight);
            canvas.Line(66f, 22f, 66f, 34f, 3f, p.Highlight);
        }

        private static void DrawFallback(IconCanvas canvas, Palette p)
        {
            canvas.Ring(50f, 50f, 22f, 5f, p.Accent);
            canvas.FillCircle(50f, 50f, 8f, p.Highlight);
        }

        // ------------------------------------------------------------------ colour tables

        private static Color ResolveBackground(string adaptationId)
        {
            return adaptationId switch
            {
                "conidial_relay" => UIStyleTokens.Surface.PanelSecondary,
                "hyphal_economy" => Color.Lerp(UIStyleTokens.Accent.Moss, UIStyleTokens.Surface.PanelPrimary, 0.45f),
                "mycotoxic_halo" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Surface.PanelPrimary, 0.4f),
                "mycotoxic_lash" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Surface.PanelPrimary, 0.52f),
                "retrograde_bloom" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                "aegis_hyphae" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.58f),
                "saprophage_ring" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Surface.PanelPrimary, 0.6f),
                "marginal_clamp" => Color.Lerp(UIStyleTokens.State.Danger, UIStyleTokens.Surface.PanelPrimary, 0.55f),
                "apical_yield" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.Surface.PanelPrimary, 0.48f),
                "crustal_callus" => Color.Lerp(UIStyleTokens.Accent.Moss, UIStyleTokens.Surface.PanelPrimary, 0.5f),
                "distal_spore" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.52f),
                "ascus_primacy" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Surface.PanelPrimary, 0.5f),
                "spore_salvo" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Surface.PanelPrimary, 0.48f),
                "hyphal_bridge" => Color.Lerp(UIStyleTokens.Accent.Moss, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                "rhizomorphic_hunger" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Surface.PanelPrimary, 0.46f),
                "vesicle_burst" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Accent.Putrefaction, 0.3f),
                "mycelial_crescendo" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.Surface.PanelPrimary, 0.40f),
                "ossified_advance" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.45f),
                "conidia_ascent" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.35f),
                "oblique_filament" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.Surface.PanelPrimary, 0.55f),
                "thanatrophic_rebound" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Surface.PanelPrimary, 0.5f),
                "toxin_primacy" => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Surface.PanelPrimary, 0.38f),
                "centripetal_germination" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.4f),
                "signal_economy" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.Surface.PanelPrimary, 0.44f),
                "liminal_sporemeal" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Surface.PanelPrimary, 0.46f),
                "putrefactive_resilience" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                "compound_reserve" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.Surface.PanelPrimary, 0.34f),
                "hyphal_priming" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.Surface.PanelPrimary, 0.28f),
                "tropic_lysis" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Surface.PanelPrimary, 0.34f),
                "prime_pulse" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.3f),
                "hyphal_echo" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.Surface.PanelPrimary, 0.46f),
                _ => UIStyleTokens.Surface.PanelPrimary
            };
        }

        private static Color ResolveAccent(string adaptationId)
        {
            return adaptationId switch
            {
                "conidial_relay" => UIStyleTokens.State.Info,
                "hyphal_economy" => UIStyleTokens.State.Success,
                "mycotoxic_halo" => UIStyleTokens.State.Warning,
                "mycotoxic_lash" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.State.Danger, 0.45f),
                "retrograde_bloom" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.State.Warning, 0.2f),
                "aegis_hyphae" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Text.Primary, 0.15f),
                "saprophage_ring" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.State.Warning, 0.25f),
                "marginal_clamp" => Color.Lerp(UIStyleTokens.State.Danger, UIStyleTokens.State.Warning, 0.2f),
                "apical_yield" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.State.Warning, 0.25f),
                "crustal_callus" => Color.Lerp(UIStyleTokens.Accent.Moss, UIStyleTokens.State.Info, 0.2f),
                "distal_spore" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Text.Primary, 0.08f),
                "ascus_primacy" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Text.Primary, 0.12f),
                "spore_salvo" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.State.Danger, 0.35f),
                "hyphal_bridge" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.State.Info, 0.25f),
                "rhizomorphic_hunger" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Accent.Moss, 0.32f),
                "vesicle_burst" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Accent.Putrefaction, 0.35f),
                "mycelial_crescendo" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.State.Warning, 0.25f),
                "ossified_advance" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Text.Primary, 0.18f),
                "conidia_ascent" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.State.Info, 0.45f),
                "oblique_filament" => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.Text.Primary, 0.15f),
                "thanatrophic_rebound" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.Accent.Putrefaction, 0.32f),
                "toxin_primacy" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Text.Primary, 0.14f),
                "centripetal_germination" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.State.Warning, 0.28f),
                "signal_economy" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.State.Info, 0.22f),
                "liminal_sporemeal" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.State.Success, 0.18f),
                "putrefactive_resilience" => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.State.Info, 0.24f),
                "compound_reserve" => Color.Lerp(UIStyleTokens.State.Success, UIStyleTokens.State.Warning, 0.18f),
                "hyphal_priming" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Text.Primary, 0.12f),
                "tropic_lysis" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.Accent.Putrefaction, 0.3f),
                "prime_pulse" => Color.Lerp(UIStyleTokens.State.Warning, UIStyleTokens.State.Info, 0.35f),
                "hyphal_echo" => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Category.MycelialSurges, 0.45f),
                _ => UIStyleTokens.Text.Primary
            };
        }
    }
}
