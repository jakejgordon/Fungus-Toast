namespace FungusToast.Core
{
    public class Mutation
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public int CurrentLevel { get; private set; }
        public int MaxLevel { get; private set; }
        public int CostPerLevel { get; private set; }

        public Mutation(string name, string description, int maxLevel, int costPerLevel)
        {
            Name = name;
            Description = description;
            MaxLevel = maxLevel;
            CostPerLevel = costPerLevel;
            CurrentLevel = 0;
        }

        public bool CanUpgrade()
        {
            return CurrentLevel < MaxLevel;
        }

        public void Upgrade()
        {
            if (CanUpgrade())
            {
                CurrentLevel++;
            }
        }
    }
}
