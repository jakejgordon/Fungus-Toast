using FungusToast.Core.Players;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;

namespace FungusToast.Core.Death
{
    public static class SporeDropEngine
    {
        public static void ExecuteSporeDrop(GameBoard board, Player player, int currentCycle)
        {
            int level = player.GetMutationLevel(MutationIds.SporocidalBloom);
            if (level <= 0)
                return;

            int livingCells = player.ControlledTileIds.Count;
            int sporesToDrop = level * (int)Math.Floor(Math.Log(livingCells + 1, 2));

            var rng = new Random(player.PlayerId + currentCycle); // deterministic randomness per player per cycle
            var allTileIds = board.GetAllTileIds();

            for (int i = 0; i < sporesToDrop; i++)
            {
                int targetIndex = rng.Next(allTileIds.Count);
                int targetId = allTileIds[targetIndex];

                var cell = board.GetCell(targetId);
                if (cell == null)
                    continue;

                bool isEnemy = cell.OwnerPlayerId != player.PlayerId;

                if (cell.IsAlive && isEnemy)
                {
                    // Kill and toxify
                    cell.Kill(DeathReason.SporocidalBloom);
                    board.PlaceToxinTile(targetId, player.PlayerId, currentCycle + GameBalance.ToxinTileDuration);
                }
                else if (!cell.IsAlive && isEnemy)
                {
                    board.PlaceToxinTile(targetId, player.PlayerId, currentCycle + GameBalance.ToxinTileDuration);
                }
            }
        }
    }
}
