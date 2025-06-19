using FungusToast.Core.Board;
using FungusToast.Core.Players;

public static class MycovariantEffectProcessor
{
    public static void CheckAndTrigger(Player player, GameBoard board)
    {
        foreach (var playerMyco in player.Mycovariants)
        {
            if (!playerMyco.HasTriggered &&
                playerMyco.Mycovariant.IsTriggerConditionMet?.Invoke(playerMyco, board) == true)
            {
                playerMyco.Mycovariant.ApplyEffect?.Invoke(playerMyco, board, new Random());
                playerMyco.MarkTriggered();
            }
        }
    }
}
