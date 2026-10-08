using System;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Campaign
{
    /// <summary>Equal-weight per-stage selection, independent of gameplay RNG and legacy boss selection.</summary>
    public static class CampaignLevelVariantSelection
    {
        private const int VariantSeedSalt = 0x56415249;

        public static int PickVariantIndex(int runSeed, int levelIndex, int variantCount)
        {
            if (levelIndex < 0) throw new ArgumentOutOfRangeException(nameof(levelIndex));
            if (variantCount <= 0) throw new ArgumentOutOfRangeException(nameof(variantCount));
            int seed = unchecked(CampaignRunDeterminism.GetBossPoolSeed(runSeed, levelIndex) ^ VariantSeedSalt);
            return new Random(seed).Next(variantCount);
        }

        /// <summary>Persisted IDs beat seed selection; old saves retain their existing preset.</summary>
        public static int ResolveVariantIndex(int runSeed, int levelIndex,
            IReadOnlyList<string> variantIds, IReadOnlyList<string> presetIds,
            string? savedVariantId = null, string? savedPresetId = null)
        {
            if (variantIds == null) throw new ArgumentNullException(nameof(variantIds));
            if (presetIds == null) throw new ArgumentNullException(nameof(presetIds));
            if (variantIds.Count == 0 || variantIds.Count != presetIds.Count)
                throw new ArgumentException("Variant and preset lists must be nonempty and the same length.");
            if (variantIds.Any(string.IsNullOrWhiteSpace) || presetIds.Any(string.IsNullOrWhiteSpace)
                || variantIds.Distinct(StringComparer.Ordinal).Count() != variantIds.Count
                || presetIds.Distinct(StringComparer.Ordinal).Count() != presetIds.Count)
                throw new ArgumentException("Each variant requires a unique nonempty variant ID and preset ID.");
            int Find(IReadOnlyList<string> ids, string value)
            {
                for (int i = 0; i < ids.Count; i++) if (StringComparer.Ordinal.Equals(ids[i], value)) return i;
                throw new InvalidOperationException($"Saved campaign identity '{value}' is absent from this stage; do not reroll an active save.");
            }
            if (!string.IsNullOrEmpty(savedVariantId))
            {
                int index = Find(variantIds, savedVariantId!);
                if (!string.IsNullOrEmpty(savedPresetId) && !StringComparer.Ordinal.Equals(presetIds[index], savedPresetId))
                    throw new InvalidOperationException("Saved campaign variant and board preset identities disagree.");
                return index;
            }
            if (!string.IsNullOrEmpty(savedPresetId)) return Find(presetIds, savedPresetId!);
            return PickVariantIndex(runSeed, levelIndex, variantIds.Count);
        }
    }
}
