namespace FungusToast.Core
{
    using System.Collections.Generic;

    public class Mutation
    {
        public string Name;
        public int CurrentLevel;
        public int MaxLevel;
        public int PointsPerUpgrade;
        public List<Mutation> Children = new List<Mutation>();

        public Mutation(string name, int maxLevel = 5, int pointsPerUpgrade = 1)
        {
            Name = name;
            MaxLevel = maxLevel;
            PointsPerUpgrade = pointsPerUpgrade;
            CurrentLevel = 0;
        }

        public bool CanUpgrade()
        {
            return CurrentLevel < MaxLevel;
        }

        public bool IsUnlocked()
        {
            // For now, always true — later can depend on parent mutation level
            return true;
        }
    }
}
