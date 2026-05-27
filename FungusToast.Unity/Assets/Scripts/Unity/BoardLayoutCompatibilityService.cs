using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Persistence;
using FungusToast.Core.Players;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.Save;
using UnityEngine;

namespace FungusToast.Unity
{
    internal static class BoardLayoutCompatibilityService
    {
        private const string AppliedCompatibilityTokenKey = "System.BoardLayoutCompatibilityToken";
        private const string PendingNoticeTitleKey = "System.BoardLayoutCompatibilityNoticeTitle";
        private const string PendingNoticeBodyKey = "System.BoardLayoutCompatibilityNoticeBody";
        private const string CurrentCompatibilityToken = "board-layout-2026-05-27";
        private const string RestartNoticeTitle = "In-Progress Save Restarted";

        public static void ApplyIfNeeded()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            string appliedToken = ScopedPlayerPrefs.GetString(AppliedCompatibilityTokenKey, string.Empty);
            if (string.Equals(appliedToken, CurrentCompatibilityToken, StringComparison.Ordinal))
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

            ScopedPlayerPrefs.SetString(AppliedCompatibilityTokenKey, CurrentCompatibilityToken);
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
