using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FungusToast.Core.Persistence;
using FungusToast.Core.Players;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.Save;
using FungusToast.Unity.Grid;
using UnityEngine;

namespace FungusToast.Unity
{
    internal static class BoardLayoutCompatibilityService
    {
        private const string AppliedCompatibilityTokenKey = "System.BoardLayoutCompatibilityToken";
        private const string PendingNoticeTitleKey = "System.BoardLayoutCompatibilityNoticeTitle";
        private const string PendingNoticeBodyKey = "System.BoardLayoutCompatibilityNoticeBody";
        private const string CompatibilitySchemaVersion = "board-layout-signature-v1";
        private const string RestartNoticeTitle = "In-Progress Save Restarted";

        public static void ApplyIfNeeded(CampaignProgression campaignProgression, BoardMediumConfig defaultSoloBoardMedium)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            string currentCompatibilityToken = BuildCurrentCompatibilityToken(campaignProgression, defaultSoloBoardMedium);
            string appliedToken = ScopedPlayerPrefs.GetString(AppliedCompatibilityTokenKey, string.Empty);
            if (string.Equals(appliedToken, currentCompatibilityToken, StringComparison.Ordinal))
            {
                return;
            }

            bool invalidatedCampaignCheckpoint = InvalidatePersistedCampaignCheckpoint();
            bool invalidatedSoloSave = InvalidatePersistedSoloSave();
            if (invalidatedCampaignCheckpoint || invalidatedSoloSave)
            {
                QueueRestartNotice(BuildTokenMismatchNoticeBody(invalidatedCampaignCheckpoint, invalidatedSoloSave));
                Debug.Log("[BoardLayoutCompatibility] Cleared incompatible in-progress saves after board layout token change.");
            }

            ScopedPlayerPrefs.SetString(AppliedCompatibilityTokenKey, currentCompatibilityToken);
            ScopedPlayerPrefs.Save();
        }

        public static void RecoverCampaignCheckpoint(CampaignController campaignController, string failureReason)
        {
            bool invalidated = false;
            if (campaignController != null && campaignController.HasActiveRun)
            {
                campaignController.ClearGameplayCheckpoint();
                invalidated = true;
            }
            else
            {
                invalidated = InvalidatePersistedCampaignCheckpoint();
            }

            if (invalidated)
            {
                QueueRestartNotice(BuildRuntimeRecoveryNoticeBody("campaign", failureReason));
            }
        }

        public static void RecoverSoloSave(string failureReason)
        {
            if (!InvalidatePersistedSoloSave())
            {
                return;
            }

            QueueRestartNotice(BuildRuntimeRecoveryNoticeBody("hotseat", failureReason));
        }

        public static bool HasPendingRestartNotice()
        {
            return !string.IsNullOrWhiteSpace(ScopedPlayerPrefs.GetString(PendingNoticeBodyKey, string.Empty));
        }

        public static bool TryConsumePendingRestartNotice(out string title, out string body)
        {
            title = ScopedPlayerPrefs.GetString(PendingNoticeTitleKey, string.Empty);
            body = ScopedPlayerPrefs.GetString(PendingNoticeBodyKey, string.Empty);
            if (string.IsNullOrWhiteSpace(body))
            {
                title = string.Empty;
                return false;
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                title = RestartNoticeTitle;
            }

            ScopedPlayerPrefs.SetString(PendingNoticeTitleKey, string.Empty);
            ScopedPlayerPrefs.SetString(PendingNoticeBodyKey, string.Empty);
            ScopedPlayerPrefs.Save();
            return true;
        }

        public static bool TryValidateRuntimeSnapshot(RoundStartRuntimeSnapshot snapshot, out string failureReason)
        {
            failureReason = string.Empty;

            if (snapshot == null)
            {
                failureReason = "The saved checkpoint is missing.";
                return false;
            }

            if (snapshot.BoardWidth <= 0 || snapshot.BoardHeight <= 0)
            {
                failureReason = "The saved checkpoint has invalid board dimensions.";
                return false;
            }

            int playerCount = snapshot.Players?.Count ?? 0;
            if (playerCount <= 0)
            {
                failureReason = "The saved checkpoint has no players.";
                return false;
            }

            int totalTiles = snapshot.BoardWidth * snapshot.BoardHeight;
            if (totalTiles <= 0)
            {
                failureReason = "The saved checkpoint has no playable board area.";
                return false;
            }

            var seenPlayerIds = new HashSet<int>();
            bool hasHumanPlayer = false;
            foreach (PlayerRuntimeSnapshot playerSnapshot in snapshot.Players)
            {
                if (playerSnapshot == null)
                {
                    failureReason = "The saved checkpoint contains a missing player entry.";
                    return false;
                }

                if (!IsPlayerIdInRange(playerSnapshot.PlayerId, playerCount) || !seenPlayerIds.Add(playerSnapshot.PlayerId))
                {
                    failureReason = $"The saved checkpoint contains an invalid player id ({playerSnapshot.PlayerId}).";
                    return false;
                }

                if (playerSnapshot.PlayerType == PlayerTypeEnum.Human)
                {
                    hasHumanPlayer = true;
                }

                if (!ValidateTileIds(playerSnapshot.ControlledTileIds, totalTiles, "controlled tile", out failureReason))
                {
                    return false;
                }

                if (playerSnapshot.StartingTileId.HasValue && !IsTileIdInRange(playerSnapshot.StartingTileId.Value, totalTiles))
                {
                    failureReason = $"The saved checkpoint contains an invalid starting tile id ({playerSnapshot.StartingTileId.Value}).";
                    return false;
                }
            }

            if (!hasHumanPlayer)
            {
                failureReason = "The saved checkpoint no longer contains a human player.";
                return false;
            }

            if (!ValidateTileIds(snapshot.PermanentlyBlockedTileIds, totalTiles, "blocked tile", out failureReason))
            {
                return false;
            }

            if (!ValidateTileIds(snapshot.NutrientPatches?.Select(patch => patch != null ? patch.TileId : -1), totalTiles, "nutrient tile", out failureReason))
            {
                return false;
            }

            if (!ValidateTileIds(snapshot.Chemobeacons?.Select(marker => marker != null ? marker.TileId : -1), totalTiles, "chemobeacon tile", out failureReason))
            {
                return false;
            }

            foreach (ChemobeaconMarkerSnapshot marker in snapshot.Chemobeacons ?? Enumerable.Empty<ChemobeaconMarkerSnapshot>())
            {
                if (marker == null)
                {
                    failureReason = "The saved checkpoint contains a missing chemobeacon entry.";
                    return false;
                }

                if (!IsPlayerIdInRange(marker.PlayerId, playerCount))
                {
                    failureReason = $"The saved checkpoint contains an invalid chemobeacon player id ({marker.PlayerId}).";
                    return false;
                }
            }

            foreach (int pendingPlayerId in snapshot.PendingHypervariationDraftPlayerIds ?? Enumerable.Empty<int>())
            {
                if (!IsPlayerIdInRange(pendingPlayerId, playerCount))
                {
                    failureReason = $"The saved checkpoint contains an invalid pending draft player id ({pendingPlayerId}).";
                    return false;
                }
            }

            foreach (FungalCellSnapshot cellSnapshot in snapshot.Cells ?? Enumerable.Empty<FungalCellSnapshot>())
            {
                if (cellSnapshot == null)
                {
                    failureReason = "The saved checkpoint contains a missing cell entry.";
                    return false;
                }

                if (!IsTileIdInRange(cellSnapshot.TileId, totalTiles))
                {
                    failureReason = $"The saved checkpoint contains an invalid cell tile id ({cellSnapshot.TileId}).";
                    return false;
                }

                if (!IsPlayerIdInRange(cellSnapshot.OriginalOwnerPlayerId, playerCount))
                {
                    failureReason = $"The saved checkpoint contains an invalid original owner id ({cellSnapshot.OriginalOwnerPlayerId}).";
                    return false;
                }

                if (cellSnapshot.OwnerPlayerId.HasValue && !IsPlayerIdInRange(cellSnapshot.OwnerPlayerId.Value, playerCount))
                {
                    failureReason = $"The saved checkpoint contains an invalid owner id ({cellSnapshot.OwnerPlayerId.Value}).";
                    return false;
                }

                if (cellSnapshot.LastOwnerPlayerId.HasValue && !IsPlayerIdInRange(cellSnapshot.LastOwnerPlayerId.Value, playerCount))
                {
                    failureReason = $"The saved checkpoint contains an invalid last-owner id ({cellSnapshot.LastOwnerPlayerId.Value}).";
                    return false;
                }
            }

            return true;
        }

        private static bool InvalidatePersistedCampaignCheckpoint()
        {
            CampaignState state = CampaignSaveService.Load();
            if (state == null || !state.hasInLevelGameplayCheckpoint || state.inLevelRuntimeSnapshot == null)
            {
                return false;
            }

            state.hasInLevelGameplayCheckpoint = false;
            state.inLevelRuntimeSnapshot = null;
            state.inLevelRandomState = null;
            CampaignSaveService.Save(state);
            return true;
        }

        private static bool InvalidatePersistedSoloSave()
        {
            if (!SoloGameSaveService.Exists())
            {
                return false;
            }

            SoloGameSaveService.Delete();
            return true;
        }

        private static bool ValidateTileIds(IEnumerable<int> tileIds, int totalTiles, string label, out string failureReason)
        {
            failureReason = string.Empty;
            foreach (int tileId in tileIds ?? Enumerable.Empty<int>())
            {
                if (!IsTileIdInRange(tileId, totalTiles))
                {
                    failureReason = $"The saved checkpoint contains an invalid {label} id ({tileId}).";
                    return false;
                }
            }

            return true;
        }

        private static bool IsPlayerIdInRange(int playerId, int playerCount)
        {
            return playerId >= 0 && playerId < playerCount;
        }

        private static bool IsTileIdInRange(int tileId, int totalTiles)
        {
            return tileId >= 0 && tileId < totalTiles;
        }

        private static string BuildCurrentCompatibilityToken(CampaignProgression campaignProgression, BoardMediumConfig defaultSoloBoardMedium)
        {
            var signature = new StringBuilder(4096);
            signature.Append(CompatibilitySchemaVersion);
            AppendCampaignProgressionSignature(signature, campaignProgression);
            AppendBoardMediumSignature(signature, defaultSoloBoardMedium, "solo-default");
            return $"{CompatibilitySchemaVersion}:{Hash128.Compute(signature.ToString())}";
        }

        private static void AppendCampaignProgressionSignature(StringBuilder signature, CampaignProgression campaignProgression)
        {
            signature.Append("|campaign:");
            if (campaignProgression == null || campaignProgression.levels == null)
            {
                signature.Append("<null>");
                return;
            }

            signature.Append(campaignProgression.levels.Count);
            for (int i = 0; i < campaignProgression.levels.Count; i++)
            {
                CampaignProgression.LevelSpec level = campaignProgression.levels[i];
                signature.Append("|level:").Append(i);
                if (level == null)
                {
                    signature.Append(":<null>");
                    continue;
                }

                signature.Append(":title=").Append(level.levelTitle ?? string.Empty);
                signature.Append(":preset=");
                AppendBoardPresetSignature(signature, level.boardPreset);
                signature.Append(":bossPool=");
                AppendBoardPresetCollectionSignature(signature, level.bossBoardPresets);
            }
        }

        private static void AppendBoardPresetCollectionSignature(StringBuilder signature, IReadOnlyList<BoardPreset> presets)
        {
            if (presets == null)
            {
                signature.Append("<null>");
                return;
            }

            signature.Append(presets.Count);
            for (int i = 0; i < presets.Count; i++)
            {
                signature.Append("|poolPreset:").Append(i).Append('=');
                AppendBoardPresetSignature(signature, presets[i]);
            }
        }

        private static void AppendBoardPresetSignature(StringBuilder signature, BoardPreset preset)
        {
            if (preset == null)
            {
                signature.Append("<null>");
                return;
            }

            signature.Append(preset.presetId ?? string.Empty);
            signature.Append("@").Append(preset.boardWidth).Append("x").Append(preset.boardHeight);
            signature.Append(":humanStarts=");
            AppendVector2IntList(signature, preset.humanStartingCoordinatePool);
            signature.Append(":aiStarts=");
            if (preset.aiPlayers == null)
            {
                signature.Append("<null>");
            }
            else
            {
                signature.Append(preset.aiPlayers.Count);
                for (int i = 0; i < preset.aiPlayers.Count; i++)
                {
                    BoardPreset.AIPlayerSpec aiPlayer = preset.aiPlayers[i];
                    signature.Append("|ai:").Append(i).Append('=');
                    if (aiPlayer == null)
                    {
                        signature.Append("<null>");
                        continue;
                    }

                    signature.Append(aiPlayer.strategyName ?? string.Empty);
                    signature.Append('@');
                    if (aiPlayer.startingCoordinate.HasValue)
                    {
                        Vector2Int coordinate = aiPlayer.startingCoordinate.Value;
                        signature.Append(coordinate.x).Append(',').Append(coordinate.y);
                    }
                    else
                    {
                        signature.Append("<auto>");
                    }
                }
            }

            signature.Append(":medium=");
            AppendBoardMediumSignature(signature, preset.boardMedium, preset.presetId ?? "preset");
        }

        private static void AppendBoardMediumSignature(StringBuilder signature, BoardMediumConfig medium, string label)
        {
            signature.Append("|medium:").Append(label).Append('=');
            if (medium == null)
            {
                signature.Append("<null>");
                return;
            }

            signature.Append(medium.mediumId ?? string.Empty);
            signature.Append(":defaultSizeBands=");
            AppendBoardBackgroundSettingsSignature(
                signature,
                medium.renderBoardBackground,
                medium.backgroundSprite,
                medium.backgroundColor,
                medium.hidePlayableSurfaceTiles,
                medium.deriveBlockedTilesFromBackgroundAlpha,
                medium.backgroundAlphaPlayableThreshold,
                medium.backgroundMinTileCoverage,
                medium.backgroundMaxTileClipFraction,
                medium.backgroundTileClipSampleResolution,
                medium.useExplicitBlockedTileIds,
                medium.explicitBlockedTileIds,
                medium.backgroundInsetLeftNormalized,
                medium.backgroundInsetRightNormalized,
                medium.backgroundInsetBottomNormalized,
                medium.backgroundInsetTopNormalized,
                medium.composeSafeAreaWithBoardBoundsMetadata,
                medium.backgroundScaleMultiplier,
                medium.renderPlayableAreaOverlay,
                medium.playableAreaOverlayColor,
                medium.renderBoardEdgeFade,
                medium.boardEdgeFadeColor,
                medium.boardEdgeFadeWidthTiles,
                medium.boardEdgeFadeNoiseStrength);

            signature.Append(":overrides=");
            if (medium.boardBackgroundOverrides == null)
            {
                signature.Append("<null>");
            }
            else
            {
                signature.Append(medium.boardBackgroundOverrides.Count);
                for (int i = 0; i < medium.boardBackgroundOverrides.Count; i++)
                {
                    BoardMediumConfig.BoardBackgroundSizeOverride backgroundOverride = medium.boardBackgroundOverrides[i];
                    signature.Append("|override:").Append(i).Append('=');
                    if (backgroundOverride == null)
                    {
                        signature.Append("<null>");
                        continue;
                    }

                    signature.Append(backgroundOverride.minBoardWidth).Append('x').Append(backgroundOverride.minBoardHeight);
                    signature.Append('-').Append(backgroundOverride.maxBoardWidth).Append('x').Append(backgroundOverride.maxBoardHeight);
                    AppendBoardBackgroundSettingsSignature(
                        signature,
                        backgroundOverride.renderBoardBackground,
                        backgroundOverride.backgroundSprite,
                        backgroundOverride.backgroundColor,
                        backgroundOverride.hidePlayableSurfaceTiles,
                        backgroundOverride.deriveBlockedTilesFromBackgroundAlpha,
                        backgroundOverride.backgroundAlphaPlayableThreshold,
                        backgroundOverride.backgroundMinTileCoverage,
                        backgroundOverride.backgroundMaxTileClipFraction,
                        backgroundOverride.backgroundTileClipSampleResolution,
                        backgroundOverride.useExplicitBlockedTileIds,
                        backgroundOverride.explicitBlockedTileIds,
                        backgroundOverride.backgroundInsetLeftNormalized,
                        backgroundOverride.backgroundInsetRightNormalized,
                        backgroundOverride.backgroundInsetBottomNormalized,
                        backgroundOverride.backgroundInsetTopNormalized,
                        backgroundOverride.composeSafeAreaWithBoardBoundsMetadata,
                        backgroundOverride.backgroundScaleMultiplier,
                        backgroundOverride.renderPlayableAreaOverlay,
                        backgroundOverride.playableAreaOverlayColor,
                        backgroundOverride.renderBoardEdgeFade,
                        backgroundOverride.boardEdgeFadeColor,
                        backgroundOverride.boardEdgeFadeWidthTiles,
                        backgroundOverride.boardEdgeFadeNoiseStrength);
                }
            }

            signature.Append(":metadata=");
            if (medium.boardBackgroundSpriteMetadata == null)
            {
                signature.Append("<null>");
                return;
            }

            signature.Append(medium.boardBackgroundSpriteMetadata.Count);
            for (int i = 0; i < medium.boardBackgroundSpriteMetadata.Count; i++)
            {
                BoardMediumConfig.BoardBackgroundSpriteMetadata metadata = medium.boardBackgroundSpriteMetadata[i];
                signature.Append("|metadata:").Append(i).Append('=');
                if (metadata == null)
                {
                    signature.Append("<null>");
                    continue;
                }

                signature.Append(GetSpriteStableId(metadata.backgroundSprite));
                signature.Append(":visible=").Append(metadata.hasVisibleAlphaBounds);
                AppendRect(signature, metadata.visibleAlphaBoundsNormalized);
                signature.Append(":board=").Append(metadata.hasBoardBounds);
                AppendRect(signature, metadata.boardBoundsNormalized);
                signature.Append(":ellipse=").Append(metadata.hasPlayableEllipse);
                AppendVector2(signature, metadata.playableEllipseCenterNormalized);
                AppendVector2(signature, metadata.playableEllipseRadiiNormalized);
                signature.Append(":span=").Append(metadata.hasPlayableHorizontalSpanProfile);
                signature.Append('@').Append(metadata.playableHorizontalSpanProfileMinYNormalized);
                signature.Append(',').Append(metadata.playableHorizontalSpanProfileMaxYNormalized);
                AppendPlayableHorizontalSpanProfile(signature, metadata.playableHorizontalSpanProfile);
                AppendBakedBlockedTileMasks(signature, metadata.bakedBlockedTileMasks);
            }
        }

        private static void AppendBoardBackgroundSettingsSignature(
            StringBuilder signature,
            bool renderBoardBackground,
            Sprite backgroundSprite,
            Color backgroundColor,
            bool hidePlayableSurfaceTiles,
            bool deriveBlockedTilesFromBackgroundAlpha,
            float backgroundAlphaPlayableThreshold,
            float backgroundMinTileCoverage,
            float backgroundMaxTileClipFraction,
            int backgroundTileClipSampleResolution,
            bool useExplicitBlockedTileIds,
            IReadOnlyList<int> explicitBlockedTileIds,
            float backgroundInsetLeftNormalized,
            float backgroundInsetRightNormalized,
            float backgroundInsetBottomNormalized,
            float backgroundInsetTopNormalized,
            bool composeSafeAreaWithBoardBoundsMetadata,
            float backgroundScaleMultiplier,
            bool renderPlayableAreaOverlay,
            Color playableAreaOverlayColor,
            bool renderBoardEdgeFade,
            Color boardEdgeFadeColor,
            float boardEdgeFadeWidthTiles,
            float boardEdgeFadeNoiseStrength)
        {
            signature.Append(":render=").Append(renderBoardBackground);
            signature.Append(":sprite=").Append(GetSpriteStableId(backgroundSprite));
            AppendColor(signature, backgroundColor);
            signature.Append(":hide=").Append(hidePlayableSurfaceTiles);
            signature.Append(":alphaMask=").Append(deriveBlockedTilesFromBackgroundAlpha);
            signature.Append(':').Append(backgroundAlphaPlayableThreshold);
            signature.Append(':').Append(backgroundMinTileCoverage);
            signature.Append(':').Append(backgroundMaxTileClipFraction);
            signature.Append(':').Append(backgroundTileClipSampleResolution);
            signature.Append(":explicit=").Append(useExplicitBlockedTileIds);
            AppendIntList(signature, explicitBlockedTileIds);
            signature.Append(":insets=");
            signature.Append(backgroundInsetLeftNormalized).Append(',');
            signature.Append(backgroundInsetRightNormalized).Append(',');
            signature.Append(backgroundInsetBottomNormalized).Append(',');
            signature.Append(backgroundInsetTopNormalized);
            signature.Append(":compose=").Append(composeSafeAreaWithBoardBoundsMetadata);
            signature.Append(":scale=").Append(backgroundScaleMultiplier);
            signature.Append(":overlay=").Append(renderPlayableAreaOverlay);
            AppendColor(signature, playableAreaOverlayColor);
            signature.Append(":edgeFade=").Append(renderBoardEdgeFade);
            AppendColor(signature, boardEdgeFadeColor);
            signature.Append(':').Append(boardEdgeFadeWidthTiles);
            signature.Append(':').Append(boardEdgeFadeNoiseStrength);
        }

        private static void AppendVector2IntList(StringBuilder signature, IReadOnlyList<Vector2Int> coordinates)
        {
            if (coordinates == null)
            {
                signature.Append("<null>");
                return;
            }

            signature.Append(coordinates.Count);
            for (int i = 0; i < coordinates.Count; i++)
            {
                Vector2Int coordinate = coordinates[i];
                signature.Append('|').Append(coordinate.x).Append(',').Append(coordinate.y);
            }
        }

        private static void AppendPlayableHorizontalSpanProfile(StringBuilder signature, IReadOnlyList<BoardMediumConfig.PlayableHorizontalSpanStop> stops)
        {
            signature.Append(":stops=");
            if (stops == null)
            {
                signature.Append("<null>");
                return;
            }

            signature.Append(stops.Count);
            for (int i = 0; i < stops.Count; i++)
            {
                BoardMediumConfig.PlayableHorizontalSpanStop stop = stops[i];
                signature.Append('|');
                if (stop == null)
                {
                    signature.Append("<null>");
                    continue;
                }

                signature.Append(stop.normalizedY).Append(',');
                signature.Append(stop.minXNormalized).Append(',');
                signature.Append(stop.maxXNormalized);
            }
        }

        private static void AppendBakedBlockedTileMasks(StringBuilder signature, IReadOnlyList<BoardMediumConfig.BakedBlockedTileMask> masks)
        {
            signature.Append(":masks=");
            if (masks == null)
            {
                signature.Append("<null>");
                return;
            }

            signature.Append(masks.Count);
            for (int i = 0; i < masks.Count; i++)
            {
                BoardMediumConfig.BakedBlockedTileMask mask = masks[i];
                signature.Append('|');
                if (mask == null)
                {
                    signature.Append("<null>");
                    continue;
                }

                signature.Append(mask.boardWidth).Append('x').Append(mask.boardHeight);
                signature.Append(':').Append(mask.bakeVersion ?? string.Empty);
                signature.Append(':').Append(mask.spriteContentHash ?? string.Empty);
                AppendIntList(signature, mask.blockedTileIds);
            }
        }

        private static void AppendIntList(StringBuilder signature, IReadOnlyList<int> values)
        {
            if (values == null)
            {
                signature.Append("<null>");
                return;
            }

            signature.Append('[').Append(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                signature.Append('|').Append(values[i]);
            }

            signature.Append(']');
        }

        private static void AppendRect(StringBuilder signature, Rect rect)
        {
            signature.Append('@').Append(rect.x);
            signature.Append(',').Append(rect.y);
            signature.Append(',').Append(rect.width);
            signature.Append(',').Append(rect.height);
        }

        private static void AppendVector2(StringBuilder signature, Vector2 value)
        {
            signature.Append('@').Append(value.x).Append(',').Append(value.y);
        }

        private static void AppendColor(StringBuilder signature, Color color)
        {
            signature.Append('@').Append(color.r);
            signature.Append(',').Append(color.g);
            signature.Append(',').Append(color.b);
            signature.Append(',').Append(color.a);
        }

        private static string GetSpriteStableId(Sprite sprite)
        {
            if (sprite == null)
            {
                return "<null>";
            }

            Texture2D texture = sprite.texture;
            string textureName = texture != null ? texture.name : string.Empty;
            Rect textureRect = sprite.textureRect;
            return $"{textureName}/{sprite.name}@{textureRect.x},{textureRect.y},{textureRect.width},{textureRect.height}";
        }

        private static void QueueRestartNotice(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            ScopedPlayerPrefs.SetString(PendingNoticeTitleKey, RestartNoticeTitle);
            ScopedPlayerPrefs.SetString(PendingNoticeBodyKey, body);
            ScopedPlayerPrefs.Save();
        }

        private static string BuildTokenMismatchNoticeBody(bool invalidatedCampaignCheckpoint, bool invalidatedSoloSave)
        {
            if (invalidatedCampaignCheckpoint && invalidatedSoloSave)
            {
                return "A recent update changed board layout data. To avoid a crash, Fungus Toast cleared your in-progress campaign checkpoint and your saved hotseat game. Campaign progression and permanent unlocks were kept when possible.";
            }

            if (invalidatedCampaignCheckpoint)
            {
                return "A recent update changed board layout data. To avoid a crash, Fungus Toast cleared your in-progress campaign checkpoint and will restart that level from a safe state. Campaign progression and permanent unlocks were kept.";
            }

            return "A recent update changed board layout data. To avoid a crash, Fungus Toast cleared your saved hotseat game before resuming.";
        }

        private static string BuildRuntimeRecoveryNoticeBody(string saveKind, string failureReason)
        {
            if (string.Equals(saveKind, "campaign", StringComparison.OrdinalIgnoreCase))
            {
                return $"Fungus Toast detected that your in-progress campaign checkpoint could not be resumed safely and cleared that checkpoint to avoid a crash. The current level will restart from the beginning. Details: {failureReason}";
            }

            return $"Fungus Toast detected that your saved hotseat game could not be resumed safely and cleared that save to avoid a crash. Details: {failureReason}";
        }
    }
}
