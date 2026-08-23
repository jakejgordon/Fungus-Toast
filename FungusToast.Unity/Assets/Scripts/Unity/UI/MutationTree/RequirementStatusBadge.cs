#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Small non-text requirement state marker used by the mutation inspector.
    /// It is procedural so the runtime-built inspector has no prefab or TMP glyph dependency.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    internal sealed class RequirementStatusBadge : MonoBehaviour
    {
        private static Sprite? circleSprite;
        private static Sprite? checkSprite;
        private static Sprite? lockSprite;

        private Image background = null!;
        private Image symbol = null!;

        private void Awake()
        {
            background = CreateImage("Circle", transform);
            background.sprite = GetCircleSprite();
            background.rectTransform.anchorMin = Vector2.zero;
            background.rectTransform.anchorMax = Vector2.one;
            background.rectTransform.offsetMin = Vector2.zero;
            background.rectTransform.offsetMax = Vector2.zero;

            symbol = CreateImage("Symbol", transform);
            symbol.rectTransform.anchorMin = Vector2.zero;
            symbol.rectTransform.anchorMax = Vector2.one;
            symbol.rectTransform.offsetMin = new Vector2(4f, 4f);
            symbol.rectTransform.offsetMax = new Vector2(-4f, -4f);
        }

        public void SetStatus(RequirementStatus status)
        {
            bool isStatusVisible = status != RequirementStatus.None;
            gameObject.SetActive(isStatusVisible);
            if (!isStatusVisible)
            {
                return;
            }

            bool isMet = status == RequirementStatus.Met;
            background.color = isMet ? UIStyleTokens.State.Success : UIStyleTokens.State.Warning;
            symbol.sprite = isMet ? GetCheckSprite() : GetLockSprite();
            symbol.color = UIStyleTokens.Text.OnAccent;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private static Sprite GetCircleSprite() => circleSprite ??= CreateSprite("RequirementStatusCircle", DrawCircle);
        private static Sprite GetCheckSprite() => checkSprite ??= CreateSprite("RequirementStatusCheck", DrawCheck);
        private static Sprite GetLockSprite() => lockSprite ??= CreateSprite("RequirementStatusLock", DrawLock);

        private static Sprite CreateSprite(string name, System.Action<Color32[]> draw)
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            draw(pixels);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static void DrawCircle(Color32[] pixels)
        {
            const int size = 32;
            const float radius = 15f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f));
                    if (distance <= radius)
                    {
                        pixels[(y * size) + x] = new Color32(255, 255, 255, 255);
                    }
                }
            }
        }

        private static void DrawCheck(Color32[] pixels)
        {
            DrawLine(pixels, new Vector2(7f, 16f), new Vector2(13f, 10f), 4f);
            DrawLine(pixels, new Vector2(13f, 10f), new Vector2(25f, 23f), 4f);
        }

        private static void DrawLock(Color32[] pixels)
        {
            FillRect(pixels, 7, 7, 18, 14);
            DrawLine(pixels, new Vector2(10f, 20f), new Vector2(10f, 24f), 3f);
            DrawLine(pixels, new Vector2(10f, 24f), new Vector2(22f, 24f), 3f);
            DrawLine(pixels, new Vector2(22f, 24f), new Vector2(22f, 20f), 3f);
        }

        private static void FillRect(Color32[] pixels, int left, int bottom, int width, int height)
        {
            for (int y = bottom; y < bottom + height; y++)
            {
                for (int x = left; x < left + width; x++)
                {
                    pixels[(y * 32) + x] = new Color32(255, 255, 255, 255);
                }
            }
        }

        private static void DrawLine(Color32[] pixels, Vector2 start, Vector2 end, float thickness)
        {
            const int size = 32;
            float squaredThickness = thickness * thickness;
            Vector2 direction = end - start;
            float squaredLength = direction.sqrMagnitude;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    float t = squaredLength > 0f ? Mathf.Clamp01(Vector2.Dot(point - start, direction) / squaredLength) : 0f;
                    if ((point - (start + (direction * t))).sqrMagnitude <= squaredThickness)
                    {
                        pixels[(y * size) + x] = new Color32(255, 255, 255, 255);
                    }
                }
            }
        }
    }
}
