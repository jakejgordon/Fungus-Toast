using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;

namespace FungusToast.Core.Mycovariants
{
    internal static class ResistanceMycovariantFactory
    {
        public static IEnumerable<Mycovariant> CreateAll()
        {
            yield return MycelialBastionI();
            yield return MycelialBastionII();
            yield return MycelialBastionIII();
            yield return SurgicalInoculation();
            yield return HyphalResistanceTransfer();
            yield return AggressotropicConduitI();
            yield return AggressotropicConduitII();
            yield return AggressotropicConduitIII();
        }

        private static Mycovariant MycelialBastionI() => new Mycovariant
        {
            Id = MycovariantIds.MycelialBastionIId,
            Name = "Mycelial Bastion I",
            Description = $"Immediately select up to {MycovariantGameBalance.MycelialBastionIMaxResistantCells} of your living cells to become Resistant (invincible). These cells cannot be killed, replaced, or converted for the rest of the game.",
            FlavorText = "A fortified network of hyphae, woven to withstand any threat.",
            Type = MycovariantType.Active,
            Category = MycovariantCategory.Resistance,
            IsUniversal = true,
            ApplyEffect = (playerMyco, board, rng, observer) =>
            {
                var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                if (player.PlayerType == PlayerTypeEnum.AI)
                    MycovariantEffectProcessor.ResolveMycelialBastion(playerMyco, board, rng, observer);
            },
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.MycelialBastionIId),
            AIScore = (player, board) => MycovariantGameBalance.MycelialBastionIBaseAIScore
        };

        private static Mycovariant MycelialBastionII() => new Mycovariant
        {
            Id = MycovariantIds.MycelialBastionIIId,
            Name = "Mycelial Bastion II",
            Description = $"Immediately select up to {MycovariantGameBalance.MycelialBastionIIMaxResistantCells} of your living cells to become Resistant (invincible). These cells cannot be killed, replaced, or converted for the rest of the game.",
            FlavorText = "Advanced fortification techniques create an impenetrable mycelial bulwark.",
            Type = MycovariantType.Active,
            Category = MycovariantCategory.Resistance,
            IsUniversal = false,
            ApplyEffect = (playerMyco, board, rng, observer) =>
            {
                var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                if (player.PlayerType == PlayerTypeEnum.AI)
                    MycovariantEffectProcessor.ResolveMycelialBastion(playerMyco, board, rng, observer);
            },
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.MycelialBastionIIId),
            AIScore = (player, board) => MycovariantGameBalance.MycelialBastionIIBaseAIScore
        };

        private static Mycovariant MycelialBastionIII() => new Mycovariant
        {
            Id = MycovariantIds.MycelialBastionIIIId,
            Name = "Mycelial Bastion III",
            Description = $"Immediately select up to {MycovariantGameBalance.MycelialBastionIIIMaxResistantCells} of your living cells to become Resistant (invincible). These cells cannot be killed, replaced, or converted for the rest of the game.",
            FlavorText = "Master-level mycelial engineering creates an unassailable fortress of living tissue.",
            Type = MycovariantType.Active,
            Category = MycovariantCategory.Resistance,
            IsUniversal = false,
            ApplyEffect = (playerMyco, board, rng, observer) =>
            {
                var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                if (player.PlayerType == PlayerTypeEnum.AI)
                    MycovariantEffectProcessor.ResolveMycelialBastion(playerMyco, board, rng, observer);
            },
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.MycelialBastionIIIId),
            AIScore = (player, board) => MycovariantGameBalance.MycelialBastionIIIBaseAIScore
        };

        private static Mycovariant SurgicalInoculation() => new Mycovariant
        {
            Id = MycovariantIds.SurgicalInoculationId,
            Name = "Surgical Inoculation",
            Description = "Place a single Resistant (invincible) fungal cell anywhere on the board, except on top of another Resistant cell.",
            FlavorText = "A single spore, delivered with surgical precision, takes root where none could before.",
            Type = MycovariantType.Active,
            Category = MycovariantCategory.Resistance,
            IsUniversal = false,
            ApplyEffect = (playerMyco, board, rng, observer) =>
            {
                var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                bool shouldUseCoreLogic = player?.PlayerType == PlayerTypeEnum.AI;
                if (shouldUseCoreLogic)
                    MycovariantEffectProcessor.ResolveSurgicalInoculationAI(playerMyco, board, rng, observer);
            },
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.SurgicalInoculationId),
            AIScore = (player, board) => Config.MycovariantGameBalance.AIDraftModeratePriority
        };

        private static Mycovariant HyphalResistanceTransfer() => new Mycovariant
        {
            Id = MycovariantIds.HyphalResistanceTransferId,
            Name = "Hyphal Resistance Transfer",
            Description = $"After each growth phase, your living cells adjacent to Resistant cells (including diagonally) have a {MycovariantGameBalance.HyphalResistanceTransferChance * 100f:0}% chance to become Resistant.",
            FlavorText = "The protective genetic material flows through the mycelial network, sharing resilience with neighboring cells.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Resistance,
            IsUniversal = false,
            AutoMarkTriggered = true,
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.HyphalResistanceTransferId),
            AIScore = (player, board) => MycovariantGameBalance.HyphalResistanceTransferBaseAIScoreEarly
        };

        private static Mycovariant AggressotropicConduitI() => new Mycovariant
        {
            Id = MycovariantIds.AggressotropicConduitIId,
            Name = "Aggressotropic Conduit I",
            Description = $"Before each growth phase, trace a path from your starting spore toward the enemy starting spore with the most living cells." +
                $"Replace up to {MycovariantGameBalance.AggressotropicConduitIReplacementsPerPhase} tiles along that path. The final replaced tile becomes Resistant. " +
                "Skips friendly living and enemy Resistant cells. Stacks with other Aggressotropic Conduit upgrades.",
            FlavorText = "A probing arterial strand advances toward dominant rival biomass, crystallizing a hardened foothold at its leading edge.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Growth,
            IsUniversal = true,
            AutoMarkTriggered = true,
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.AggressotropicConduitIId),
            AIScore = (player, board) => 3f
        };

        private static Mycovariant AggressotropicConduitII() => new Mycovariant
        {
            Id = MycovariantIds.AggressotropicConduitIIId,
            Name = "Aggressotropic Conduit II",
            Description = $"Before each growth phase, trace a path from your starting spore toward the enemy starting spore with the most living cells (random tie-break)." +
                $"Replace up to {MycovariantGameBalance.AggressotropicConduitIIReplacementsPerPhase} tiles along that path. The final replaced tile becomes Resistant. " +
                "Skips friendly living and enemy Resistant cells. Stacks with other Aggressotropic Conduit upgrades.",
            FlavorText = "The invasive corridor thickens, boring deeper toward hostile dominance while fortifying its terminal node.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Growth,
            IsUniversal = false,
            AutoMarkTriggered = true,
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.AggressotropicConduitIIId),
            AIScore = (player, board) => 4f
        };

        private static Mycovariant AggressotropicConduitIII() => new Mycovariant
        {
            Id = MycovariantIds.AggressotropicConduitIIIId,
            Name = "Aggressotropic Conduit III",
            Description = $"Before each growth phase, trace a path from your starting spore toward the enemy starting spore with the most living cells (random tie-break)." +
                $"Replace up to {MycovariantGameBalance.AggressotropicConduitIIIReplacementsPerPhase} tiles along that path. The final replaced tile becomes Resistant. " +
                "Skips friendly living and enemy Resistant cells. Stacks with other Aggressotropic Conduit upgrades.",
            FlavorText = "A fully committed invasive artery, tunneling decisively toward the heart of rival dominance and sealing its spearpoint in impervious tissue.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Growth,
            IsUniversal = false,
            AutoMarkTriggered = true,
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.AggressotropicConduitIIIId),
            AIScore = (player, board) => 6f
        };
    }
}
