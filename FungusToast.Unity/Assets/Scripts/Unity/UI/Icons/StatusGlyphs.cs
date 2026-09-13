using UnityEngine;

namespace FungusToast.Unity.UI.Icons
{
    /// <summary>
    /// Small status marks drawn with <see cref="IconCanvas"/> on a transparent background, for
    /// overlays that sit on top of cards (the mutation-tree lock). Drawn rather than loaded so
    /// the colour can come from <see cref="UIStyleTokens"/>: the old lock PNG was a near-black
    /// glyph that vanished on the dark locked-card fill, and an Image tint cannot lighten black.
    /// </summary>
    public static class StatusGlyphs
    {
        private const int Size = 64;

        private static Sprite lockSprite;

        /// <summary>Padlock in <c>Text.Muted</c>: legible on the dark locked fill without outshining the card name; cached.</summary>
        public static Sprite Lock
        {
            get
            {
                if (lockSprite == null)
                {
                    var canvas = new IconCanvas(Size);
                    DrawLock(canvas, UIStyleTokens.Text.Muted, UIStyleTokens.Surface.Canvas);
                    lockSprite = canvas.ToSprite("StatusGlyph_Lock");
                }

                return lockSprite;
            }
        }

        /// <summary>A padlock: shackle over a rounded body with a keyhole cut in the body colour.</summary>
        public static void DrawLock(IconCanvas canvas, Color color, Color keyhole)
        {
            canvas.Fill(new Color(0f, 0f, 0f, 0f));
            canvas.Arc(50f, 42f, 19f, 180f, 360f, 9f, color);
            canvas.Line(31f, 42f, 31f, 54f, 9f, color);
            canvas.Line(69f, 42f, 69f, 54f, 9f, color);
            canvas.FillRect(20f, 46f, 60f, 46f, color, 8f);
            canvas.FillCircle(50f, 64f, 6f, keyhole);
            canvas.FillRect(46.5f, 64f, 7f, 14f, keyhole, 2f);
        }
    }
}
