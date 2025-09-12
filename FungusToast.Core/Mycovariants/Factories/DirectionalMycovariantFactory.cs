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
            yield return CreateJettingMycelium("North", MycovariantIds.JettingMyceliumNorthId, CardinalDirection.North);
            yield return CreateJettingMycelium("East", MycovariantIds.JettingMyceliumEastId, CardinalDirection.East);
            yield return CreateJettingMycelium("South", MycovariantIds.JettingMyceliumSouthId, CardinalDirection.South);
            yield return CreateJettingMycelium("West", MycovariantIds.JettingMyceliumWestId, CardinalDirection.West);
        }

        private static Mycovariant CreateJettingMycelium(string directionLabel, int id, CardinalDirection cardinalDirection)
        {
            return new Mycovariant
            {
                Id = id,
                Name = $"Jetting Mycelium ({directionLabel})",
                Description = $"Immediately grow {Config.MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles} mold tiles {directionLabel.ToLower()} from a chosen cell, followed by a spreading cone of toxins that starts {Config.MycovariantGameBalance.JettingMyceliumConeNarrowWidth} tile wide and expands to {Config.MycovariantGameBalance.JettingMyceliumConeWideWidth} tiles wide.",
                FlavorText = $"The cap ruptures violently. The colony explodes {directionLabel.ToLower()}ward in a widening cloud of toxic spores.",
                Type = MycovariantType.Directional,
                Category = MycovariantCategory.Fungicide,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    bool shouldUseCoreLogic = player.PlayerType == PlayerTypeEnum.AI;
                    if (shouldUseCoreLogic)
                    {
                        var livingCells = board.GetAllCellsOwnedBy(player.PlayerId)
                            .Where(c => c.IsAlive)
                            .ToList();
                        if (livingCells.Count > 0)
                        {
                            var sourceCell = livingCells[rng.Next(livingCells.Count)];
                            MycovariantEffectProcessor.ResolveJettingMycelium(
                                playerMyco, player, board, sourceCell.TileId, cardinalDirection, rng, observer);
                        }
                    }
                },
                AIScore = (player, board) =>
                {
                    var bestPlacement = JettingMyceliumHelper.FindBestPlacement(player, board, cardinalDirection);
                    if (bestPlacement == null) return 1f;
                    return JettingMyceliumHelper.ScoreToAIScore(bestPlacement.Value.score);
                }
            };
        }
    }
}
