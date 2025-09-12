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
            Description = $"Whenever an enemy toxin is placed orthogonally adjacent to your living cells, you have a {MycovariantGameBalance.NeutralizingMantleNeutralizeChance * 100f:0}% chance to neutralize (remove) it instantly.",
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
            Description = $"Immediately adds {MycovariantGameBalance.EnduringToxaphoresExistingToxinExtension} growth cycles to all your existing toxins. Additionally, all toxins you place after acquiring this will last {MycovariantGameBalance.EnduringToxaphoresNewToxinExtension} cycles longer than normal.",
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
            Description = $"Immediately drop up to {MycovariantGameBalance.BallistosporeDischargeISpores} toxin spores on any empty space (or less, if fewer than that are available).",
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
            Description = $"Immediately drop up to {MycovariantGameBalance.BallistosporeDischargeIISpores} toxin spores on any empty space (or less, if fewer than that are available).",
            FlavorText = "A thunderous burst of spores erupts, blanketing the battlefield in a toxic haze.",
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
            Description = $"Immediately drop up to {MycovariantGameBalance.BallistosporeDischargeIIISpores} toxin spores on any empty space (or less, if fewer than that are available).",
            FlavorText = "The ultimate actinic volley: a storm of spores rains down, saturating the terrain with lethal intent.",
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
            Description = $"Select one of your toxins to explode in a {MycovariantGameBalance.CytolyticBurstRadius}-tile radius. Each tile in the area has a {MycovariantGameBalance.CytolyticBurstToxinChance * 100f:0}% chance to receive a toxin, killing anything in its path.",
            FlavorText = "The toxin's cellular membrane ruptures catastrophically, releasing cytolytic enzymes in a violent cascade that spreads destruction through the surrounding substrate.",
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
            Description = $"End of decay: isolated toxins each have X% chance to relocate to a living enemy (X = {MycovariantGameBalance.ChemotacticMycotoxinsMycotoxinTracerMultiplier} × Mycotoxin Tracer level; same targeting rules).",
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
