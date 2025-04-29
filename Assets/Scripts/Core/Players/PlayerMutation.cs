public class PlayerMutation
{
    public int PlayerId { get; private set; }
    public int MutationId { get; private set; }
    public int CurrentLevel { get; private set; }

    public PlayerMutation(int playerId, int mutationId)
    {
        PlayerId = playerId;
        MutationId = mutationId;
        CurrentLevel = 1; // Starting at level 1 when first acquired
    }

    public void Upgrade()
    {
        CurrentLevel++;
    }
}
