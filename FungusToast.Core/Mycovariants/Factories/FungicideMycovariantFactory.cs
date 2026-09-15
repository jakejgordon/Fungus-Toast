using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Mycovariants
{
    internal static class FungicideMycovariantFactory
    {
        public static IEnumerable<Mycovariant> CreateAll()
        {
            yield return NeutralizingMantle();
            yield return EnduringToxaphores();
            yield return BallistosporeDischargeI();
            yield return BallistosporeDischargeII();
            yield return BallistosporeDischargeIII();
            yield return CytolyticBurst();
            yield return ChemotacticMycotoxins();
        }

        private static Mycovariant NeutralizingMantle() => new Mycovariant
        {
            Id = MycovariantIds.NeutralizingMantleId,
            Name = "Neutralizing Mantle",
            Description = $"For the rest of the game, whenever an enemy toxin appears orthogonally adjacent (up / down / left / right) to one of your living cells, clear it immediately with {MycovariantGameBalance.NeutralizingMantleNeutralizeChance * 100f:0}% chance.",
            FlavorText = "A protective sheath of hyphae, secreting enzymes to break down hostile compounds.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Defense,
            IsUniversal = false,
            AutoMarkTriggered = true,
            AIScore = (player, board) => MycovariantGameBalance.AIDraftModeratePriority
        };

        private static Mycovariant EnduringToxaphores() => new Mycovariant
        {
            Id = MycovariantIds.EnduringToxaphoresId,
            Name = "Enduring Toxaphores",
            Description = $"One-time on draft: extend all of your current toxins by {MycovariantGameBalance.EnduringToxaphoresExistingToxinExtension} Growth Cycles. For the rest of the game, new toxins you place last {MycovariantGameBalance.EnduringToxaphoresNewToxinExtension} extra Growth Cycles.",
            FlavorText = "Through secreted compounds, the colony's toxins linger long after their release, defying the march of time.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Fungicide,
            IsUniversal = false,
            AutoMarkTriggered = true,
            ApplyEffect = (playerMyco, board, rng, observer) =>
            {
                var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                int extension = MycovariantGameBalance.EnduringToxaphoresExistingToxinExtension;
                int extendedCount = 0;
                foreach (var cell in board.GetAllCellsOwnedBy(player.PlayerId))
                {
                    if (cell.IsToxin)
                    {
                        cell.ToxinExpirationAge += extension;
                        extendedCount += extension;
                    }
                }
                if (extendedCount > 0)
                {
                    playerMyco.IncrementEffectCount(MycovariantEffectType.ExistingExtensions, extendedCount);
                    observer?.RecordEnduringToxaphoresExistingExtensions(player.PlayerId, extendedCount);
                }
            },
            SynergyWith = MycovariantSynergyListFactory.GetToxinSynergyMycovariantIdsExcluding(MycovariantIds.EnduringToxaphoresId),
            AIScore = (player, board) =>
            {
                int toxinCount = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => c.IsToxin);
                if (toxinCount == 0) return 1f;
                int toxinMutations = 0;
                if (player.GetMutationLevel(MutationIds.MycotoxinTracer) > 0) toxinMutations++;
                if (player.GetMutationLevel(MutationIds.SporicidalBloom) > 0) toxinMutations++;
                double baseScore = 1.0 + 3.0 * System.Math.Log10(1 + toxinCount);
                double mutationBonus = toxinMutations * 1.5;
                double total = baseScore + mutationBonus;
                return (float)System.Math.Min(10.0, System.Math.Max(1.0, total));
            }
        };

        private static Mycovariant BallistosporeDischargeI() => new Mycovariant
        {
            Id = MycovariantIds.BallistosporeDischargeIId,
            Name = "Ballistospore Discharge I",
            Description = $"One-time on draft: launch toxin spores to toxify up to {MycovariantGameBalance.BallistosporeDischargeISpores} empty tiles.",
            FlavorText = "The colony's fruiting bodies tense, launching a volley of toxin-laden spores across the substrate.",
            Type = MycovariantType.Active,
            Category = MycovariantCategory.Fungicide,
            IsUniversal = true,
            SynergyWith = MycovariantSynergyListFactory.GetToxinSynergyMycovariantIds(),
            ApplyEffect = (playerMyco, board, rng, observer) =>
            {
                BallistosporeDischargeHelper.ResolveBallistosporeDischarge(
                    playerMyco,
                    board,
                    MycovariantGameBalance.BallistosporeDischargeISpores,
                    rng,
                    observer);
            },
            AIScore = (player, board) => MycovariantGameBalance.BallistosporeDischargeIAIScore
        };

        private static Mycovariant BallistosporeDischargeII() => new Mycovariant
        {
            Id = MycovariantIds.BallistosporeDischargeIIId,
            Name = "Ballistospore Discharge II",
            Description = $"One-time on draft: launch toxin spores to toxify up to {MycovariantGameBalance.BallistosporeDischargeIISpores} empty tiles.",
            FlavorText = "A second, heavier burst follows, blanketing the crumb in a toxic haze.",
            Type = MycovariantType.Active,
            Category = MycovariantCategory.Fungicide,
            IsUniversal = false,
            SynergyWith = MycovariantSynergyListFactory.GetToxinSynergyMycovariantIds(),
            ApplyEffect = (playerMyco, board, rng, observer) =>
            {
                BallistosporeDischargeHelper.ResolveBallistosporeDischarge(
                    playerMyco,
                    board,
                    MycovariantGameBalance.BallistosporeDischargeIISpores,
                    rng,
                    observer);
            },
            AIScore = (player, board) => MycovariantGameBalance.BallistosporeDischargeIIIAIScore
        };

        private static Mycovariant BallistosporeDischargeIII() => new Mycovariant
        {
            Id = MycovariantIds.BallistosporeDischargeIIIId,
            Name = "Ballistospore Discharge III",
            Description = $"One-time on draft: launch toxin spores to toxify up to {MycovariantGameBalance.BallistosporeDischargeIIISpores} empty tiles.",
            FlavorText = "The colony empties its fruiting bodies at once, and the whole crust falls silent under the settling haze.",
            Type = MycovariantType.Active,
            Category = MycovariantCategory.Fungicide,
            IsUniversal = false,
            SynergyWith = MycovariantSynergyListFactory.GetToxinSynergyMycovariantIds(),
            ApplyEffect = (playerMyco, board, rng, observer) =>
            {
                BallistosporeDischargeHelper.ResolveBallistosporeDischarge(
                    playerMyco,
                    board,
                    MycovariantGameBalance.BallistosporeDischargeIIISpores,
                    rng,
                    observer);
            },
            AIScore = (player, board) => MycovariantGameBalance.BallistosporeDischargeIIIAIScore
        };

        private static Mycovariant CytolyticBurst() => new Mycovariant
        {
            Id = MycovariantIds.CytolyticBurstId,
            Name = "Cytolytic Burst",
            Description = $"One-time on draft: choose one of your toxins to burst in a {MycovariantGameBalance.CytolyticBurstRadius}-tile radius. Each tile in range has {MycovariantGameBalance.CytolyticBurstToxinChance * 100f:0}% chance to poison a non-Resistant living cell or toxify an empty tile or dead cell.",
            FlavorText = "The toxin's membrane ruptures, and cytolytic enzymes spread outward through the surrounding substrate.",
            Type = MycovariantType.Active,
            Category = MycovariantCategory.Fungicide,
            IsUniversal = false,
            SynergyWith = MycovariantSynergyListFactory.GetToxinSynergyMycovariantIds(),
            ApplyEffect = (playerMyco, board, rng, observer) =>
            {
                var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                bool shouldUseCoreLogic = player.PlayerType == PlayerTypeEnum.AI;
                if (shouldUseCoreLogic)
                {
                    var bestToxin = CytolyticBurstHelper.FindBestToxinToExplode(player, board);
                    if (bestToxin.HasValue)
                    {
                        MycovariantEffectProcessor.ResolveCytolyticBurst(
                            playerMyco, board, bestToxin.Value.tileId, rng, observer);
                    }
                }
            },
            AIScore = (player, board) =>
            {
                var bestToxin = CytolyticBurstHelper.FindBestToxinToExplode(player, board);
                if (!bestToxin.HasValue) return 1f;
                int explosionScore = bestToxin.Value.score;
                float baseScore = MycovariantGameBalance.CytolyticBurstBaseAIScore;
                if (explosionScore <= 0) return System.Math.Max(1f, baseScore - 3f);
                if (explosionScore < 20) return System.Math.Min(10f, baseScore + 1f);
                return System.Math.Min(10f, baseScore + 2f);
            }
        };

        private static Mycovariant ChemotacticMycotoxins() => new Mycovariant
        {
            Id = MycovariantIds.ChemotacticMycotoxinsId,
            Name = "Chemotactic Mycotoxins",
            Description = $"For the rest of the game, at the end of each Decay Phase, each of your toxins with no orthogonally adjacent (up / down / left / right) enemy living cell has a {MycovariantGameBalance.ChemotacticMycotoxinsMycotoxinTracerMultiplier:0.#}% chance per Mycotoxin Tracer level to move to a random empty tile next to an enemy living cell.",
            FlavorText = "Sensing the absence of targets, the colony's toxic spores drift through microscopic gradients, seeking new hosts to poison.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Fungicide,
            IsUniversal = false,
            AutoMarkTriggered = true,
            SynergyWith = MycovariantSynergyListFactory.GetToxinSynergyMycovariantIdsExcluding(MycovariantIds.ChemotacticMycotoxinsId),
            AIScore = (player, board) =>
            {
                int toxinCount = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => c.IsToxin);
                int tracerLevel = player.GetMutationLevel(MutationIds.MycotoxinTracer);
                if (toxinCount == 0 || tracerLevel == 0) return 1f;
                float score = 3f;
                if (toxinCount >= 10) score += 1f;
                float tracerBonus = (tracerLevel / 5) * 1f;
                score += tracerBonus;
                return System.Math.Min(10f, score);
            }
        };
    }
}
