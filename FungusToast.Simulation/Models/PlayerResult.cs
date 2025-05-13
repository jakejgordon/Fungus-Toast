namespace FungusToast.Simulation.GameSimulation.Models
{
    public class PlayerResult
    {
        public int PlayerId { get; set; }

        // Set to empty string to satisfy nullability
        public string StrategyName { get; set; } = string.Empty;

        public int LivingCells { get; set; }
        public int DeadCells { get; set; }

        // Set to empty dictionary
        public Dictionary<int, int> MutationLevels { get; set; } = new();
    }
}
