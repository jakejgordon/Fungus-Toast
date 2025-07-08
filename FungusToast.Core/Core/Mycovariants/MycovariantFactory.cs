using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Players;
using System;
using System.Linq;

namespace FungusToast.Core.Mycovariants
{
    public static class MycovariantFactory
    {
        public static Mycovariant JettingMyceliumNorth() =>
            CreateJettingMycelium(
                "North",
                MycovariantIds.JettingMyceliumNorthId,
                CardinalDirection.North);

        public static Mycovariant JettingMyceliumEast() =>
            CreateJettingMycelium(
                "East",
                MycovariantIds.JettingMyceliumEastId,
                CardinalDirection.East);

        public static Mycovariant JettingMyceliumSouth() =>
            CreateJettingMycelium(
                "South",
                MycovariantIds.JettingMyceliumSouthId,
                CardinalDirection.South);

        public static Mycovariant JettingMyceliumWest() =>
            CreateJettingMycelium(
                "West",
                MycovariantIds.JettingMyceliumWestId,
                CardinalDirection.West);

        private static Mycovariant CreateJettingMycelium(
            string directionLabel,
            int id,
            CardinalDirection cardinalDirection)
        {
            return new Mycovariant
            {
                Id = id,
                Name = $"Jetting Mycelium ({directionLabel})",
                Description = $"Immediately grow {MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles} mold tiles {directionLabel.ToLower()} from a chosen cell, followed by {MycovariantGameBalance.JettingMyceliumNumberOfToxinTiles} toxin tiles.",
                FlavorText = $"The cap cracks. The colony launches {directionLabel.ToLower()}ward.",
                Type = MycovariantType.Directional,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    
                    bool shouldUseCoreLogic = player.PlayerType == PlayerTypeEnum.AI || 
                                             observer != null; // Simulation context
                    
                    if (shouldUseCoreLogic)
                    {
                        // AI or Simulation: Core handles everything (selection + effect application)
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
                    // Human in Unity: UI layer handles selection + effect application
                    // (ApplyEffect does nothing, avoiding double execution)
                }
            };
        }


        public static Mycovariant NecrophoricAdaptation() =>
            new Mycovariant
            {
                Id = 1002,
                Name = "Necrophoric Adaptation",
                Description = "When a mold cell dies, there is a chance to reclaim a nearby dead tile.",
                FlavorText = "Even in death, the colony endures.",
                Type = MycovariantType.Passive
            };

        internal static Mycovariant SaprobicRelay()
        {
            throw new NotImplementedException();
        }

        internal static Mycovariant SporodochialGrowth()
        {
            throw new NotImplementedException();
        }

        public static Mycovariant PlasmidBounty() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyId,
                Name = "Plasmid Bounty I",
                Description = $"Instantly gain {MycovariantGameBalance.PlasmidBountyMutationPointAward} mutation points as foreign DNA infuses the colony.",
                FlavorText = "Horizontal gene transfer introduces novel genetic material, accelerating the colony's evolutionary potential.",
                Type = MycovariantType.Economy,
                IsUniversal = true,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    // Always apply the effect - works for both Unity and simulation
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                    {
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyMutationPointAward);
                    }
                }
            };

        public static Mycovariant PlasmidBountyII() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyIIId,
                Name = "Plasmid Bounty II",
                Description = $"Instantly gain {MycovariantGameBalance.PlasmidBountyIIMutationPointAward} mutation points as foreign DNA infuses the colony.",
                FlavorText = "Multiple plasmid integrations trigger a cascade of genetic recombination events across the mycelial network.",
                Type = MycovariantType.Economy,
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    // Always apply the effect - works for both Unity and simulation
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                    {
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyIIMutationPointAward);
                    }
                }
            };

        public static Mycovariant PlasmidBountyIII() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyIIIId,
                Name = "Plasmid Bounty III",
                Description = $"Instantly gain {MycovariantGameBalance.PlasmidBountyIIIMutationPointAward} mutation points as foreign DNA infuses the colony.",
                FlavorText = "Massive genetic influx overwhelms cellular repair mechanisms, creating unprecedented mutation rates throughout the colony.",
                Type = MycovariantType.Economy,
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    // Always apply the effect - works for both Unity and simulation
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                    {
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyIIIMutationPointAward);
                    }
                }
            };

        public static Mycovariant NeutralizingMantle() =>
        new Mycovariant
        {
            Id = MycovariantIds.NeutralizingMantleId,
            Name = "Neutralizing Mantle",
            Description = $"Whenever an enemy toxin is placed orthogonally adjacent to your living cells, you have a {MycovariantGameBalance.NeutralizingMantleNeutralizeChance * 100f:0}% chance to neutralize (remove) it instantly.",
            FlavorText = "A protective sheath of hyphae, secreting enzymes to break down hostile compounds.",
            Type = MycovariantType.Passive,
            IsUniversal = false
        };

        public static Mycovariant MycelialBastionI() =>
            new Mycovariant
            {
                Id = MycovariantIds.MycelialBastionIId,
                Name = "Mycelial Bastion I",
                Description = $"Immediately select up to {MycovariantGameBalance.MycelialBastionIMaxResistantCells} of your living cells to become Resistant (invincible). These cells cannot be killed, replaced, or converted for the rest of the game.",
                FlavorText = "A fortified network of hyphae, woven to withstand any threat.",
                Type = MycovariantType.Active,
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    
                    if (player.PlayerType == PlayerTypeEnum.AI)
                    {
                        // AI: Core handles everything (selection + effect application)
                        MycovariantEffectProcessor.ResolveMycelialBastion(playerMyco, board, rng, observer);
                    }
                    // Human: UI layer handles selection + effect application
                    // (ApplyEffect does nothing, avoiding double execution)
                }
            };

        public static Mycovariant MycelialBastionII() =>
            new Mycovariant
            {
                Id = MycovariantIds.MycelialBastionIIId,
                Name = "Mycelial Bastion II",
                Description = $"Immediately select up to {MycovariantGameBalance.MycelialBastionIIMaxResistantCells} of your living cells to become Resistant (invincible). These cells cannot be killed, replaced, or converted for the rest of the game.",
                FlavorText = "Advanced fortification techniques create an impenetrable mycelial bulwark.",
                Type = MycovariantType.Active,
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    
                    if (player.PlayerType == PlayerTypeEnum.AI)
                    {
                        // AI: Core handles everything (selection + effect application)
                        MycovariantEffectProcessor.ResolveMycelialBastion(playerMyco, board, rng, observer);
                    }
                    // Human: UI layer handles selection + effect application
                    // (ApplyEffect does nothing, avoiding double execution)
                }
            };

        public static Mycovariant MycelialBastionIII() =>
            new Mycovariant
            {
                Id = MycovariantIds.MycelialBastionIIIId,
                Name = "Mycelial Bastion III",
                Description = $"Immediately select up to {MycovariantGameBalance.MycelialBastionIIIMaxResistantCells} of your living cells to become Resistant (invincible). These cells cannot be killed, replaced, or converted for the rest of the game.",
                FlavorText = "Master-level mycelial engineering creates an unassailable fortress of living tissue.",
                Type = MycovariantType.Active,
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    
                    if (player.PlayerType == PlayerTypeEnum.AI)
                    {
                        // AI: Core handles everything (selection + effect application)
                        MycovariantEffectProcessor.ResolveMycelialBastion(playerMyco, board, rng, observer);
                    }
                    // Human: UI layer handles selection + effect application
                    // (ApplyEffect does nothing, avoiding double execution)
                }
            };

        public static Mycovariant SurgicalInoculation() =>
            new Mycovariant
            {
                Id = 1011,
                Name = "Surgical Inoculation",
                Description = "Place a single Resistant (invincible) fungal cell anywhere on the board, except on top of another Resistant cell.",
                FlavorText = "A single spore, delivered with surgical precision, takes root where none could before.",
                Type = MycovariantType.Active,
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    
                    bool shouldUseCoreLogic = player?.PlayerType == PlayerTypeEnum.AI || 
                                             observer != null; // Simulation context
                    
                    if (shouldUseCoreLogic)
                    {
                        // AI or Simulation: Core handles everything (selection + effect application)
                        MycovariantEffectProcessor.ResolveSurgicalInoculationAI(playerMyco, board, rng, observer);
                    }
                    // Human in Unity: UI layer handles selection + effect application
                    // (ApplyEffect does nothing, avoiding double execution)
                }
            };

        public static Mycovariant PerimeterProliferator() =>
            new Mycovariant
            {
                Id = MycovariantIds.PerimeterProliferatorId,
                Name = "Perimeter Proliferator",
                Description = $"Multiplies the growth rate of your mold by {MycovariantGameBalance.PerimeterProliferatorEdgeMultiplier}x when it is adjacent to the crust (the outer edge of the board).",
                FlavorText = "At the bread's edge, the colony finds untapped vigor, racing along the crust in a surge of expansion.",
                Type = MycovariantType.Passive,
                IsUniversal = false,
                // This mycovariant's effect should be checked/applied in the growth phase logic:
                // If a cell is adjacent to the board edge, double its growth rate for that cycle.
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    // Passive: No immediate effect. Growth logic must check for this mycovariant and apply the multiplier.
                }
            };

        public static Mycovariant HyphalResistanceTransfer() =>
            new Mycovariant
            {
                Id = MycovariantIds.HyphalResistanceTransferId,
                Name = "Hyphal Resistance Transfer",
                Description = $"After each growth phase, your living cells adjacent to Resistant cells (including diagonally) have a {MycovariantGameBalance.HyphalResistanceTransferChance * 100f:0}% chance to become Resistant.",
                FlavorText = "The protective genetic material flows through the mycelial network, sharing resilience with neighboring cells.",
                Type = MycovariantType.Passive,
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    // Passive: No immediate effect. This effect is handled in the MycovariantEffectProcessor
                    // after each growth phase, checking for this mycovariant and applying the transfer effect.
                }
            };

        public static Mycovariant EnduringToxaphores() =>
            new Mycovariant
            {
                Id = MycovariantIds.EnduringToxaphoresId,
                Name = "Enduring Toxaphores",
                Description = $"Extends the lifespan of your newly placed toxins by {MycovariantGameBalance.EnduringToxaphoresNewToxinExtension} growth cycles, and your existing toxins by {MycovariantGameBalance.EnduringToxaphoresExistingToxinExtension} cycles upon acquisition.",
                FlavorText = "Through secreted compounds, the colony's toxins linger long after their release, defying the march of time.",
                Type = MycovariantType.Passive,
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    // Extend all existing toxins by the configured number of cycles at acquisition
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    int extension = MycovariantGameBalance.EnduringToxaphoresExistingToxinExtension;
                    int extendedCount = 0;
                    foreach (var cell in board.GetAllCellsOwnedBy(player.PlayerId))
                    {
                        if (cell.IsToxin)
                        {
                            cell.ToxinExpirationCycle += extension;
                            extendedCount += extension;
                        }
                    }
                    if (extendedCount > 0)
                    {
                        playerMyco.IncrementEffectCount(MycovariantEffectType.ExistingExtensions, extendedCount);
                        if (observer != null)
                            observer.RecordEnduringToxaphoresExistingExtensions(player.PlayerId, extendedCount);
                    }
                }
            };

        public static Mycovariant ReclamationRhizomorphs() =>
            new Mycovariant
            {
                Id = MycovariantIds.ReclamationRhizomorphsId,
                Name = "Reclamation Rhizomorphs",
                Description = $"When your reclamation attempts fail, you have a {MycovariantGameBalance.ReclamationRhizomorphsSecondAttemptChance * 100f:0}% chance to immediately try again.",
                FlavorText = "Specialized hyphal networks persist even after setbacks, allowing the colony to recover and try again with renewed vigor.",
                Type = MycovariantType.Passive,
                IsUniversal = false
                // This is a passive effect that triggers on failed reclamation attempts
                // The effect logic is handled in MycovariantEffectProcessor via event subscription
            };

    }
}
