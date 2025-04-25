namespace FungusToast.Core
{
    public class PlayerData
    {
        public int Id { get; }
        public string Name { get; set; }
        public int MutationPoints { get; set; }

        public float GrowthChance { get; set; } = 0.2f;
        public bool CanReleaseSpores { get; set; } = false;
        public bool CanReclaimDeadTiles { get; set; } = false;

        public PlayerData(int id, string name)
        {
            Id = id;
            Name = name;
            MutationPoints = 0;
        }
    }
}
