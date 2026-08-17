using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>Static, low-contrast substrate grain and hyphae behind the mutation lanes.</summary>
    public sealed class MycelialBackdropGraphic : MaskableGraphic
    {
        private const int SporeCount = 72;
        private const int HyphaCount = 9;

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect area = rectTransform.rect;
            uint state = 0x6D796365;

            Color sporeColor = UIStyleTokens.WithAlpha(UIStyleTokens.Accent.Spore, 0.055f);
            for (int index = 0; index < SporeCount; index++)
            {
                float x = Mathf.Lerp(area.xMin, area.xMax, Next01(ref state));
                float y = Mathf.Lerp(area.yMin, area.yMax, Next01(ref state));
                float radius = Mathf.Lerp(0.8f, 2.2f, Next01(ref state));
                AddQuad(helper, new Vector2(x - radius, y - radius), new Vector2(x + radius, y + radius), sporeColor);
            }

            Color hyphaColor = UIStyleTokens.WithAlpha(UIStyleTokens.Category.Growth, 0.075f);
            for (int index = 0; index < HyphaCount; index++)
            {
                Vector2 start = new(
                    Mathf.Lerp(area.xMin, area.xMax, Next01(ref state)),
                    Mathf.Lerp(area.yMin, area.yMax, Next01(ref state)));
                Vector2 bend = start + new Vector2(
                    Mathf.Lerp(-90f, 90f, Next01(ref state)),
                    Mathf.Lerp(28f, 80f, Next01(ref state)));
                Vector2 end = bend + new Vector2(
                    Mathf.Lerp(-70f, 70f, Next01(ref state)),
                    Mathf.Lerp(20f, 65f, Next01(ref state)));
                AddLine(helper, start, bend, 1f, hyphaColor);
                AddLine(helper, bend, end, 1f, hyphaColor);
                AddLine(helper, bend, bend + new Vector2(Mathf.Lerp(-35f, 35f, Next01(ref state)), 24f), 0.8f, hyphaColor);
            }
        }

        private static float Next01(ref uint state)
        {
            state = (state * 1664525u) + 1013904223u;
            return (state & 0x00FFFFFFu) / 16777215f;
        }

        private static void AddQuad(VertexHelper helper, Vector2 min, Vector2 max, Color color)
        {
            int start = helper.currentVertCount;
            helper.AddVert(new Vector2(min.x, min.y), color, Vector2.zero);
            helper.AddVert(new Vector2(min.x, max.y), color, Vector2.zero);
            helper.AddVert(new Vector2(max.x, max.y), color, Vector2.zero);
            helper.AddVert(new Vector2(max.x, min.y), color, Vector2.zero);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddLine(VertexHelper helper, Vector2 start, Vector2 end, float width, Color color)
        {
            Vector2 direction = (end - start).normalized;
            Vector2 offset = new Vector2(-direction.y, direction.x) * (width * 0.5f);
            int index = helper.currentVertCount;
            helper.AddVert(start - offset, color, Vector2.zero);
            helper.AddVert(start + offset, color, Vector2.zero);
            helper.AddVert(end + offset, color, Vector2.zero);
            helper.AddVert(end - offset, color, Vector2.zero);
            helper.AddTriangle(index, index + 1, index + 2);
            helper.AddTriangle(index, index + 2, index + 3);
        }
    }
}
