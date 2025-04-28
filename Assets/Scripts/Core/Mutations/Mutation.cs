using FungusToast.Core;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations
{
    public class Mutation
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public MutationType Type { get; private set; }
        public float BaseEffectValue { get; private set; }
        public float EffectGrowthPerLevel { get; private set; }
        public List<Mutation> Children { get; private set; }
        public int CurrentLevel { get; set; }
        public int PointsPerUpgrade { get; private set; }
        public int MaxLevel { get; private set; }

        public Mutation(string name, string description, MutationType type, float baseEffectValue, float effectGrowthPerLevel, int pointsPerUpgrade = 1, int maxLevel = 5)
        {
            Name = name;
            Description = description;
            Type = type;
            BaseEffectValue = baseEffectValue;
            EffectGrowthPerLevel = effectGrowthPerLevel;
            PointsPerUpgrade = pointsPerUpgrade;
            MaxLevel = maxLevel;
            Children = new List<Mutation>();
            CurrentLevel = 0;
        }

        public bool CanUpgrade()
        {
            return CurrentLevel < MaxLevel;
        }

        public float GetTotalEffect()
        {
            return BaseEffectValue + (CurrentLevel * EffectGrowthPerLevel);
        }
    }
}
