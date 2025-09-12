using System.Collections.Generic;
using System;
using System.Linq;
using FungusToast.Core.Config;

namespace FungusToast.Core.Mycovariants
{
    internal static class ReclamationMycovariantFactory
    {
        public static IEnumerable<Mycovariant> CreateAll()
        {
            yield return NecrophoricAdaptation();
            yield return ReclamationRhizomorphs();
        }

        private static Mycovariant NecrophoricAdaptation() =>
            new Mycovariant
            {
                Id = MycovariantIds.NecrophoricAdaptation,
                Name = "Necrophoric Adaptation",
                Description = $"When a mold cell dies, there is a {MycovariantGameBalance.NecrophoricAdaptationReclamationChance * 100f:0}% chance to reclaim an orthogonally adjacent dead tile.",
                FlavorText = "Even in death, the colony endures.",
                Type = MycovariantType.Passive,
                Category = MycovariantCategory.Reclamation,
                IsUniversal = false,
                AutoMarkTriggered = true,
                SynergyWith = MycovariantSynergyListFactory.GetReclamationSynergyMycovariantIdsExcluding(MycovariantIds.NecrophoricAdaptation),
                AIScore = (player, board) => {
                    int livingCells = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => c.IsAlive);
                    float baseScore = Math.Min(6f, Math.Max(1f, 1f + (livingCells * 5f / 50f)));
                    return baseScore; // Synergy auto-applied by base class
                }
            };

        private static Mycovariant ReclamationRhizomorphs() =>
            new Mycovariant
            {
                Id = MycovariantIds.ReclamationRhizomorphsId,
                Name = "Reclamation Rhizomorphs",
                Description = $"When your reclamation attempts fail, you have a {MycovariantGameBalance.ReclamationRhizomorphsSecondAttemptChance * 100f:0}% chance to immediately try again.",
                FlavorText = "Specialized hyphal networks persist even after setbacks, allowing the colony to recover and try again with renewed vigor.",
                Type = MycovariantType.Passive,
                Category = MycovariantCategory.Reclamation,
                IsUniversal = false,
                AutoMarkTriggered = true,
                SynergyWith = MycovariantSynergyListFactory.GetReclamationSynergyMycovariantIdsExcluding(MycovariantIds.ReclamationRhizomorphsId),
                AIScore = (player, board) => {
                    bool hasBastion = player.PlayerMycovariants.Any(pm =>
                        pm.MycovariantId == MycovariantIds.MycelialBastionIId ||
                        pm.MycovariantId == MycovariantIds.MycelialBastionIIId ||
                        pm.MycovariantId == MycovariantIds.MycelialBastionIIIId);
                    float baseScore = board.CurrentRound < 20 ? MycovariantGameBalance.ReclamationRhizomorphsBaseAIScoreEarly : MycovariantGameBalance.ReclamationRhizomorphsBaseAIScoreLate;
                    return baseScore + (hasBastion ? MycovariantGameBalance.ReclamationRhizomorphsBonusAIScore : 0f);
                }
            };
    }
}
