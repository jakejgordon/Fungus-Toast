using System;

namespace FungusToast.Core.Campaign
{
    public static class CampaignRunDeterminism
    {
        public static int GetBossPoolSeed(int runSeed, int levelIndex)
        {
            return unchecked((runSeed * 397) ^ levelIndex);
        }

        public static int PickBossPresetIndex(int runSeed, int levelIndex, int bossCount)
        {
            if (bossCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bossCount), "Boss count must be positive.");
            }

            return new Random(GetBossPoolSeed(runSeed, levelIndex)).Next(bossCount);
        }

        public static int GetPooledAiSelectionSeed(int runSeed, int levelIndex, string presetId)
        {
            return unchecked(GetBossPoolSeed(runSeed, levelIndex) ^ GetStableStringHash(presetId));
        }

        public static int GetStableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;
                foreach (char c in value)
                {
                    hash = (hash * 31) + c;
                }

                return hash;
            }
        }
    }
}
