using System.Collections.Generic;
using FungusToast.Core.Board;
using System.Linq;
using FungusToast.Core.Players;

namespace FungusToast.Core.Mycovariants
{
    internal static class DirectionalMycovariantFactory
    {
        public static IEnumerable<Mycovariant> CreateAll()
        {
            yield return CreateJettingMycelium();
        }

        private static Mycovariant CreateJettingMycelium()
        {
            return new Mycovariant
            {
                Id = MycovariantIds.JettingMyceliumId,
                Name = "Jetting Mycelium",
                Description = $"One-time on draft: choose a living source cell, then aim a spore-jet in a cardinal direction: grow {Config.MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles} living tiles, then place a toxin cone widening from {Config.MycovariantGameBalance.JettingMyceliumConeNarrowWidth} to {Config.MycovariantGameBalance.JettingMyceliumConeWideWidth} tiles.",
                FlavorText = "The cap ruptures violently. The colony blasts outward in a widening cloud of toxic spores wherever the pilot aims.",
                Type = MycovariantType.Directional,
                Category = MycovariantCategory.Fungicide,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    bool shouldUseCoreLogic = player.PlayerType == PlayerTypeEnum.AI;
                    if (shouldUseCoreLogic)
                    {
                        var bestPlacement = JettingMyceliumHelper.FindBestPlacement(player, board);
                        if (bestPlacement != null)
                        {
                            var placement = bestPlacement.Value;
                            MycovariantEffectProcessor.ResolveJettingMycelium(
                                playerMyco, player, board, placement.sourceCell.TileId, placement.direction, rng, observer);
                        }
                    }
                },
                AIScore = (player, board) =>
                {
                    var bestPlacement = JettingMyceliumHelper.FindBestPlacement(player, board);
                    if (bestPlacement == null) return 1f;
                    return JettingMyceliumHelper.ScoreToAIScore(bestPlacement.Value.score);
                }
            };
        }
    }
}
