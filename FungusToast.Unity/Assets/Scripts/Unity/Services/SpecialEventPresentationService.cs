using FungusToast.Core.Board;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Events;
using FungusToast.Core.Players;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI;
using UnityEngine;

namespace FungusToast.Unity
{
    public class SpecialEventPresentationService
    {
        private readonly Func<GameUIManager> getGameUIManager;
        private readonly Func<GridVisualizer> getGridVisualizer;
        private readonly Func<Player> getHumanPlayer;
        private readonly Func<bool> isFastForwarding;
        private readonly Queue<SpecialBoardEventArgs> pendingImmediateEvents = new();
        private readonly Queue<SpecialBoardEventArgs> pendingPostDecayEvents = new();

        private bool isPresenting;

        public SpecialEventPresentationService(
            Func<GameUIManager> getGameUIManager,
            Func<GridVisualizer> getGridVisualizer,
            Func<Player> getHumanPlayer,
            Func<bool> isFastForwarding)
        {
            this.getGameUIManager = getGameUIManager;
            this.getGridVisualizer = getGridVisualizer;
            this.getHumanPlayer = getHumanPlayer;
            this.isFastForwarding = isFastForwarding;
        }

        public bool HasPendingEvents => pendingPostDecayEvents.Count > 0;
        public bool HasPendingImmediateEvents => pendingImmediateEvents.Count > 0;

        public void Reset()
        {
            pendingImmediateEvents.Clear();
            pendingPostDecayEvents.Clear();
            isPresenting = false;
            getGridVisualizer()?.ClearPreAnimationPreviewTiles();
        }

        public void Enqueue(SpecialBoardEventArgs e)
        {
            if (e == null || isFastForwarding())
            {
                return;
            }

            var humanPlayer = getHumanPlayer();
            if (humanPlayer == null || e.PlayerId != humanPlayer.PlayerId)
            {
                return;
            }

            if (IsImmediateEvent(e.EventKind))
            {
                RegisterPreAnimationPreviewTiles(e);
                pendingImmediateEvents.Enqueue(e);
                return;
            }

            pendingPostDecayEvents.Enqueue(e);
        }

        private void RegisterPreAnimationPreviewTiles(SpecialBoardEventArgs specialEvent)
        {
            if (specialEvent?.EventKind != SpecialBoardEventKind.HyphalBridgeTriggered)
            {
                return;
            }

            var gridVisualizer = getGridVisualizer();
            if (gridVisualizer == null)
            {
                return;
            }

            gridVisualizer.RegisterPreAnimationHiddenPreviewTiles(specialEvent.AffectedTileIds);
        }

        public IEnumerator PresentPendingImmediate()
        {
            if (isPresenting || pendingImmediateEvents.Count == 0 || isFastForwarding())
            {
                yield break;
            }

            isPresenting = true;
            try
            {
                while (pendingImmediateEvents.Count > 0)
                {
                    var specialEvent = pendingImmediateEvents.Dequeue();

                    if (specialEvent.EventKind == SpecialBoardEventKind.SporeSalvoTriggered)
                    {
                        yield return PresentSporeSalvoBatch(CollectSporeSalvoBatch(specialEvent));
                        continue;
                    }

                    if (specialEvent.EventKind == SpecialBoardEventKind.MarginalClampTriggered)
                    {
                        yield return PresentMarginalClampBatch(CollectMarginalClampBatch(specialEvent));
                        continue;
                    }

                    if (specialEvent.EventKind == SpecialBoardEventKind.VesicleBurstTriggered)
                    {
                        yield return PresentVesicleBurstBatch(CollectVesicleBurstBatch(specialEvent));
                        continue;
                    }

                    yield return PresentSpecialEvent(specialEvent);
                }
            }
            finally
            {
                isPresenting = false;
            }
        }

        public IEnumerator PresentPendingAfterDecayRender()
        {
            if (isPresenting || pendingPostDecayEvents.Count == 0 || isFastForwarding())
            {
                yield break;
            }

            isPresenting = true;
            try
            {
                while (pendingPostDecayEvents.Count > 0)
                {
                    var specialEvent = pendingPostDecayEvents.Dequeue();

                    if (specialEvent.EventKind == SpecialBoardEventKind.MycotoxicLashTriggered)
                    {
                        yield return PresentMycotoxicLashBatch(CollectMycotoxicLashBatch(specialEvent));
                        continue;
                    }

                    if (specialEvent.EventKind == SpecialBoardEventKind.SaprophageRingTriggered)
                    {
                        yield return PresentSaprophageRingBatch(CollectSaprophageRingBatch(specialEvent));
                        continue;
                    }

                    yield return PresentSpecialEvent(specialEvent);
                }
            }
            finally
            {
                isPresenting = false;
            }
        }

        private IEnumerator PresentSpecialEvent(SpecialBoardEventArgs specialEvent)
        {
            var uiManager = getGameUIManager();
            var gridVisualizer = getGridVisualizer();
            if (uiManager == null || gridVisualizer == null)
            {
                yield break;
            }

            switch (specialEvent.EventKind)
            {
                case SpecialBoardEventKind.ConidialRelayTriggered:
                    uiManager.GameLogRouter?.RecordConidialRelayRelocation(specialEvent.PlayerId);
                    uiManager.PhaseBanner?.Show(
                        "Conidial Relay triggered!",
                        UIEffectConstants.ConidialRelayBannerHoldSeconds);
                    yield return gridVisualizer.PlayConidialRelayAnimation(
                        specialEvent.PlayerId,
                        specialEvent.SourceTileId,
                        specialEvent.DestinationTileId);
                    break;
                case SpecialBoardEventKind.HyphalBridgeTriggered:
                    int bridgeCells = specialEvent.AffectedTileIds?.Count ?? 0;
                    if (bridgeCells <= 0)
                    {
                        yield break;
                    }

                    uiManager.GameLogRouter?.RecordHyphalBridgeDeployment(specialEvent.PlayerId, bridgeCells);
                    uiManager.PhaseBanner?.Show(
                        BuildHyphalBridgeBannerText(bridgeCells),
                        UIEffectConstants.HyphalBridgeBannerHoldSeconds);
                    yield return gridVisualizer.PlayHyphalBridgeAnimation(
                        specialEvent.PlayerId,
                        specialEvent.SourceTileId,
                        specialEvent.AffectedTileIds.ToList());
                    break;
                case SpecialBoardEventKind.DistalSporeTriggered:
                    uiManager.GameLogRouter?.RecordDistalSporeDeployment(specialEvent.PlayerId);
                    uiManager.PhaseBanner?.Show(
                        "Distal Spore triggered!",
                        UIEffectConstants.ConidialRelayBannerHoldSeconds);
                    var activeBoard = gridVisualizer.ActiveBoard;
                    if (activeBoard != null)
                    {
                        gridVisualizer.RenderBoard(activeBoard, suppressAnimations: true);
                    }
                    yield return gridVisualizer.PlayDistalSporeAnimation(
                        specialEvent.PlayerId,
                        specialEvent.SourceTileId,
                        specialEvent.DestinationTileId);
                    if (activeBoard != null)
                    {
                        gridVisualizer.RenderBoard(activeBoard, suppressAnimations: true);
                    }
                    break;
                case SpecialBoardEventKind.RetrogradeBloomTriggered:
                    uiManager.PhaseBanner?.Show(
                        "Retrograde Bloom twists your mutation tree!",
                        UIEffectConstants.RetrogradeBloomBannerHoldSeconds);
                    yield return gridVisualizer.PlayRetrogradeBloomAnimation(specialEvent.SourceTileId);
                    break;
                case SpecialBoardEventKind.MarginalClampTriggered:
                    int cellsKilled = specialEvent.AffectedTileIds?.Distinct().Count() ?? 0;
                    if (cellsKilled <= 0)
                    {
                        yield break;
                    }

                    uiManager.PhaseBanner?.Show(
                        BuildMarginalClampBannerText(cellsKilled),
                        UIEffectConstants.MarginalClampBannerHoldSeconds);
                    uiManager.GameLogRouter?.RecordMarginalClampKills(specialEvent.PlayerId, cellsKilled);
                    yield return gridVisualizer.PlayMycotoxicLashAnimation(specialEvent.AffectedTileIds.Distinct().ToList());
                    break;
                case SpecialBoardEventKind.VesicleBurstTriggered:
                    int affectedTiles = specialEvent.AffectedTileIds?.Distinct().Count() ?? 0;
                    if (affectedTiles <= 0)
                    {
                        yield break;
                    }

                    uiManager.PhaseBanner?.Show(
                        BuildVesicleBurstBannerText(affectedTiles),
                        UIEffectConstants.MarginalClampBannerHoldSeconds);
                    yield return gridVisualizer.PlayMycotoxicLashAnimation(specialEvent.AffectedTileIds.Distinct().ToList());
                    break;
                case SpecialBoardEventKind.NutrientPatchConsumed:
                    yield return gridVisualizer.PlayNutrientPatchConsumptionAnimation(
                        specialEvent.SourceTileId,
                        specialEvent.DestinationTileId,
                        specialEvent.NutrientPatchType ?? NutrientPatchType.Adaptogen,
                        specialEvent.NutrientRewardType ?? NutrientRewardType.MutationPoints,
                        specialEvent.RewardAmount > 0 ? specialEvent.RewardAmount : specialEvent.AffectedTileIds.Distinct().Count());
                    break;
            }
        }

        private static bool IsImmediateEvent(SpecialBoardEventKind eventKind)
        {
            return eventKind == SpecialBoardEventKind.RetrogradeBloomTriggered
                || eventKind == SpecialBoardEventKind.MarginalClampTriggered
                || eventKind == SpecialBoardEventKind.DistalSporeTriggered
                || eventKind == SpecialBoardEventKind.SporeSalvoTriggered
                || eventKind == SpecialBoardEventKind.HyphalBridgeTriggered
                || eventKind == SpecialBoardEventKind.NutrientPatchConsumed;
        }

        private List<SpecialBoardEventArgs> CollectSporeSalvoBatch(SpecialBoardEventArgs firstEvent)
        {
            var batch = new List<SpecialBoardEventArgs> { firstEvent };

            while (pendingImmediateEvents.Count > 0)
            {
                var nextEvent = pendingImmediateEvents.Peek();
                if (nextEvent.EventKind != SpecialBoardEventKind.SporeSalvoTriggered || nextEvent.PlayerId != firstEvent.PlayerId)
                {
                    break;
                }

                batch.Add(pendingImmediateEvents.Dequeue());
            }

            return batch;
        }

        private List<SpecialBoardEventArgs> CollectMycotoxicLashBatch(SpecialBoardEventArgs firstEvent)
        {
            var batch = new List<SpecialBoardEventArgs> { firstEvent };

            while (pendingPostDecayEvents.Count > 0)
            {
                var nextEvent = pendingPostDecayEvents.Peek();
                if (nextEvent.EventKind != SpecialBoardEventKind.MycotoxicLashTriggered || nextEvent.PlayerId != firstEvent.PlayerId)
                {
                    break;
                }

                batch.Add(pendingPostDecayEvents.Dequeue());
            }

            return batch;
        }

        private List<SpecialBoardEventArgs> CollectSaprophageRingBatch(SpecialBoardEventArgs firstEvent)
        {
            var batch = new List<SpecialBoardEventArgs> { firstEvent };

            while (pendingPostDecayEvents.Count > 0)
            {
                var nextEvent = pendingPostDecayEvents.Peek();
                if (nextEvent.EventKind != SpecialBoardEventKind.SaprophageRingTriggered || nextEvent.PlayerId != firstEvent.PlayerId)
                {
                    break;
                }

                batch.Add(pendingPostDecayEvents.Dequeue());
            }

            return batch;
        }

        private List<SpecialBoardEventArgs> CollectMarginalClampBatch(SpecialBoardEventArgs firstEvent)
        {
            var batch = new List<SpecialBoardEventArgs> { firstEvent };

            while (pendingImmediateEvents.Count > 0)
            {
                var nextEvent = pendingImmediateEvents.Peek();
                if (nextEvent.EventKind != SpecialBoardEventKind.MarginalClampTriggered || nextEvent.PlayerId != firstEvent.PlayerId)
                {
                    break;
                }

                batch.Add(pendingImmediateEvents.Dequeue());
            }

            return batch;
        }

        private List<SpecialBoardEventArgs> CollectVesicleBurstBatch(SpecialBoardEventArgs firstEvent)
        {
            var batch = new List<SpecialBoardEventArgs> { firstEvent };

            while (pendingImmediateEvents.Count > 0)
            {
                var nextEvent = pendingImmediateEvents.Peek();
                if (nextEvent.EventKind != SpecialBoardEventKind.VesicleBurstTriggered || nextEvent.PlayerId != firstEvent.PlayerId)
                {
                    break;
                }

                batch.Add(pendingImmediateEvents.Dequeue());
            }

            return batch;
        }

        private IEnumerator PresentMycotoxicLashBatch(IReadOnlyList<SpecialBoardEventArgs> events)
        {
            if (events == null || events.Count == 0)
            {
                yield break;
            }

            var uiManager = getGameUIManager();
            var gridVisualizer = getGridVisualizer();
            if (uiManager == null || gridVisualizer == null)
            {
                yield break;
            }

            var affectedTileIds = events
                .SelectMany(e => e.AffectedTileIds ?? new List<int>())
                .Distinct()
                .ToList();
            if (affectedTileIds.Count == 0)
            {
                yield break;
            }

            uiManager.PhaseBanner?.Show(
                BuildMycotoxicLashBannerText(affectedTileIds.Count),
                0f);
            uiManager.GameLogRouter?.RecordMycotoxicLashKills(events[0].PlayerId, affectedTileIds.Count);
            yield return gridVisualizer.PlayMycotoxicLashAnimation(affectedTileIds);
        }

        private IEnumerator PresentSaprophageRingBatch(IReadOnlyList<SpecialBoardEventArgs> events)
        {
            if (events == null || events.Count == 0)
            {
                yield break;
            }

            var uiManager = getGameUIManager();
            var gridVisualizer = getGridVisualizer();
            if (uiManager == null || gridVisualizer == null)
            {
                yield break;
            }

            var consumedTileIds = events
                .SelectMany(e => e.AffectedTileIds ?? new List<int>())
                .Distinct()
                .ToList();
            if (consumedTileIds.Count == 0)
            {
                yield break;
            }

            var resistantTileIds = events
                .Select(e => e.SourceTileId)
                .Where(tileId => tileId >= 0)
                .Distinct()
                .ToList();

            uiManager.PhaseBanner?.Show(
                BuildSaprophageRingBannerText(consumedTileIds.Count),
                UIEffectConstants.SaprophageRingBannerHoldSeconds);
            uiManager.GameLogRouter?.RecordSaprophageRingConsumption(events[0].PlayerId, consumedTileIds.Count);
            yield return gridVisualizer.PlaySaprophageRingAnimation(resistantTileIds, consumedTileIds);
        }

        private IEnumerator PresentMarginalClampBatch(IReadOnlyList<SpecialBoardEventArgs> events)
        {
            if (events == null || events.Count == 0)
            {
                yield break;
            }

            var uiManager = getGameUIManager();
            var gridVisualizer = getGridVisualizer();
            if (uiManager == null || gridVisualizer == null)
            {
                yield break;
            }

            var affectedTileIds = events
                .SelectMany(e => e.AffectedTileIds ?? new List<int>())
                .Distinct()
                .ToList();
            if (affectedTileIds.Count == 0)
            {
                yield break;
            }

            uiManager.PhaseBanner?.Show(
                BuildMarginalClampBannerText(affectedTileIds.Count),
                UIEffectConstants.MarginalClampBannerHoldSeconds);
            uiManager.GameLogRouter?.RecordMarginalClampKills(events[0].PlayerId, affectedTileIds.Count);
            yield return gridVisualizer.PlayMycotoxicLashAnimation(affectedTileIds);
        }

        private IEnumerator PresentVesicleBurstBatch(IReadOnlyList<SpecialBoardEventArgs> events)
        {
            if (events == null || events.Count == 0)
            {
                yield break;
            }

            var uiManager = getGameUIManager();
            var gridVisualizer = getGridVisualizer();
            if (uiManager == null || gridVisualizer == null)
            {
                yield break;
            }

            var affectedTileIds = events
                .SelectMany(e => e.AffectedTileIds ?? new List<int>())
                .Distinct()
                .ToList();
            if (affectedTileIds.Count == 0)
            {
                yield break;
            }

            uiManager.PhaseBanner?.Show(
                BuildVesicleBurstBannerText(affectedTileIds.Count),
                UIEffectConstants.MarginalClampBannerHoldSeconds);
            yield return gridVisualizer.PlayMycotoxicLashAnimation(affectedTileIds);
        }

        private IEnumerator PresentSporeSalvoBatch(IReadOnlyList<SpecialBoardEventArgs> events)
        {
            if (events == null || events.Count == 0)
            {
                yield break;
            }

            var uiManager = getGameUIManager();
            var gridVisualizer = getGridVisualizer();
            if (uiManager == null || gridVisualizer == null)
            {
                yield break;
            }

            uiManager.PhaseBanner?.Show(
                BuildSporeSalvoBannerText(),
                UIEffectConstants.SporeSalvoBannerHoldSeconds);
            uiManager.GameLogRouter?.RecordSporeSalvoLaunches(events[0].PlayerId, events.Count);

            var activeBoard = gridVisualizer.ActiveBoard;
            var destinationTileIds = events
                .Select(e => e.DestinationTileId)
                .Where(tileId => tileId >= 0)
                .Distinct()
                .ToList();
            foreach (var tileId in destinationTileIds)
            {
                activeBoard?.GetTileById(tileId)?.FungalCell?.ClearToxinDropFlag();
            }

            int remainingAnimations = 0;
            foreach (var specialEvent in events)
            {
                remainingAnimations++;
                gridVisualizer.StartCoroutine(AwaitAnimation(
                    gridVisualizer.PlaySporeSalvoAnimation(
                        specialEvent.PlayerId,
                        specialEvent.SourceTileId,
                        specialEvent.DestinationTileId),
                    () => remainingAnimations--));
            }

            while (remainingAnimations > 0)
            {
                yield return null;
            }

            if (activeBoard != null)
            {
                gridVisualizer.RenderBoard(activeBoard, suppressAnimations: true);
            }
        }

        private static IEnumerator AwaitAnimation(IEnumerator animation, Action onComplete)
        {
            if (animation != null)
            {
                yield return animation;
            }

            onComplete?.Invoke();
        }

        private static string BuildMycotoxicLashBannerText(int cellsKilled)
        {
            string lashMessage = cellsKilled == 1
                ? "Mycotoxic Lash kills 1 cell!"
                : $"Mycotoxic Lash kills {cellsKilled} cells!";
            string colorHex = ColorUtility.ToHtmlStringRGB(UIStyleTokens.State.Danger);
            return $"Decay Phase Begins!\n<size=60%><color=#{colorHex}>{lashMessage}</color></size>";
        }

        private static string BuildSaprophageRingBannerText(int cellsConsumed)
        {
            return cellsConsumed == 1
                ? "Saprophage Ring consumes 1 dying cell!"
                : $"Saprophage Ring consumes {cellsConsumed} dying cells!";
        }

        private static string BuildMarginalClampBannerText(int cellsKilled)
        {
            return cellsKilled == 1
                ? "Marginal Clamp clears 1 border threat!"
                : $"Marginal Clamp clears {cellsKilled} border threats!";
        }

        private static string BuildVesicleBurstBannerText(int affectedTiles)
        {
            return affectedTiles == 1
                ? "Vesicle Burst toxifies 1 tile!"
                : $"Vesicle Burst toxifies {affectedTiles} tiles!";
        }

        private static string BuildSporeSalvoBannerText()
        {
            return "Spore Salvo launches toxins at enemy molds!";
        }

        private static string BuildHyphalBridgeBannerText(int bridgeCells)
        {
            return bridgeCells == 1
                ? "Hyphal Bridge roots 1 bridge cell!"
                : $"Hyphal Bridge roots {bridgeCells} bridge cells!";
        }
    }
}