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
            yield return SeptalAlarm();
            yield return SeptalSeal();
            yield return AggressotropicConduitI();
            yield return AggressotropicConduitII();
            yield return AggressotropicConduitIII();
        }

        private static Mycovariant MycelialBastionI() => new Mycovariant
        {
            Id = MycovariantIds.MycelialBastionIId,
            Name = "Mycelial Bastion I",
            Description = $"One-time on draft: select up to {MycovariantGameBalance.MycelialBastionIMaxResistantCells} living cells to become Resistant. Resistant cells cannot be killed, replaced, or converted for the rest of the game.",
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
            Description = $"One-time on draft: select up to {MycovariantGameBalance.MycelialBastionIIMaxResistantCells} living cells to become Resistant. Resistant cells cannot be killed, replaced, or converted for the rest of the game.",
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
            Description = $"One-time on draft: select up to {MycovariantGameBalance.MycelialBastionIIIMaxResistantCells} living cells to become Resistant. Resistant cells cannot be killed, replaced, or converted for the rest of the game.",
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
            Description = "One-time on draft: place one Resistant cell on any valid tile except a tile already occupied by a Resistant cell.",
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
            Description = $"For the rest of the game, after each growth phase, each of your living cells adjacent (including diagonally) to a Resistant cell becomes Resistant with {MycovariantGameBalance.HyphalResistanceTransferChance * 100f:0}% chance.",
            FlavorText = "The protective genetic material flows through the mycelial network, sharing resilience with neighboring cells.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Resistance,
            IsUniversal = false,
            AutoMarkTriggered = true,
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.HyphalResistanceTransferId),
            AIScore = (player, board) => MycovariantGameBalance.HyphalResistanceTransferBaseAIScoreEarly
        };

        private static Mycovariant SeptalAlarm() => new Mycovariant
        {
            Id = MycovariantIds.SeptalAlarmId,
            Name = "Septal Alarm",
            Description = $"For the rest of the game, whenever one of your living cells dies, each of your living non-Resistant cells orthogonally adjacent to it has a {MycovariantGameBalance.SeptalAlarmResistanceChance * 100f:0}% chance to become Resistant.",
            FlavorText = "Damage closes the septa and sends a hardening signal through the surviving branch tips.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Resistance,
            IsUniversal = false,
            AutoMarkTriggered = true,
            IconId = "myco_septal_alarm",
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.SeptalAlarmId),
            AIScore = (player, board) => MycovariantGameBalance.SeptalAlarmBaseAIScore
        };

        private static Mycovariant SeptalSeal() => new Mycovariant
        {
            Id = MycovariantIds.SeptalSealId,
            Name = "Septal Seal",
            Description = "Randomly make a portion of your living non-Resistant cells Resistant for the rest of the game. The portion is 30% divided by your existing Mycovariant count, rounded up, with a minimum divisor of 1.",
            FlavorText = "Sealed septa harden scattered branches before the colony learns where the next wound will land.",
            IconId = "myco_septal_seal",
            Type = MycovariantType.Active,
            Category = MycovariantCategory.Resistance,
            IsUniversal = false,
            IsLocked = true,
            RequiredMoldinessUnlockLevel = 1,
            ApplyEffect = (playerMyco, board, rng, observer) =>
            {
                MycovariantEffectProcessor.ResolveSeptalSeal(playerMyco, board, rng, observer);
            },
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.SeptalSealId),
            AIScore = (player, board) => MycovariantGameBalance.SeptalSealAIScore
        };

        private static Mycovariant AggressotropicConduitI() => new Mycovariant
        {
            Id = MycovariantIds.AggressotropicConduitIId,
            Name = "Aggressotropic Conduit I",
            Description = CreateAggressotropicConduitDescription(MycovariantGameBalance.AggressotropicConduitIReplacementsPerPhase),
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
            Description = CreateAggressotropicConduitDescription(MycovariantGameBalance.AggressotropicConduitIIReplacementsPerPhase),
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
            Description = CreateAggressotropicConduitDescription(MycovariantGameBalance.AggressotropicConduitIIIReplacementsPerPhase),
            FlavorText = "A fully committed invasive artery, tunneling decisively toward the heart of rival dominance and sealing its spearpoint in impervious tissue.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Growth,
            IsUniversal = false,
            AutoMarkTriggered = true,
            SynergyWith = MycovariantSynergyListFactory.GetResistanceSynergyMycovariantIdsExcluding(MycovariantIds.AggressotropicConduitIIIId),
            AIScore = (player, board) => 6f
        };

        private static string CreateAggressotropicConduitDescription(int tilesPerPhase)
            => $"Before each growth phase, grow up to {tilesPerPhase} tiles from your starting spore toward the enemy starting spore with the most living cells (random tie-break). The last cell placed becomes Resistant. Skips your living cells and enemy Resistant cells. Stacks with other Aggressotropic Mycovariants.";
    }
}
