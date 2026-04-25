using System.Collections.Generic;
using FungusToast.Core.Config;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    internal static class EconomyMycovariantFactory
    {
        public static IEnumerable<Mycovariant> CreateAll()
        {
            yield return PlasmidBounty();
            yield return PlasmidBountyII();
            yield return PlasmidBountyIII();
            yield return AscusWager();
            yield return AscusBait();
            yield return SporalSnare();
        }

        private static Mycovariant PlasmidBounty() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyId,
                Name = "Plasmid Bounty I",
                Description = $"One-time on draft: absorb foreign plasmids and gain {MycovariantGameBalance.PlasmidBountyMutationPointAward} mutation points.",
                FlavorText = "Horizontal gene transfer introduces novel genetic material, accelerating the colony's evolutionary potential.",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy,
                IsUniversal = true,
                AutoMarkTriggered = true,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyMutationPointAward);
                },
                AIPrioritizeEarly = true,
                AIScore = (player, board) => 5f
            };

        private static Mycovariant PlasmidBountyII() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyIIId,
                Name = "Plasmid Bounty II",
                Description = $"One-time on draft: absorb foreign plasmids and gain {MycovariantGameBalance.PlasmidBountyIIMutationPointAward} mutation points.",
                FlavorText = "Multiple plasmid integrations trigger a cascade of genetic recombination events across the mycelial network.",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy,
                IsUniversal = false,
                AutoMarkTriggered = true,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyIIMutationPointAward);
                },
                AIPrioritizeEarly = true,
                AIScore = (player, board) => 7f
            };

        private static Mycovariant PlasmidBountyIII() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyIIIId,
                Name = "Plasmid Bounty III",
                Description = $"One-time on draft: absorb foreign plasmids and gain {MycovariantGameBalance.PlasmidBountyIIIMutationPointAward} mutation points.",
                FlavorText = "Massive genetic influx overwhelms cellular repair mechanisms, creating unprecedented mutation rates throughout the colony.",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy,
                IsUniversal = false,
                AutoMarkTriggered = true,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyIIIMutationPointAward);
                },
                AIPrioritizeEarly = true,
                AIScore = (player, board) => 10f
            };

        private static Mycovariant AscusWager() =>
            new Mycovariant
            {
                Id = MycovariantIds.AscusWagerId,
                Name = "Ascus Wager",
                Description = $"One-time on draft: gain {MycovariantGameBalance.AscusWagerTier5LevelsGranted} free level of a random Tier 5 mutation, ignoring prerequisites.",
                FlavorText = "A sealed ascus bursts with reckless promise, gambling the colony's future on a single rare trait.",
                IconId = "myco_ascus_wager",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy,
                IsUniversal = false,
                AutoMarkTriggered = true,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    MycovariantEffectProcessor.ResolveAscusWager(playerMyco, board, rng, observer);
                },
                AIPrioritizeEarly = true,
                AIScore = (player, board) => MycovariantGameBalance.AscusWagerAIScore
            };

        private static Mycovariant AscusBait() =>
            new Mycovariant
            {
                Id = MycovariantIds.AscusBaitId,
                Name = "Ascus Bait",
                Description = $"One-time on draft: if Human, gain {MycovariantGameBalance.AscusBaitMutationPointAward} mutation points. If AI, {MycovariantGameBalance.AscusBaitSelfCullPercentage * 100f:0}% of your non-Resistant living cells die at random (rounded up).",
                FlavorText = "A swollen ascus promises easy advantage, luring rash colonies into rupturing their own hyphae.",
                IconId = "myco_ascus_bait",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy,
                IsUniversal = true,
                IsLocked = true,
                RequiredMoldinessUnlockLevel = 1,
                AutoMarkTriggered = true,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    MycovariantEffectProcessor.ResolveAscusBait(playerMyco, board, rng, observer);
                },
                AIPrioritizeEarly = false,
                AIScore = (player, board) => player.IsLastAiMycovariantDrafterForCurrentDraft
                    ? MycovariantGameBalance.AscusBaitPreferredAIScore
                    : MycovariantGameBalance.AscusBaitFallbackAIScore
            };

        private static Mycovariant SporalSnare() =>
            new Mycovariant
            {
                Id = MycovariantIds.SporalSnareId,
                Name = "Sporal Snare",
                Description = $"The leading AI player always prefers drafting this Mycovariant, causing a line of living cells from the Human player to shoot toward the AI player's starting spore, reclaiming dead cells, infesting non-resistant living cells, and overgrowing toxins. If drafted by the Human player, grants {MycovariantGameBalance.SporalSnareMutationPointAward} mutation points.",
                FlavorText = "A baited pore-mouth yawns open, inviting rival growth to thread a breach straight back through the taker's own lane.",
            IconId = "myco_sporal_snare",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy,
                IsUniversal = true,
                IsLocked = true,
                RequiredMoldinessUnlockLevel = 6,
                AutoMarkTriggered = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    MycovariantEffectProcessor.ResolveSporalSnare(playerMyco, board, rng, observer);
                },
                AIPrioritizeEarly = false,
                AIScore = (player, board) => player.IsLastAiMycovariantDrafterForCurrentDraft
                    ? MycovariantGameBalance.SporalSnarePreferredAIScore
                    : MycovariantGameBalance.SporalSnareFallbackAIScore
            };
    }
}
