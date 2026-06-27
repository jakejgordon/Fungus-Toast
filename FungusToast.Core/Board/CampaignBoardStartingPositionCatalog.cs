using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FungusToast.Core.Board
{
    public sealed class CampaignBoardStartingPositionEntry
    {
        public CampaignBoardStartingPositionEntry(
            int slotIndex,
            int x,
            int y,
            int favorRank,
            double winPercentage)
        {
            SlotIndex = slotIndex;
            X = x;
            Y = y;
            FavorRank = favorRank;
            WinPercentage = winPercentage;
        }

        public int SlotIndex { get; }
        public int X { get; }
        public int Y { get; }
        public int FavorRank { get; }
        public double WinPercentage { get; }
    }

    public sealed class CampaignBoardStartingPositionMetadata
    {
        public CampaignBoardStartingPositionMetadata(
            string presetId,
            string shapeKey,
            int boardWidth,
            int boardHeight,
            int playerCount,
            string spriteName,
            string shapeSource,
            IReadOnlyList<CampaignBoardStartingPositionEntry> entries)
        {
            PresetId = presetId ?? throw new ArgumentNullException(nameof(presetId));
            ShapeKey = shapeKey ?? throw new ArgumentNullException(nameof(shapeKey));
            BoardWidth = boardWidth;
            BoardHeight = boardHeight;
            PlayerCount = playerCount;
            SpriteName = spriteName ?? string.Empty;
            ShapeSource = shapeSource ?? string.Empty;
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }

        public string PresetId { get; }
        public string ShapeKey { get; }
        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public int PlayerCount { get; }
        public string SpriteName { get; }
        public string ShapeSource { get; }
        public IReadOnlyList<CampaignBoardStartingPositionEntry> Entries { get; }
    }

    public static partial class CampaignBoardStartingPositionCatalog
    {
        public static string ComputeShapeKey(int boardWidth, int boardHeight, IEnumerable<int> blockedTileIds)
        {
            if (boardWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(boardWidth));
            }

            if (boardHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(boardHeight));
            }

            var orderedTileIds = (blockedTileIds ?? Array.Empty<int>())
                .Distinct()
                .OrderBy(tileId => tileId)
                .ToArray();

            var builder = new StringBuilder();
            builder.Append(boardWidth);
            builder.Append('x');
            builder.Append(boardHeight);
            builder.Append('|');
            for (int i = 0; i < orderedTileIds.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(orderedTileIds[i]);
            }

            byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(bytes);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        public static bool TryGetMetadata(string presetId, int playerCount, out CampaignBoardStartingPositionMetadata metadata)
        {
            metadata = null!;
            if (string.IsNullOrWhiteSpace(presetId) || playerCount <= 0)
            {
                return false;
            }

            return MetadataByPresetAndPlayerCount.TryGetValue((presetId.Trim(), playerCount), out metadata);
        }

        public static bool TryGetMetadataByShapeKey(string shapeKey, int playerCount, out CampaignBoardStartingPositionMetadata metadata)
        {
            metadata = null!;
            if (string.IsNullOrWhiteSpace(shapeKey) || playerCount <= 0)
            {
                return false;
            }

            metadata = MetadataByPresetAndPlayerCount.Values.FirstOrDefault(entry =>
                entry.PlayerCount == playerCount
                && string.Equals(entry.ShapeKey, shapeKey.Trim(), StringComparison.OrdinalIgnoreCase));
            return metadata != null;
        }
    }
}
