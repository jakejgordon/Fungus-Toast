﻿using FungusToast.Core;
using System.Collections.Generic;

namespace FungusToast.Core.Mutations
{
    public class Mutation
    {
        public int Id { get; private set; } // ✅ Unique identifier per mutation
        public string Name { get; private set; }
        public string Description { get; private set; }
        public MutationType Type { get; private set; }
        public float EffectPerLevel { get; private set; }
        public int PointsPerUpgrade { get; private set; }
        public int MaxLevel { get; private set; }
        public MutationCategory Category { get; private set; }

        public List<MutationPrerequisite> Prerequisites { get; private set; }
        public List<Mutation> Children { get; private set; }

        public Mutation(
            int id,
            string name,
            string description,
            MutationType type,
            float effectPerLevel,
            int pointsPerUpgrade = 1,
            int maxLevel = 50,
            MutationCategory category = MutationCategory.Growth)
        {
            Id = id;
            Name = name;
            Description = description;
            Type = type;
            EffectPerLevel = effectPerLevel;
            PointsPerUpgrade = pointsPerUpgrade;
            MaxLevel = maxLevel;
            Category = category;

            Prerequisites = new List<MutationPrerequisite>();
            Children = new List<Mutation>();
        }

        /// <summary>
        /// Calculates whether the mutation can be upgraded based on current level.
        /// </summary>
        public bool CanUpgrade(int currentLevel) => currentLevel < MaxLevel;

        /// <summary>
        /// Returns the effect for the given level.
        /// </summary>
        public float GetTotalEffect(int level) => level * EffectPerLevel;
    }
}
