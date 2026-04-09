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
            yield return CreateJettingMycelium(MycovariantIds.JettingMyceliumIId, "Jetting Mycelium I", isUniversal: true);
            yield return CreateJettingMycelium(MycovariantIds.JettingMyceliumIIId, "Jetting Mycelium II", isUniversal: false);
            yield return CreateJettingMycelium(MycovariantIds.JettingMyceliumIIIId, "Jetting Mycelium III", isUniversal: false);
        }

        private static Mycovariant CreateJettingMycelium(int mycovariantId, string name, bool isUniversal)
        {
            var livingLength = JettingMyceliumHelper.GetLivingLengthForMycovariant(mycovariantId);
            var maxToxinWidth = JettingMyceliumHelper.GetMaximumToxinWidthForMycovariant(mycovariantId);

            return new Mycovariant
            {
                Id = mycovariantId,
                Name = name,
                Description = $"One-time on draft: choose a living source cell, then aim a spore-jet in a cardinal direction: grow {livingLength} living tiles, then place a widening toxin fan up to {maxToxinWidth} tiles wide.",
                FlavorText = "The cap ruptures violently. The colony blasts outward in a widening cloud of toxic spores wherever the pilot aims.",
                Type = MycovariantType.Directional,
                Category = MycovariantCategory.Fungicide,
                IsUniversal = isUniversal,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    var shouldUseCoreLogic = player.PlayerType == PlayerTypeEnum.AI;
                    if (shouldUseCoreLogic)
                    {
                        var bestPlacement = JettingMyceliumHelper.FindBestPlacement(player, board, mycovariantId);
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
                    var bestPlacement = JettingMyceliumHelper.FindBestPlacement(player, board, mycovariantId);
                    if (bestPlacement == null) return 1f;
                    return JettingMyceliumHelper.ScoreToAIScore(bestPlacement.Value.score);
                }
            };
        }
    }
}
