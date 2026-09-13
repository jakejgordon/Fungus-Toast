using System;
using FungusToast.Core.Mycovariants;
using UnityEngine;

namespace FungusToast.Unity.UI.Icons
{
    /// <summary>
    /// Drawings for the Mycovariant icons. Each icon is a small diagram of what the mycovariant
    /// does on the board, built from <see cref="IconGlyphs"/>. Background tint follows the
    /// mycovariant's category and the accent follows its type, as the old icons did, so the
    /// colour coding players already know survives the redraw. Tiered mycovariants (I/II/III)
    /// share one drawing and differ by the pips in the top-right corner.
    ///
    /// Pure drawing (no textures) so the <c>tools/icon-preview</c> harness can render it;
    /// <c>MycovariantArtRepository</c> owns the sprite cache.
    /// </summary>
    public static class MycovariantIcons
    {
        public const float FrameThickness = 5f;

        private const float PipRadius = 2.6f;
        private const float PipSpacing = 6.5f;
        private const float PipY = 14f;
        private const float PipRightEdge = 86f;

        public static Color Background(Mycovariant mycovariant)
        {
            return mycovariant.Category switch
            {
                MycovariantCategory.Economy => Color.Lerp(UIStyleTokens.Accent.Moss, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                MycovariantCategory.Growth => Color.Lerp(UIStyleTokens.Category.Growth, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                MycovariantCategory.Resistance => Color.Lerp(UIStyleTokens.Category.CellularResilience, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                MycovariantCategory.Fungicide => Color.Lerp(UIStyleTokens.Category.Fungicide, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                MycovariantCategory.Reclamation => Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Surface.PanelPrimary, 0.42f),
                MycovariantCategory.Defense => Color.Lerp(UIStyleTokens.State.Info, UIStyleTokens.Surface.PanelPrimary, 0.5f),
                _ => UIStyleTokens.Surface.PanelPrimary
            };
        }

        public static Color Accent(Mycovariant mycovariant)
        {
            Color raw = mycovariant.Type switch
            {
                MycovariantType.Directional => UIStyleTokens.State.Info,
                MycovariantType.Economy => UIStyleTokens.State.Success,
                MycovariantType.Triggered => UIStyleTokens.State.Warning,
                MycovariantType.AreaEffect => Color.Lerp(UIStyleTokens.Category.MycelialSurges, UIStyleTokens.State.Info, 0.25f),
                MycovariantType.Active => UIStyleTokens.State.Focus,
                MycovariantType.Passive => UIStyleTokens.Text.Primary,
                _ => UIStyleTokens.Text.Primary
            };
            // Lifted toward the text colour, as the surge accents are, so strokes stay legible on
            // every category tint (Economy's green accent sat on a green background otherwise).
            return Color.Lerp(raw, UIStyleTokens.Text.Primary, 0.28f);
        }

        /// <summary>Tier parsed from the trailing roman numeral of the name; 0 when untiered.</summary>
        public static int ResolveTier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return 0;
            }

            if (name.EndsWith(" III", StringComparison.Ordinal)) return 3;
            if (name.EndsWith(" II", StringComparison.Ordinal)) return 2;
            if (name.EndsWith(" I", StringComparison.Ordinal)) return 1;
            return 0;
        }

        public static bool HasDedicatedIcon(int mycovariantId)
        {
            switch (mycovariantId)
            {
                case MycovariantIds.JettingMyceliumIId:
                case MycovariantIds.JettingMyceliumIIId:
                case MycovariantIds.JettingMyceliumIIIId:
                case MycovariantIds.PlasmidBountyId:
                case MycovariantIds.PlasmidBountyIIId:
                case MycovariantIds.PlasmidBountyIIIId:
                case MycovariantIds.AscusWagerId:
                case MycovariantIds.AscusBaitId:
                case MycovariantIds.SporophoreDecoyId:
                case MycovariantIds.SporalSnareId:
                case MycovariantIds.PerisporeCrownId:
                case MycovariantIds.NeutralizingMantleId:
                case MycovariantIds.EnduringToxaphoresId:
                case MycovariantIds.BallistosporeDischargeIId:
                case MycovariantIds.BallistosporeDischargeIIId:
                case MycovariantIds.BallistosporeDischargeIIIId:
                case MycovariantIds.CytolyticBurstId:
                case MycovariantIds.ChemotacticMycotoxinsId:
                case MycovariantIds.PerimeterProliferatorId:
                case MycovariantIds.CornerConduitIId:
                case MycovariantIds.CornerConduitIIId:
                case MycovariantIds.CornerConduitIIIId:
                case MycovariantIds.HyphalDrawId:
                case MycovariantIds.NecrophoricAdaptation:
                case MycovariantIds.ReclamationRhizomorphsId:
                case MycovariantIds.MycelialBastionIId:
                case MycovariantIds.MycelialBastionIIId:
                case MycovariantIds.MycelialBastionIIIId:
                case MycovariantIds.SurgicalInoculationId:
                case MycovariantIds.HyphalResistanceTransferId:
                case MycovariantIds.SeptalAlarmId:
                case MycovariantIds.SeptalSealId:
                case MycovariantIds.AggressotropicConduitIId:
                case MycovariantIds.AggressotropicConduitIIId:
                case MycovariantIds.AggressotropicConduitIIIId:
                    return true;
                default:
                    return false;
            }
        }

        public static void Draw(IconCanvas canvas, Mycovariant mycovariant)
        {
            Color background = Background(mycovariant);
            Color accent = Accent(mycovariant);
            Color highlight = Color.Lerp(accent, Color.white, 0.32f);

            canvas.Fill(background);
            canvas.Frame(accent, FrameThickness);

            switch (mycovariant.Id)
            {
                case MycovariantIds.JettingMyceliumIId:
                case MycovariantIds.JettingMyceliumIIId:
                case MycovariantIds.JettingMyceliumIIIId:
                    DrawJettingMycelium(canvas, accent);
                    break;
                case MycovariantIds.PlasmidBountyId:
                case MycovariantIds.PlasmidBountyIIId:
                case MycovariantIds.PlasmidBountyIIIId:
                    DrawPlasmidBounty(canvas, accent, highlight);
                    break;
                case MycovariantIds.AscusWagerId:
                    DrawAscusWager(canvas, accent, highlight);
                    break;
                case MycovariantIds.AscusBaitId:
                    DrawAscusBait(canvas, accent, highlight);
                    break;
                case MycovariantIds.SporophoreDecoyId:
                    DrawSporophoreDecoy(canvas, accent, highlight);
                    break;
                case MycovariantIds.SporalSnareId:
                    DrawSporalSnare(canvas, accent, highlight);
                    break;
                case MycovariantIds.PerisporeCrownId:
                    DrawPerisporeCrown(canvas, accent, highlight);
                    break;
                case MycovariantIds.NeutralizingMantleId:
                    DrawNeutralizingMantle(canvas, accent, highlight);
                    break;
                case MycovariantIds.EnduringToxaphoresId:
                    DrawEnduringToxaphores(canvas, accent, highlight);
                    break;
                case MycovariantIds.BallistosporeDischargeIId:
                case MycovariantIds.BallistosporeDischargeIIId:
                case MycovariantIds.BallistosporeDischargeIIIId:
                    DrawBallistosporeDischarge(canvas, accent, highlight);
                    break;
                case MycovariantIds.CytolyticBurstId:
                    DrawCytolyticBurst(canvas, accent, highlight);
                    break;
                case MycovariantIds.ChemotacticMycotoxinsId:
                    DrawChemotacticMycotoxins(canvas, accent, highlight);
                    break;
                case MycovariantIds.PerimeterProliferatorId:
                    DrawPerimeterProliferator(canvas, accent, highlight);
                    break;
                case MycovariantIds.CornerConduitIId:
                case MycovariantIds.CornerConduitIIId:
                case MycovariantIds.CornerConduitIIIId:
                    DrawCornerConduit(canvas, accent, highlight);
                    break;
                case MycovariantIds.HyphalDrawId:
                    DrawHyphalDraw(canvas, accent, highlight);
                    break;
                case MycovariantIds.NecrophoricAdaptation:
                    DrawNecrophoricAdaptation(canvas, accent, highlight);
                    break;
                case MycovariantIds.ReclamationRhizomorphsId:
                    DrawReclamationRhizomorphs(canvas, accent, highlight);
                    break;
                case MycovariantIds.MycelialBastionIId:
                case MycovariantIds.MycelialBastionIIId:
                case MycovariantIds.MycelialBastionIIIId:
                    DrawMycelialBastion(canvas, accent, background);
                    break;
                case MycovariantIds.SurgicalInoculationId:
                    DrawSurgicalInoculation(canvas, accent, highlight, background);
                    break;
                case MycovariantIds.HyphalResistanceTransferId:
                    DrawHyphalResistanceTransfer(canvas, accent, background);
                    break;
                case MycovariantIds.SeptalAlarmId:
                    DrawSeptalAlarm(canvas, accent, highlight, background);
                    break;
                case MycovariantIds.SeptalSealId:
                    DrawSeptalSeal(canvas, accent, highlight, background);
                    break;
                case MycovariantIds.AggressotropicConduitIId:
                case MycovariantIds.AggressotropicConduitIIId:
                case MycovariantIds.AggressotropicConduitIIIId:
                    DrawAggressotropicConduit(canvas, accent, highlight);
                    break;
                default:
                    DrawFallback(canvas, accent, highlight);
                    break;
            }

            DrawTierPips(canvas, ResolveTier(mycovariant.Name), background, accent);
        }

        /// <summary>Mycovariant family tell: tier pips in the top-right corner (untiered icons have none).</summary>
        private static void DrawTierPips(IconCanvas canvas, int tier, Color background, Color accent)
        {
            if (tier <= 0)
            {
                return;
            }

            float width = PipSpacing * (tier - 1) + PipRadius * 2f + 6f;
            canvas.FillRect(PipRightEdge + PipRadius + 3f - width, PipY - PipRadius - 3f, width, PipRadius * 2f + 6f, background, 3f);
            float centerX = PipRightEdge - PipSpacing * (tier - 1) * 0.5f;
            IconGlyphs.Pips(canvas, centerX, PipY, tier, PipRadius, PipSpacing, accent);
        }

        /// <summary>Mark colour that stays legible on a cell of the given fill.</summary>
        private static Color MarkOn(Color fill, Color background)
        {
            float luminance = 0.299f * fill.r + 0.587f * fill.g + 0.114f * fill.b;
            return luminance > 0.6f ? Color.Lerp(background, UIStyleTokens.Surface.Canvas, 0.6f) : IconGlyphs.Palette.Mark;
        }

        /// <summary>Source cell, a straight jet of cells, then the widening toxin fan past its tip.</summary>
        private static void DrawJettingMycelium(IconCanvas canvas, Color accent)
        {
            const float y = 54f;
            IconGlyphs.LivingCell(canvas, 17f, y, 14f, accent);
            for (int i = 0; i < 3; i++)
            {
                IconGlyphs.LivingCell(canvas, 32f + 13f * i, y, 11f, accent);
            }

            Color toxin = IconGlyphs.Palette.Toxin;
            IconGlyphs.Toxin(canvas, 70f, y, 5.5f, toxin);
            IconGlyphs.Toxin(canvas, 81f, y - 11f, 5.5f, toxin);
            IconGlyphs.Toxin(canvas, 81f, y + 11f, 5.5f, toxin);
        }

        /// <summary>Two plasmid loops absorbed, with a plus for the mutation points they yield.</summary>
        private static void DrawPlasmidBounty(IconCanvas canvas, Color accent, Color highlight)
        {
            canvas.Ring(42f, 54f, 19f, 5f, accent);
            canvas.Ring(42f, 54f, 9f, 3f, highlight);
            canvas.Line(69f, 54f, 85f, 54f, 4.5f, highlight);
            canvas.Line(77f, 46f, 77f, 62f, 4.5f, highlight);
        }

        /// <summary>A bursting ascus sends one spore to the top rung of a five-tier ladder.</summary>
        private static void DrawAscusWager(IconCanvas canvas, Color accent, Color highlight)
        {
            Color rail = IconGlyphs.Palette.Faint;
            canvas.Line(22f, 26f, 22f, 84f, 3f, rail);
            canvas.Line(42f, 26f, 42f, 84f, 3f, rail);
            for (int i = 0; i < 5; i++)
            {
                float y = 80f - 12.5f * i;
                canvas.Line(22f, y, 42f, y, 3f, i == 4 ? highlight : rail);
            }

            canvas.FillPolygon(new[]
            {
                new Vector2(58f, 82f), new Vector2(70f, 70f), new Vector2(84f, 72f), new Vector2(86f, 84f), new Vector2(72f, 88f)
            }, accent);
            canvas.FillCircle(66f, 58f, 2.4f, highlight);
            canvas.FillCircle(56f, 46f, 2.4f, highlight);
            canvas.FillCircle(44f, 36f, 2.6f, highlight);
            canvas.FillCircle(32f, 30f, 4.2f, UIStyleTokens.State.Warning);
        }

        /// <summary>A spore skewered on a fish hook: a lure, not a prize.</summary>
        private static void DrawAscusBait(IconCanvas canvas, Color accent, Color highlight)
        {
            canvas.Line(62f, 14f, 62f, 56f, 4f, accent);
            canvas.Arc(48f, 56f, 14f, 0f, 180f, 4f, accent);
            canvas.Line(34f, 56f, 34f, 46f, 4f, accent);
            canvas.ArrowHead(34f, 38f, 0f, -1f, 9f, accent);
            IconGlyphs.Spore(canvas, 34f, 36f, 7.5f, highlight, accent);
        }

        /// <summary>A hollow mushroom silhouette; the shield it promises is crossed out.</summary>
        private static void DrawSporophoreDecoy(IconCanvas canvas, Color accent, Color highlight)
        {
            canvas.DashedArc(46f, 50f, 24f, 180f, 360f, 3f, 5f, 3.5f, accent);
            canvas.DashedLine(22f, 50f, 70f, 50f, 3f, 5f, 3.5f, accent);
            canvas.DashedLine(38f, 50f, 38f, 82f, 3f, 5f, 3.5f, accent);
            canvas.DashedLine(54f, 50f, 54f, 82f, 3f, 5f, 3.5f, accent);
            canvas.DashedLine(38f, 82f, 54f, 82f, 3f, 5f, 3.5f, accent);
            IconGlyphs.Shield(canvas, 78f, 74f, 8f, highlight);
            IconGlyphs.KillMark(canvas, 78f, 74f, 8f, 3f, IconGlyphs.Palette.Enemy);
        }

        /// <summary>Cells along the line between two starting spores caught in snare loops.</summary>
        private static void DrawSporalSnare(IconCanvas canvas, Color accent, Color highlight)
        {
            const float x0 = 20f, y0 = 80f, x1 = 80f, y1 = 20f;
            canvas.DashedLine(x0, y0, x1, y1, 2.5f, 4f, 3f, IconGlyphs.Palette.Faint);
            IconGlyphs.Spore(canvas, x0, y0, 7f, accent, highlight);
            IconGlyphs.EnemySpore(canvas, x1, y1, 7f, IconGlyphs.Palette.Enemy);
            foreach (float t in new[] { 0.32f, 0.5f, 0.68f })
            {
                float x = x0 + (x1 - x0) * t;
                float y = y0 + (y1 - y0) * t;
                IconGlyphs.LivingCell(canvas, x, y, 8f, accent);
                canvas.Ring(x, y, 7.5f, 2.5f, highlight);
            }
        }

        /// <summary>A ring of toxins erupting around a rival's starting spore.</summary>
        private static void DrawPerisporeCrown(IconCanvas canvas, Color accent, Color highlight)
        {
            IconGlyphs.EnemySpore(canvas, 50f, 52f, 9f, IconGlyphs.Palette.Enemy);
            IconGlyphs.Radius(canvas, 50f, 52f, 20f, 2f, highlight);
            for (int i = 0; i < 6; i++)
            {
                float angle = (-90f + 60f * i) * (float)Math.PI / 180f;
                IconGlyphs.Toxin(canvas, 50f + (float)Math.Cos(angle) * 30f, 52f + (float)Math.Sin(angle) * 30f, 5.5f, IconGlyphs.Palette.Toxin);
            }
        }

        /// <summary>A haloed cell with the toxin beside it struck out.</summary>
        private static void DrawNeutralizingMantle(IconCanvas canvas, Color accent, Color highlight)
        {
            IconGlyphs.LivingCell(canvas, 38f, 52f, 20f, accent);
            canvas.Ring(38f, 52f, 18f, 3f, highlight);
            IconGlyphs.Toxin(canvas, 72f, 52f, 9f, IconGlyphs.Palette.Toxin);
            IconGlyphs.KillMark(canvas, 72f, 52f, 11f, 3.5f, highlight);
        }

        /// <summary>A toxin wrapped in a long clock sweep: it lingers.</summary>
        private static void DrawEnduringToxaphores(IconCanvas canvas, Color accent, Color highlight)
        {
            IconGlyphs.Toxin(canvas, 50f, 54f, 12f, IconGlyphs.Palette.Toxin);
            const float radius = 25f;
            const float start = 250f, end = 520f;
            canvas.Arc(50f, 54f, radius, start, end, 3.5f, highlight);
            float endAngle = end * (float)Math.PI / 180f;
            canvas.ArrowHead(50f + (float)Math.Cos(endAngle) * radius, 54f + (float)Math.Sin(endAngle) * radius, -(float)Math.Sin(endAngle), (float)Math.Cos(endAngle), 8f, highlight);
        }

        /// <summary>A spore lobbing toxins along arcs onto distant empty tiles.</summary>
        private static void DrawBallistosporeDischarge(IconCanvas canvas, Color accent, Color highlight)
        {
            const float sx = 22f, sy = 78f;
            IconGlyphs.Spore(canvas, sx, sy, 7f, accent, highlight);
            foreach (var (tx, ty) in new[] { (50f, 26f), (78f, 44f), (68f, 76f) })
            {
                IconGlyphs.FlightArc(canvas, sx + 6f, sy - 6f, tx, ty, 8f, 2.5f, highlight, arrowHead: false);
                IconGlyphs.Toxin(canvas, tx, ty, 6f, IconGlyphs.Palette.Toxin);
            }
        }

        /// <summary>One toxin bursting across a radius, poisoning the rival cells inside it.</summary>
        private static void DrawCytolyticBurst(IconCanvas canvas, Color accent, Color highlight)
        {
            const float cx = 50f, cy = 52f;
            IconGlyphs.Radius(canvas, cx, cy, 28f, 2.5f, highlight);
            foreach (var (x, y) in new[] { (30f, 38f), (68f, 36f), (66f, 70f) })
            {
                IconGlyphs.EnemyCell(canvas, x, y, 12f, IconGlyphs.Palette.Enemy);
                canvas.FillCircle(x, y, 2.8f, IconGlyphs.Palette.Toxin);
            }

            IconGlyphs.Toxin(canvas, cx, cy, 10f, IconGlyphs.Palette.Toxin);
        }

        /// <summary>An isolated toxin drifting toward a rival cell.</summary>
        private static void DrawChemotacticMycotoxins(IconCanvas canvas, Color accent, Color highlight)
        {
            IconGlyphs.Toxin(canvas, 26f, 62f, 9.5f, IconGlyphs.Palette.Toxin);
            canvas.DashedLine(38f, 58f, 58f, 50f, 3f, 5f, 3.5f, highlight);
            canvas.ArrowHead(64f, 48f, 0.93f, -0.37f, 9f, highlight);
            IconGlyphs.EnemyCell(canvas, 76f, 44f, 16f, IconGlyphs.Palette.Enemy);
        }

        /// <summary>Growth arrows accelerating into cells that sit against the crust.</summary>
        private static void DrawPerimeterProliferator(IconCanvas canvas, Color accent, Color highlight)
        {
            IconGlyphs.Crust(canvas, CrustEdge.Right, 10f, 9f, Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Text.Primary, 0.2f));
            foreach (float y in new[] { 30f, 52f, 74f })
            {
                IconGlyphs.LivingCell(canvas, 68f, y, 12f, accent);
                canvas.Arrow(30f, y, 58f, y, 3f, 8f, highlight);
            }
        }

        /// <summary>Cells appearing in a board corner, drawn there from the colony.</summary>
        private static void DrawCornerConduit(IconCanvas canvas, Color accent, Color highlight)
        {
            Color crust = Color.Lerp(UIStyleTokens.Accent.Putrefaction, UIStyleTokens.Text.Primary, 0.2f);
            canvas.FillRect(10f, 10f, 56f, 8f, crust);
            canvas.FillRect(10f, 10f, 8f, 56f, crust);
            IconGlyphs.LivingCell(canvas, 26f, 26f, 12f, accent);
            IconGlyphs.LivingCell(canvas, 40f, 26f, 12f, accent);
            IconGlyphs.LivingCell(canvas, 26f, 40f, 12f, accent);
            IconGlyphs.LivingCell(canvas, 74f, 76f, 14f, accent);
            canvas.DashedLine(66f, 68f, 50f, 52f, 3f, 5f, 3.5f, highlight);
            canvas.ArrowHead(45f, 47f, -0.707f, -0.707f, 9f, highlight);
        }

        /// <summary>Cells lifted off the path to the enemy and set back down from their side.</summary>
        private static void DrawHyphalDraw(IconCanvas canvas, Color accent, Color highlight)
        {
            const float x0 = 20f, y0 = 80f, x1 = 80f, y1 = 20f;
            canvas.DashedLine(x0, y0, x1, y1, 2.5f, 4f, 3f, IconGlyphs.Palette.Faint);
            IconGlyphs.Spore(canvas, x0, y0, 7f, accent, highlight);
            IconGlyphs.EnemySpore(canvas, x1, y1, 7f, IconGlyphs.Palette.Enemy);
            foreach (float t in new[] { 0.26f, 0.4f })
            {
                IconGlyphs.DissolvingCell(canvas, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, 10f, accent);
            }

            foreach (float t in new[] { 0.6f, 0.74f })
            {
                IconGlyphs.LivingCell(canvas, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, 10f, accent);
            }

            IconGlyphs.FlightArc(canvas, 38f, 56f, 62f, 40f, 12f, 2.5f, highlight);
        }

        /// <summary>A cell dying while the dead tile beside it is reclaimed.</summary>
        private static void DrawNecrophoricAdaptation(IconCanvas canvas, Color accent, Color highlight)
        {
            IconGlyphs.LivingCell(canvas, 30f, 52f, 18f, accent);
            IconGlyphs.KillMark(canvas, 30f, 52f, 6f, 3f, IconGlyphs.Palette.Enemy);
            canvas.Arrow(42f, 52f, 57f, 52f, 3f, 8f, highlight);
            IconGlyphs.DeadCell(canvas, 70f, 52f, 20f, IconGlyphs.Palette.Dead);
            IconGlyphs.LivingCell(canvas, 70f, 52f, 11f, accent);
        }

        /// <summary>A failed reclaim struck out, and the second root-like attempt landing.</summary>
        private static void DrawReclamationRhizomorphs(IconCanvas canvas, Color accent, Color highlight)
        {
            IconGlyphs.LivingCell(canvas, 26f, 52f, 16f, accent);
            IconGlyphs.DeadCell(canvas, 72f, 52f, 18f, IconGlyphs.Palette.Dead);
            canvas.Arrow(37f, 40f, 60f, 40f, 2.5f, 7f, IconGlyphs.Palette.Faint);
            IconGlyphs.KillMark(canvas, 48f, 40f, 4f, 2.5f, IconGlyphs.Palette.Enemy);
            canvas.Bezier(37f, 62f, 48f, 72f, 56f, 62f, 3f, highlight);
            canvas.ArrowHead(62f, 60f, 0.94f, -0.34f, 8f, highlight);
        }

        /// <summary>A cluster of your cells with the chosen ones shielded.</summary>
        private static void DrawMycelialBastion(IconCanvas canvas, Color accent, Color background)
        {
            Color mark = MarkOn(accent, background);
            IconGlyphs.LivingCell(canvas, 36f, 40f, 14f, accent);
            IconGlyphs.ResistantCell(canvas, 52f, 40f, 14f, accent, mark);
            IconGlyphs.ResistantCell(canvas, 44f, 56f, 14f, accent, mark);
            IconGlyphs.LivingCell(canvas, 60f, 56f, 14f, accent);
            IconGlyphs.LivingCell(canvas, 52f, 72f, 14f, accent);
        }

        /// <summary>One resistant cell placed anywhere on the board along a flight arc.</summary>
        private static void DrawSurgicalInoculation(IconCanvas canvas, Color accent, Color highlight, Color background)
        {
            IconGlyphs.LivingCell(canvas, 24f, 74f, 16f, accent);
            IconGlyphs.EmptyTile(canvas, 74f, 52f, 12f, IconGlyphs.Palette.Faint);
            IconGlyphs.EmptyTile(canvas, 56f, 30f, 12f, IconGlyphs.Palette.Faint);
            IconGlyphs.FlightArc(canvas, 32f, 66f, 70f, 34f, 12f, 2.5f, highlight);
            IconGlyphs.ResistantCell(canvas, 74f, 30f, 14f, accent, MarkOn(accent, background));
        }

        /// <summary>Resistance spreading from a shielded centre to its neighbours, diagonals included.</summary>
        private static void DrawHyphalResistanceTransfer(IconCanvas canvas, Color accent, Color background)
        {
            Color mark = MarkOn(accent, background);
            for (int row = -1; row <= 1; row++)
            {
                for (int col = -1; col <= 1; col++)
                {
                    float x = 50f + col * 20f;
                    float y = 52f + row * 20f;
                    bool shielded = (row == 0 && col == 0) || (row == -1 && col == -1) || (row == 0 && col == 1) || (row == 1 && col == 0);
                    if (shielded)
                    {
                        IconGlyphs.ResistantCell(canvas, x, y, row == 0 && col == 0 ? 16f : 13f, accent, mark);
                    }
                    else
                    {
                        IconGlyphs.LivingCell(canvas, x, y, 13f, accent);
                    }
                }
            }
        }

        /// <summary>A dying cell raising the alarm; its orthogonal neighbours shield up.</summary>
        private static void DrawSeptalAlarm(IconCanvas canvas, Color accent, Color highlight, Color background)
        {
            Color mark = MarkOn(accent, background);
            IconGlyphs.LivingCell(canvas, 50f, 52f, 16f, accent);
            IconGlyphs.KillMark(canvas, 50f, 52f, 5.5f, 3f, IconGlyphs.Palette.Enemy);
            foreach (var (dx, dy) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
            {
                canvas.Line(50f + dx * 11f, 52f + dy * 11f, 50f + dx * 17f, 52f + dy * 17f, 2.5f, highlight);
            }

            IconGlyphs.ResistantCell(canvas, 28f, 52f, 13f, accent, mark);
            IconGlyphs.ResistantCell(canvas, 50f, 30f, 13f, accent, mark);
            IconGlyphs.LivingCell(canvas, 72f, 52f, 13f, accent);
            IconGlyphs.LivingCell(canvas, 50f, 74f, 13f, accent);
        }

        /// <summary>A scattered colony with a random share of cells stamped resistant.</summary>
        private static void DrawSeptalSeal(IconCanvas canvas, Color accent, Color highlight, Color background)
        {
            Color mark = MarkOn(accent, background);
            foreach (var (x, y, isSealed) in new[] { (28f, 34f, false), (48f, 30f, true), (68f, 38f, false), (32f, 58f, false), (58f, 58f, true), (46f, 78f, false), (72f, 74f, false) })
            {
                if (isSealed)
                {
                    IconGlyphs.ResistantCell(canvas, x, y, 12f, accent, mark);
                    canvas.Ring(x, y, 9.5f, 2.2f, highlight);
                }
                else
                {
                    IconGlyphs.LivingCell(canvas, x, y, 12f, accent);
                }
            }
        }

        /// <summary>A conduit of cells reaching from your spore straight at a rival's.</summary>
        private static void DrawAggressotropicConduit(IconCanvas canvas, Color accent, Color highlight)
        {
            const float x0 = 20f, y0 = 80f, x1 = 74f, y1 = 30f;
            IconGlyphs.Spore(canvas, x0, y0, 7f, accent, highlight);
            IconGlyphs.EnemySpore(canvas, x1, y1, 7f, IconGlyphs.Palette.Enemy);
            foreach (float t in new[] { 0.25f, 0.4f, 0.55f })
            {
                IconGlyphs.LivingCell(canvas, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, 10f, accent);
            }

            float dx = x1 - x0, dy = y1 - y0;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            canvas.ArrowHead(x0 + dx * 0.76f, y0 + dy * 0.76f, dx / length, dy / length, 9f, highlight);
        }

        private static void DrawFallback(IconCanvas canvas, Color accent, Color highlight)
        {
            canvas.Ring(50f, 50f, 22f, 5f, accent);
            canvas.FillCircle(50f, 50f, 8f, highlight);
        }
    }
}
