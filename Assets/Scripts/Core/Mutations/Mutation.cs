using FungusToast.Core;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations
{
    public class Mutation
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public MutationType Type { get; private set; }
        public float EffectPerLevel { get; private set; }
        public int PointsPerUpgrade { get; private set; }
        public int MaxLevel { get; private set; }
        public int CurrentLevel { get; set; }
        public List<Mutation> Children { get; private set; }

        public Mutation(string name, string description, MutationType type, float effectPerLevel, int pointsPerUpgrade = 1, int maxLevel = 50)
        {
            Name = name;
            Description = description;
            Type = type;
            EffectPerLevel = effectPerLevel;
            PointsPerUpgrade = pointsPerUpgrade;
            MaxLevel = maxLevel;
            CurrentLevel = 0;
            Children = new List<Mutation>();
        }

        public bool CanUpgrade() => CurrentLevel < MaxLevel;

        public float GetTotalEffect() => CurrentLevel * EffectPerLevel;
    }

}
