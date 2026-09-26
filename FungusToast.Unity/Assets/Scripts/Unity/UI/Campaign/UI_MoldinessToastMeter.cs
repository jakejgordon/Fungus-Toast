#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Unity.Campaign;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.Campaign
{
    /// <summary>
    /// Moldiness progress drawn as a slice of toast whose crumb is divided into one tile per point
    /// needed for the next level. <see cref="PlayAward"/> animates an award: tiles mold over one
    /// point at a time, and a full slice crumbles into spores before a larger slice for the next
    /// level takes its place. Clicking the meter skips to the final state.
    /// </summary>
    /// <remarks>
    /// The animation runs on this component rather than its host panel so the panel's own
    /// StopAllCoroutines calls (fades, rebuilds) don't freeze it half-way.
    /// </remarks>
    public sealed class UI_MoldinessToastMeter : MonoBehaviour, IPointerClickHandler
    {
        public const float DefaultToastWidth = 200f;
        private const float LevelLabelHeight = 32f;
        private const float CounterLabelHeight = 26f;
        private const float SectionSpacing = 6f;

        // Crumb area that holds the tiles, in normalized toast coordinates (y up).
        private const float CrumbMinX = 0.16f;
        private const float CrumbMaxX = 0.84f;
        private const float CrumbMinY = 0.10f;
        private const float CrumbMaxY = 0.80f;
        private const float MaxTileSize = 34f;
        private const float TileFillRatio = 0.84f;

        private const float StartDelay = 0.65f;
        private const float PointInterval = 0.32f;
        private const float MinPointInterval = 0.14f;
        private const float MaxFillDuration = 2.8f;
        private const float TilePopDuration = 0.36f;

        private const int ToastTexWidth = 160;
        private const int ToastTexHeight = 172;
        private const int TileTexSize = 32;
        private const int MoldVariantCount = 4;

        private static readonly Color CrustRimColor = new(0.40f, 0.23f, 0.10f, 1f);
        private static readonly Color CrustColor = new(0.69f, 0.46f, 0.22f, 1f);
        private static readonly Color CrumbColor = new(0.93f, 0.80f, 0.57f, 1f);
        private static readonly Color CrumbToastedColor = new(0.85f, 0.66f, 0.40f, 1f);
        private static readonly Color CrumbPoreColor = new(0.78f, 0.60f, 0.36f, 1f);
        private static readonly Color EmptyTileColor = new(0.62f, 0.42f, 0.20f, 0.30f);
        private static readonly Color MoldDarkColor = new(0.25f, 0.36f, 0.14f, 1f);

        private static Sprite? toastSprite;
        private static Sprite? roundedTileSprite;
        private static Sprite? softDotSprite;
        private static Sprite[]? moldTileSprites;

        private sealed class TileVisual
        {
            public RectTransform Root = null!;
            public Image Mold = null!;
            public bool IsFilled;
        }

        private float toastWidth = DefaultToastWidth;
        private float toastHeight = DefaultToastWidth * ToastTexHeight / ToastTexWidth;
        private Image hitArea = null!;
        private RectTransform toastHolder = null!;
        private CanvasGroup toastGroup = null!;
        private RectTransform tileRoot = null!;
        private RectTransform fxLayer = null!;
        private RectTransform banner = null!;
        private CanvasGroup bannerGroup = null!;
        private TextMeshProUGUI bannerLabel = null!;
        private TextMeshProUGUI levelLabel = null!;
        private TextMeshProUGUI counterLabel = null!;

        private readonly List<TileVisual> tiles = new();
        private readonly List<int> fillOrder = new();
        private readonly List<Graphic> revealOnFinish = new();
        private readonly List<Action> finishCallbacks = new();
        private int currentTier;
        private int currentThreshold;
        private int filledCount;
        private int finalTier;
        private int finalProgress;
        private bool isAnimating;
        private bool snapToFinalOnEnable;

        /// <param name="showLevelLabel">
        /// False when the host already titles the card with the level; the meter still updates
        /// its counter, and a level-up is still announced by the banner.
        /// </param>
        public static UI_MoldinessToastMeter Create(Transform parent, float toastWidth = DefaultToastWidth, bool showLevelLabel = true)
        {
            var rootObject = new GameObject("UI_MoldinessToastMeter", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rootObject.transform.SetParent(parent, false);

            var meter = rootObject.AddComponent<UI_MoldinessToastMeter>();
            meter.toastWidth = toastWidth;
            meter.toastHeight = toastWidth * ToastTexHeight / ToastTexWidth;

            // Transparent raycast target so a click anywhere on the meter can skip the animation;
            // only live while an award plays, so a static meter never swallows hover or clicks.
            meter.hitArea = rootObject.GetComponent<Image>();
            meter.hitArea.color = new Color(0f, 0f, 0f, 0f);
            meter.hitArea.raycastTarget = false;

            float levelSpace = showLevelLabel ? LevelLabelHeight + SectionSpacing : 0f;
            float height = levelSpace + meter.toastHeight + SectionSpacing + CounterLabelHeight;
            var element = rootObject.GetComponent<LayoutElement>();
            element.minWidth = toastWidth;
            element.preferredWidth = toastWidth;
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;

            // Hosts whose layout groups don't control child height read the rect size instead.
            ((RectTransform)rootObject.transform).sizeDelta = new Vector2(toastWidth, height);

            meter.BuildChrome(height, levelSpace);
            meter.levelLabel.gameObject.SetActive(showLevelLabel);
            return meter;
        }

        /// <summary>Shows <paramref name="progress"/> points on the slice for <paramref name="tierIndex"/> without animating.</summary>
        public void ShowStatic(int tierIndex, int progress)
        {
            StopAnimation();
            finalTier = Math.Max(0, tierIndex);
            finalProgress = Math.Max(0, progress);
            BuildToast(finalTier, finalProgress);
            SetLevelLabel(finalTier);
            SetCounterLabel();
            FinishReveal(instant: true);
        }

        /// <summary>
        /// Starts from the pre-award state and molds over <paramref name="awarded"/> tiles, crumbling
        /// and replacing the slice at every level crossed. When the replayed award does not land on
        /// the expected post-award state (legacy or synthetic snapshots) the final state is shown
        /// instead, so the meter never contradicts the saved progression.
        /// </summary>
        public void PlayAward(int tierBefore, int progressBefore, int awarded, int expectedTierAfter, int expectedProgressAfter)
        {
            tierBefore = Math.Max(0, tierBefore);
            progressBefore = Math.Max(0, progressBefore);
            var (simulatedTier, simulatedProgress) = Advance(tierBefore, progressBefore, awarded);
            bool replayMatches = simulatedTier == expectedTierAfter && simulatedProgress == expectedProgressAfter;
            if (awarded <= 0 || !replayMatches || !isActiveAndEnabled)
            {
                ShowStatic(expectedTierAfter, expectedProgressAfter);
                return;
            }

            StopAnimation();
            finalTier = simulatedTier;
            finalProgress = simulatedProgress;
            BuildToast(tierBefore, progressBefore);
            SetLevelLabel(tierBefore);
            SetCounterLabel();
            isAnimating = true;
            hitArea.raycastTarget = true;
            foreach (var graphic in revealOnFinish)
            {
                SetAlpha(graphic, 0f);
            }

            StartCoroutine(RunAward(awarded));
        }

        /// <summary>Keeps <paramref name="graphic"/> hidden while an award plays and fades it in once the award settles.</summary>
        public void RevealAfterAnimation(Graphic? graphic)
        {
            if (graphic == null)
            {
                return;
            }

            revealOnFinish.Add(graphic);
            SetAlpha(graphic, isAnimating ? 0f : 1f);
        }

        /// <summary>Runs <paramref name="callback"/> once the award settles, or right away if nothing is animating.</summary>
        public void WhenFinished(Action? callback)
        {
            if (callback == null)
            {
                return;
            }

            if (isAnimating)
            {
                finishCallbacks.Add(callback);
                return;
            }

            callback();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isAnimating)
            {
                SkipToEnd();
            }
        }

        private void OnDisable()
        {
            // Coroutines die with the GameObject. Rebuilding here could run mid-destroy, so the
            // final state is restored when the meter is next enabled instead.
            if (isAnimating)
            {
                StopAllCoroutines();
                snapToFinalOnEnable = true;
            }
        }

        private void OnEnable()
        {
            if (snapToFinalOnEnable)
            {
                snapToFinalOnEnable = false;
                SkipToEnd();
            }
        }

        private void SkipToEnd()
        {
            ShowStatic(finalTier, finalProgress);
        }

        private void StopAnimation()
        {
            StopAllCoroutines();
            isAnimating = false;
            hitArea.raycastTarget = false;
            ClearChildren(fxLayer);
            toastHolder.localScale = Vector3.one;
            toastHolder.localRotation = Quaternion.identity;
            toastGroup.alpha = 1f;
            bannerGroup.alpha = 0f;
            banner.gameObject.SetActive(false);
            levelLabel.rectTransform.localScale = Vector3.one;
            counterLabel.rectTransform.localScale = Vector3.one;
            counterLabel.color = UIStyleTokens.Text.Secondary;
        }

        private IEnumerator RunAward(int awarded)
        {
            yield return new WaitForSecondsRealtime(StartDelay);

            float interval = Mathf.Clamp(MaxFillDuration / awarded, MinPointInterval, PointInterval);
            int remaining = awarded;
            while (remaining > 0)
            {
                if (filledCount < tiles.Count)
                {
                    FillNextTile();
                    remaining--;
                    yield return new WaitForSecondsRealtime(interval);
                }

                if (filledCount >= tiles.Count)
                {
                    yield return LevelUp();
                }
            }

            yield return new WaitForSecondsRealtime(TilePopDuration);
            isAnimating = false;
            hitArea.raycastTarget = false;
            FinishReveal(instant: false);
        }

        private void FillNextTile()
        {
            var tile = tiles[fillOrder[filledCount]];
            filledCount++;
            tile.IsFilled = true;
            SetCounterLabel();
            StartCoroutine(PopTile(tile));
            StartCoroutine(Punch(counterLabel.rectTransform, 1.22f, 0.22f));
            StartCoroutine(FlashCounter());
            SpawnSpores(tile.Root.anchoredPosition, count: 5, minDistance: 14f, maxDistance: 28f, lifetime: 0.55f);
        }

        private IEnumerator PopTile(TileVisual tile)
        {
            var glow = CreateImage("UI_MoldinessTileGlow", fxLayer, GetSoftDotSprite(), UIStyleTokens.Accent.Spore);
            glow.rectTransform.anchoredPosition = tile.Root.anchoredPosition;
            float tileSize = tile.Root.sizeDelta.x;

            tile.Mold.enabled = true;
            var moldRect = tile.Mold.rectTransform;
            yield return Tween(TilePopDuration, t =>
            {
                moldRect.localScale = Vector3.one * EaseOutBack(t, 2.4f);
                SetAlpha(tile.Mold, Mathf.Clamp01(t * 3f));

                glow.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(tileSize * 0.8f, tileSize * 2.2f, EaseOutCubic(t));
                SetAlpha(glow, 0.85f * (1f - t));
            });

            moldRect.localScale = Vector3.one;
            if (glow != null)
            {
                Destroy(glow.gameObject);
            }
        }

        private IEnumerator LevelUp()
        {
            yield return new WaitForSecondsRealtime(0.25f);

            // The slice "ripens": a quick wobble while every tile swells in a wave.
            yield return Tween(0.5f, t =>
            {
                float wobble = Mathf.Sin(t * Mathf.PI * 6f) * 5f * (1f - t);
                toastHolder.localRotation = Quaternion.Euler(0f, 0f, wobble);
                toastHolder.localScale = Vector3.one * (1f + (0.07f * Mathf.Sin(t * Mathf.PI)));
                for (int i = 0; i < tiles.Count; i++)
                {
                    float phase = Mathf.Clamp01((t * 1.6f) - (i / (float)Math.Max(1, tiles.Count)) * 0.6f);
                    tiles[i].Root.localScale = Vector3.one * (1f + (0.22f * Mathf.Sin(phase * Mathf.PI)));
                }
            });
            toastHolder.localRotation = Quaternion.identity;

            // Then it crumbles away into a puff of spores.
            SpawnSpores(new Vector2(0f, -toastHeight * 0.05f), count: 28, minDistance: 50f, maxDistance: 120f, lifetime: 0.9f);
            var scatterDirections = tiles
                .Select(tile => (tile.Root.anchoredPosition.normalized + UnityEngine.Random.insideUnitCircle * 0.6f).normalized)
                .ToArray();
            var scatterSpins = tiles.Select(_ => UnityEngine.Random.Range(-160f, 160f)).ToArray();
            var scatterStarts = tiles.Select(tile => tile.Root.anchoredPosition).ToArray();
            yield return Tween(0.55f, t =>
            {
                float eased = EaseOutCubic(t);
                toastGroup.alpha = 1f - t;
                toastHolder.localScale = Vector3.one * (1f + (0.12f * eased));
                for (int i = 0; i < tiles.Count; i++)
                {
                    tiles[i].Root.anchoredPosition = scatterStarts[i] + (scatterDirections[i] * 46f * eased) + (Vector2.up * 18f * t);
                    tiles[i].Root.localRotation = Quaternion.Euler(0f, 0f, scatterSpins[i] * t);
                    tiles[i].Root.localScale = Vector3.one * (1f - (0.5f * t));
                }
            });

            int nextTier = currentTier + 1;
            BuildToast(nextTier, 0);
            toastGroup.alpha = 0f;
            toastHolder.localScale = Vector3.one;
            SetLevelLabel(nextTier);
            SetCounterLabel();
            StartCoroutine(Punch(levelLabel.rectTransform, 1.3f, 0.35f));

            // Announce the new level over the empty space, then bring in the bigger slice.
            bannerLabel.text =
                $"<b>Level Up!</b>\n<size=62%><color=#{ColorUtility.ToHtmlStringRGB(UIStyleTokens.Text.Primary)}>Moldiness Level {nextTier + 1} unlocked</color></size>";
            banner.gameObject.SetActive(true);
            yield return Tween(0.35f, t =>
            {
                bannerGroup.alpha = Mathf.Clamp01(t * 2f);
                banner.localScale = Vector3.one * EaseOutBack(t, 2.2f);
            });
            yield return new WaitForSecondsRealtime(1.0f);

            yield return Tween(0.4f, t =>
            {
                bannerGroup.alpha = 1f - t;
                banner.localScale = Vector3.one * (1f + (0.1f * t));
                toastGroup.alpha = t;
                toastHolder.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(t, 1.8f));
            });
            banner.gameObject.SetActive(false);
            toastHolder.localScale = Vector3.one;
            toastGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(0.2f);
        }

        private IEnumerator FlashCounter()
        {
            yield return Tween(0.4f, t => counterLabel.color = Color.Lerp(UIStyleTokens.Accent.Spore, UIStyleTokens.Text.Secondary, t));
        }

        private static IEnumerator Punch(RectTransform target, float peakScale, float duration)
        {
            yield return Tween(duration, t =>
            {
                if (target != null)
                {
                    target.localScale = Vector3.one * Mathf.Lerp(peakScale, 1f, EaseOutCubic(t));
                }
            });
        }

        private void SpawnSpores(Vector2 origin, int count, float minDistance, float maxDistance, float lifetime)
        {
            for (int i = 0; i < count; i++)
            {
                StartCoroutine(DriftSpore(origin, minDistance, maxDistance, lifetime));
            }
        }

        private IEnumerator DriftSpore(Vector2 origin, float minDistance, float maxDistance, float lifetime)
        {
            var color = UnityEngine.Random.value < 0.5f ? UIStyleTokens.Accent.Spore : UIStyleTokens.Accent.Lichen;
            var spore = CreateImage("UI_MoldinessSpore", fxLayer, GetSoftDotSprite(), color);
            float size = UnityEngine.Random.Range(4f, 9f);
            spore.rectTransform.sizeDelta = Vector2.one * size;
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            float distance = UnityEngine.Random.Range(minDistance, maxDistance);
            float life = lifetime * UnityEngine.Random.Range(0.75f, 1.15f);

            yield return Tween(life, t =>
            {
                spore.rectTransform.anchoredPosition = origin + (direction * distance * EaseOutCubic(t)) + (Vector2.up * 16f * t);
                SetAlpha(spore, 1f - (t * t));
            });

            if (spore != null)
            {
                Destroy(spore.gameObject);
            }
        }

        private void FinishReveal(bool instant)
        {
            var callbacks = finishCallbacks.ToArray();
            finishCallbacks.Clear();
            foreach (var callback in callbacks)
            {
                callback();
            }

            revealOnFinish.RemoveAll(graphic => graphic == null);
            if (instant || !isActiveAndEnabled)
            {
                foreach (var graphic in revealOnFinish)
                {
                    SetAlpha(graphic, 1f);
                }

                return;
            }

            var graphics = revealOnFinish.ToArray();
            StartCoroutine(Tween(0.35f, t =>
            {
                foreach (var graphic in graphics)
                {
                    SetAlpha(graphic, t);
                }
            }));
        }

        private void BuildChrome(float height, float levelSpace)
        {
            float toastCenterY = (height * 0.5f) - levelSpace - (toastHeight * 0.5f);

            levelLabel = CreateLabel("UI_MoldinessToastLevel", 22f, FontStyles.Bold, UIStyleTokens.Text.Primary);
            PlaceCentered(levelLabel.rectTransform, new Vector2(toastWidth + 40f, LevelLabelHeight), new Vector2(0f, (height - LevelLabelHeight) * 0.5f));

            var holderObject = new GameObject("UI_MoldinessToast", typeof(RectTransform), typeof(CanvasGroup));
            toastHolder = holderObject.GetComponent<RectTransform>();
            toastHolder.SetParent(transform, false);
            PlaceCentered(toastHolder, new Vector2(toastWidth, toastHeight), new Vector2(0f, toastCenterY));
            toastGroup = holderObject.GetComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;

            // The shadow lives inside the holder so it fades and crumbles with the slice.
            var shadow = CreateImage("UI_MoldinessToastShadow", toastHolder, GetToastSprite(), new Color(0f, 0f, 0f, 0.35f));
            PlaceCentered(shadow.rectTransform, new Vector2(toastWidth, toastHeight), new Vector2(4f, -5f));

            var toast = CreateImage("UI_MoldinessToastSlice", toastHolder, GetToastSprite(), Color.white);
            PlaceCentered(toast.rectTransform, new Vector2(toastWidth, toastHeight), Vector2.zero);

            var tileRootObject = new GameObject("UI_MoldinessToastTiles", typeof(RectTransform));
            tileRoot = tileRootObject.GetComponent<RectTransform>();
            tileRoot.SetParent(toastHolder, false);
            PlaceCentered(tileRoot, new Vector2(toastWidth, toastHeight), Vector2.zero);

            var fxObject = new GameObject("UI_MoldinessToastFx", typeof(RectTransform));
            fxLayer = fxObject.GetComponent<RectTransform>();
            fxLayer.SetParent(transform, false);
            PlaceCentered(fxLayer, new Vector2(toastWidth, toastHeight), new Vector2(0f, toastCenterY));

            var bannerObject = new GameObject("UI_MoldinessLevelUpBanner", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(CanvasGroup));
            banner = bannerObject.GetComponent<RectTransform>();
            banner.SetParent(transform, false);
            PlaceCentered(banner, new Vector2(toastWidth + 30f, 78f), new Vector2(0f, toastCenterY));
            var bannerBackground = bannerObject.GetComponent<Image>();
            bannerBackground.color = new Color(UIStyleTokens.Surface.PanelPrimary.r, UIStyleTokens.Surface.PanelPrimary.g, UIStyleTokens.Surface.PanelPrimary.b, 0.95f);
            bannerBackground.raycastTarget = false;
            var outline = bannerObject.GetComponent<Outline>();
            outline.effectColor = UIStyleTokens.State.Warning;
            outline.effectDistance = new Vector2(2f, -2f);
            bannerGroup = bannerObject.GetComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            bannerLabel = CreateLabel("UI_MoldinessLevelUpText", 30f, FontStyles.Normal, UIStyleTokens.State.Warning, banner);
            bannerLabel.rectTransform.anchorMin = Vector2.zero;
            bannerLabel.rectTransform.anchorMax = Vector2.one;
            bannerLabel.rectTransform.offsetMin = new Vector2(8f, 4f);
            bannerLabel.rectTransform.offsetMax = new Vector2(-8f, -4f);
            banner.gameObject.SetActive(false);

            counterLabel = CreateLabel("UI_MoldinessToastCounter", 18f, FontStyles.Normal, UIStyleTokens.Text.Secondary);
            PlaceCentered(counterLabel.rectTransform, new Vector2(toastWidth + 40f, CounterLabelHeight), new Vector2(0f, -(height - CounterLabelHeight) * 0.5f));
        }

        private void BuildToast(int tierIndex, int progress)
        {
            ClearChildren(tileRoot);
            tiles.Clear();
            fillOrder.Clear();

            currentTier = tierIndex;
            currentThreshold = Math.Max(1, MoldinessProgression.GetThresholdForTier(tierIndex));
            filledCount = Mathf.Clamp(progress, 0, currentThreshold);

            float areaWidth = (CrumbMaxX - CrumbMinX) * toastWidth;
            float areaHeight = (CrumbMaxY - CrumbMinY) * toastHeight;
            var areaCenter = new Vector2(
                (((CrumbMinX + CrumbMaxX) * 0.5f) - 0.5f) * toastWidth,
                (((CrumbMinY + CrumbMaxY) * 0.5f) - 0.5f) * toastHeight);
            var (columns, rows, cell) = ChooseGrid(currentThreshold, areaWidth, areaHeight);
            float tileSize = Mathf.Min(cell * TileFillRatio, MaxTileSize);
            var moldSprites = GetMoldTileSprites();

            for (int i = 0; i < currentThreshold; i++)
            {
                int row = i / columns;
                int column = i % columns;
                int tilesInRow = row == rows - 1 ? currentThreshold - (columns * (rows - 1)) : columns;
                var position = areaCenter + new Vector2(
                    (column - ((tilesInRow - 1) * 0.5f)) * cell,
                    (((rows - 1) * 0.5f) - row) * cell);

                var slot = CreateImage($"UI_MoldinessToastTile_{i + 1}", tileRoot, GetRoundedTileSprite(), EmptyTileColor);
                PlaceCentered(slot.rectTransform, Vector2.one * tileSize, position);

                // Stable per-tile variety so a slice looks the same every time it is shown.
                int variety = Mathf.FloorToInt(Hash01(i, currentThreshold, 37) * 1000f);
                var mold = CreateImage("UI_MoldinessToastMold", slot.rectTransform, moldSprites[variety % moldSprites.Length], Color.white);
                PlaceCentered(mold.rectTransform, Vector2.one * tileSize * 1.08f, Vector2.zero);
                mold.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f * (variety / 7 % 4));
                mold.enabled = false;

                tiles.Add(new TileVisual { Root = slot.rectTransform, Mold = mold });
            }

            fillOrder.AddRange(BuildFillOrder(currentThreshold, columns, rows));
            for (int i = 0; i < filledCount; i++)
            {
                var tile = tiles[fillOrder[i]];
                tile.IsFilled = true;
                tile.Mold.enabled = true;
            }
        }

        private void SetLevelLabel(int tierIndex)
        {
            levelLabel.text = $"Moldiness Level {tierIndex + 1}";
        }

        private void SetCounterLabel()
        {
            counterLabel.text = $"{filledCount} / {currentThreshold} to Level {currentTier + 2}";
        }

        private static (int tier, int progress) Advance(int tier, int progress, int amount)
        {
            progress += Math.Max(0, amount);
            while (progress >= MoldinessProgression.GetThresholdForTier(tier))
            {
                progress -= MoldinessProgression.GetThresholdForTier(tier);
                tier++;
            }

            return (tier, progress);
        }

        private static (int columns, int rows, float cell) ChooseGrid(int count, float width, float height)
        {
            int bestColumns = 1;
            int bestRows = count;
            float bestCell = 0f;
            for (int columns = 1; columns <= count; columns++)
            {
                int rows = Mathf.CeilToInt(count / (float)columns);
                float cell = Mathf.Min(width / columns, height / rows);
                if (cell > bestCell + 0.01f)
                {
                    bestColumns = columns;
                    bestRows = rows;
                    bestCell = cell;
                }
            }

            return (bestColumns, bestRows, bestCell);
        }

        // Mold spreads outward from one or two seed tiles near the middle of the crumb, so each
        // new point grows the colony instead of filling the slice like a progress bar.
        private static List<int> BuildFillOrder(int tileCount, int columns, int rows)
        {
            var available = new HashSet<int>(Enumerable.Range(0, tileCount));
            var frontier = new HashSet<int>();
            var ordered = new List<int>(tileCount);

            int seedCount = tileCount >= 10 ? 2 : 1;
            for (int s = 0; s < seedCount; s++)
            {
                int salt = (s + 1) * 193;
                int seed = available
                    .OrderBy(index => (CenterDistance(index, columns, rows) * 0.65f) + (Hash01(index, tileCount, salt) * 0.35f))
                    .ThenBy(index => index)
                    .First();
                available.Remove(seed);
                frontier.Remove(seed);
                ordered.Add(seed);
                AddNeighbors(seed, tileCount, columns, rows, frontier, available);
            }

            while (ordered.Count < tileCount)
            {
                if (frontier.Count == 0)
                {
                    frontier.Add(available
                        .OrderBy(index => (CenterDistance(index, columns, rows) * 0.55f) + (Hash01(index, tileCount, 887) * 0.45f))
                        .ThenBy(index => index)
                        .First());
                }

                int next = frontier
                    .OrderBy(index => (Hash01(index, tileCount, 521) * 0.6f) + (CenterDistance(index, columns, rows) * 0.4f))
                    .ThenBy(index => index)
                    .First();
                frontier.Remove(next);
                if (!available.Remove(next))
                {
                    continue;
                }

                ordered.Add(next);
                AddNeighbors(next, tileCount, columns, rows, frontier, available);
            }

            return ordered;
        }

        private static void AddNeighbors(int index, int tileCount, int columns, int rows, HashSet<int> frontier, HashSet<int> available)
        {
            int row = index / columns;
            int column = index % columns;
            for (int rowDelta = -1; rowDelta <= 1; rowDelta++)
            {
                for (int columnDelta = -1; columnDelta <= 1; columnDelta++)
                {
                    int neighborRow = row + rowDelta;
                    int neighborColumn = column + columnDelta;
                    if ((rowDelta == 0 && columnDelta == 0)
                        || neighborRow < 0 || neighborRow >= rows
                        || neighborColumn < 0 || neighborColumn >= columns)
                    {
                        continue;
                    }

                    int neighbor = (neighborRow * columns) + neighborColumn;
                    if (neighbor < tileCount && available.Contains(neighbor))
                    {
                        frontier.Add(neighbor);
                    }
                }
            }
        }

        private static float CenterDistance(int index, int columns, int rows)
        {
            float x = columns <= 1 ? 0.5f : (index % columns) / (float)(columns - 1);
            float y = rows <= 1 ? 0.5f : (index / columns) / (float)(rows - 1);
            return Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));
        }

        private static float Hash01(int index, int tileCount, int salt)
        {
            unchecked
            {
                uint value = (uint)(index + 1);
                value ^= (uint)tileCount * 2246822519u;
                value ^= (uint)salt * 3266489917u;
                value *= 668265263u;
                value ^= value >> 15;
                value *= 2246822519u;
                value ^= value >> 13;
                return (value & 0x00FFFFFFu) / 16777215f;
            }
        }

        private static IEnumerator Tween(float duration, Action<float> step)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                step(elapsed / duration);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            step(1f);
        }

        private static float EaseOutCubic(float t)
        {
            float inverse = 1f - t;
            return 1f - (inverse * inverse * inverse);
        }

        private static float EaseOutBack(float t, float overshoot)
        {
            float shifted = t - 1f;
            return 1f + ((overshoot + 1f) * shifted * shifted * shifted) + (overshoot * shifted * shifted);
        }

        private static void SetAlpha(Graphic? graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static void ClearChildren(Transform? parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        private static void PlaceCentered(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI CreateLabel(string name, float fontSize, FontStyles fontStyle, Color color, Transform? parent = null)
        {
            var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent ?? transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        // ---- Procedural sprites -------------------------------------------------------------

        private static Sprite GetToastSprite()
        {
            if (toastSprite != null)
            {
                return toastSprite;
            }

            var texture = NewTexture("MoldinessToastSlice", ToastTexWidth, ToastTexHeight);
            var random = new System.Random(4127);
            var pixels = new Color[ToastTexWidth * ToastTexHeight];
            for (int y = 0; y < ToastTexHeight; y++)
            {
                for (int x = 0; x < ToastTexWidth; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    int covered = 0;
                    for (int sample = 0; sample < 4; sample++)
                    {
                        if (IsInsideToast(x + 0.25f + (0.5f * (sample % 2)), y + 0.25f + (0.5f * (sample / 2)), 0f))
                        {
                            covered++;
                        }
                    }

                    if (covered == 0)
                    {
                        pixels[(y * ToastTexWidth) + x] = Color.clear;
                        continue;
                    }

                    Color color;
                    if (!IsInsideToast(px, py, 2.5f))
                    {
                        color = CrustRimColor;
                    }
                    else if (!IsInsideToast(px, py, 8f))
                    {
                        color = Color.Lerp(CrustColor, CrustRimColor, (float)random.NextDouble() * 0.2f);
                    }
                    else if (!IsInsideToast(px, py, 16f))
                    {
                        color = Color.Lerp(CrumbToastedColor, CrumbColor, 0.35f);
                    }
                    else
                    {
                        color = CrumbColor;
                    }

                    color.a = covered / 4f;
                    pixels[(y * ToastTexWidth) + x] = color;
                }
            }

            // Air pockets in the crumb.
            for (int i = 0; i < 90; i++)
            {
                int cx = random.Next(ToastTexWidth);
                int cy = random.Next(ToastTexHeight);
                float radius = 0.8f + ((float)random.NextDouble() * 1.8f);
                for (int y = (int)(cy - radius); y <= cy + radius; y++)
                {
                    for (int x = (int)(cx - radius); x <= cx + radius; x++)
                    {
                        if (x < 0 || y < 0 || x >= ToastTexWidth || y >= ToastTexHeight)
                        {
                            continue;
                        }

                        float dx = x - cx;
                        float dy = y - cy;
                        if ((dx * dx) + (dy * dy) > radius * radius || !IsInsideToast(x + 0.5f, y + 0.5f, 10f))
                        {
                            continue;
                        }

                        int index = (y * ToastTexWidth) + x;
                        pixels[index] = Color.Lerp(pixels[index], CrumbPoreColor, 0.55f);
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            toastSprite = Sprite.Create(texture, new Rect(0, 0, ToastTexWidth, ToastTexHeight), new Vector2(0.5f, 0.5f), 100f);
            return toastSprite;
        }

        /// <summary>
        /// Classic sandwich-bread slice in texture pixels: a rectangular body with rounded bottom
        /// corners under two overlapping lobes that overhang the sides. <paramref name="inset"/>
        /// shrinks the silhouette uniformly, which is how the crust bands are drawn.
        /// </summary>
        private static bool IsInsideToast(float x, float y, float inset)
        {
            const float bodyLeft = 14f;
            const float bodyRight = 146f;
            const float bodyBottom = 5f;
            const float bodyTop = 116f;
            const float cornerRadius = 16f;
            const float lobeRadius = 46f;
            const float lobeLeftX = 52f;
            const float lobeRightX = 108f;
            const float lobeY = 116f;

            float lobe = lobeRadius - inset;
            if (Sqr(x - lobeLeftX) + Sqr(y - lobeY) <= lobe * lobe
                || Sqr(x - lobeRightX) + Sqr(y - lobeY) <= lobe * lobe)
            {
                return true;
            }

            float left = bodyLeft + inset;
            float right = bodyRight - inset;
            float bottom = bodyBottom + inset;
            if (x < left || x > right || y < bottom || y > bodyTop)
            {
                return false;
            }

            float radius = cornerRadius - Mathf.Min(inset, cornerRadius - 1f);
            if (y < bottom + radius)
            {
                float cornerX = x < left + radius ? left + radius : (x > right - radius ? right - radius : x);
                return Sqr(x - cornerX) + Sqr(y - (bottom + radius)) <= radius * radius;
            }

            return true;
        }

        private static Sprite GetRoundedTileSprite()
        {
            if (roundedTileSprite != null)
            {
                return roundedTileSprite;
            }

            var texture = NewTexture("MoldinessToastTileSlot", TileTexSize, TileTexSize);
            var pixels = new Color[TileTexSize * TileTexSize];
            for (int y = 0; y < TileTexSize; y++)
            {
                for (int x = 0; x < TileTexSize; x++)
                {
                    float distance = RoundedSquareDistance(x + 0.5f, y + 0.5f, TileTexSize, 7f);
                    pixels[(y * TileTexSize) + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            roundedTileSprite = Sprite.Create(texture, new Rect(0, 0, TileTexSize, TileTexSize), new Vector2(0.5f, 0.5f), 100f);
            return roundedTileSprite;
        }

        private static Sprite GetSoftDotSprite()
        {
            if (softDotSprite != null)
            {
                return softDotSprite;
            }

            const int size = 16;
            var texture = NewTexture("MoldinessSporeDot", size, size);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                    float alpha = Mathf.Clamp01(1f - distance);
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha * alpha * (3f - (2f * alpha)));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            softDotSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return softDotSprite;
        }

        /// <summary>
        /// Fuzzy mold patches: mottled greens from value noise, a few pale spore flecks, and a
        /// hyphae-white rim that frays past the tile edge.
        /// </summary>
        private static Sprite[] GetMoldTileSprites()
        {
            if (moldTileSprites != null && moldTileSprites.All(sprite => sprite != null))
            {
                return moldTileSprites;
            }

            moldTileSprites = new Sprite[MoldVariantCount];
            for (int variant = 0; variant < MoldVariantCount; variant++)
            {
                var random = new System.Random(9001 + (variant * 7919));
                const int noiseCells = 6;
                var noise = new float[(noiseCells + 1) * (noiseCells + 1)];
                for (int i = 0; i < noise.Length; i++)
                {
                    noise[i] = (float)random.NextDouble();
                }

                var texture = NewTexture($"MoldinessToastMold_{variant}", TileTexSize, TileTexSize);
                var pixels = new Color[TileTexSize * TileTexSize];
                for (int y = 0; y < TileTexSize; y++)
                {
                    for (int x = 0; x < TileTexSize; x++)
                    {
                        float fuzz = ((float)random.NextDouble() - 0.5f) * 2.4f;
                        float distance = RoundedSquareDistance(x + 0.5f, y + 0.5f, TileTexSize, 8f) + fuzz;
                        float alpha = Mathf.Clamp01(0.8f - (distance * 0.6f));
                        if (alpha <= 0f)
                        {
                            pixels[(y * TileTexSize) + x] = Color.clear;
                            continue;
                        }

                        float mottling = SampleValueNoise(noise, noiseCells, x / (float)TileTexSize, y / (float)TileTexSize);
                        var color = Color.Lerp(MoldDarkColor, UIStyleTokens.Accent.Moss, mottling);
                        color = Color.Lerp(color, UIStyleTokens.Accent.Lichen, Mathf.Clamp01((mottling - 0.6f) * 2f));
                        if (random.NextDouble() < 0.05)
                        {
                            color = UIStyleTokens.Accent.Spore;
                        }

                        if (distance > -3.5f)
                        {
                            color = Color.Lerp(color, UIStyleTokens.Accent.Hyphae, 0.4f);
                        }

                        color.a = alpha;
                        pixels[(y * TileTexSize) + x] = color;
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();
                moldTileSprites[variant] = Sprite.Create(texture, new Rect(0, 0, TileTexSize, TileTexSize), new Vector2(0.5f, 0.5f), 100f);
            }

            return moldTileSprites;
        }

        private static float SampleValueNoise(float[] noise, int cells, float u, float v)
        {
            float gx = u * cells;
            float gy = v * cells;
            int x0 = Mathf.Clamp((int)gx, 0, cells - 1);
            int y0 = Mathf.Clamp((int)gy, 0, cells - 1);
            float tx = gx - x0;
            float ty = gy - y0;
            tx = tx * tx * (3f - (2f * tx));
            ty = ty * ty * (3f - (2f * ty));
            int stride = cells + 1;
            float bottom = Mathf.Lerp(noise[(y0 * stride) + x0], noise[(y0 * stride) + x0 + 1], tx);
            float top = Mathf.Lerp(noise[((y0 + 1) * stride) + x0], noise[((y0 + 1) * stride) + x0 + 1], tx);
            return Mathf.Lerp(bottom, top, ty);
        }

        /// <summary>Signed distance in pixels from a centered rounded square filling <paramref name="size"/>, negative inside.</summary>
        private static float RoundedSquareDistance(float x, float y, int size, float radius)
        {
            float half = (size * 0.5f) - 1.5f;
            float qx = Mathf.Abs(x - (size * 0.5f)) - (half - radius);
            float qy = Mathf.Abs(y - (size * 0.5f)) - (half - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        private static Texture2D NewTexture(string name, int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        private static float Sqr(float value) => value * value;
    }
}
