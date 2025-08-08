using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Mycovariants;
using System;
using System.Linq;
using System.Collections.Generic;

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
                Description = $"Immediately grow {MycovariantGameBalance.JettingMyceliumNumberOfLivingCellTiles} mold tiles {directionLabel.ToLower()} from a chosen cell, followed by a spreading cone of toxins that starts {MycovariantGameBalance.JettingMyceliumConeNarrowWidth} tile wide and expands to {MycovariantGameBalance.JettingMyceliumConeWideWidth} tiles wide.",
                FlavorText = $"The cap ruptures violently. The colony explodes {directionLabel.ToLower()}ward in a widening cloud of toxic spores.",
                Type = MycovariantType.Directional,
                Category = MycovariantCategory.Fungicide, // Provides aggressive directional growth + toxins
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    
                    bool shouldUseCoreLogic = player.PlayerType == PlayerTypeEnum.AI;
                    
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
                },
                AIScore = (player, board) =>
                {
                    var bestPlacement = JettingMyceliumHelper.FindBestPlacement(player, board, cardinalDirection);
                    if (bestPlacement == null) return 1f; // No valid placement possible
                    
                    return JettingMyceliumHelper.ScoreToAIScore(bestPlacement.Value.score);
                }
            };
        }

        public static Mycovariant NecrophoricAdaptation() =>
            new Mycovariant
            {
                Id = MycovariantIds.NecrophoricAdaptation,
                Name = "Necrophoric Adaptation",
                Description = $"When a mold cell dies, there is a {MycovariantGameBalance.NecrophoricAdaptationReclamationChance * 100f:0}% chance to reclaim an orthogonally adjacent dead tile.",
                FlavorText = "Even in death, the colony endures.",
                Type = MycovariantType.Passive,
                Category = MycovariantCategory.Reclamation, // Cell recovery from death
                IsUniversal = false,
                AutoMarkTriggered = true, // Passive effect, always considered triggered
                SynergyWith = new List<int> {
                    MycovariantIds.ReclamationRhizomorphsId // Works with Reclamation Rhizomorphs
                },
                AIScore = (player, board) => {
                    // Score based on the number of living cells (more cells = more death potential)
                    int livingCells = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => c.IsAlive);
                    // Scale from 1 to 6: 0 cells = 1, 50+ cells = 6
                    float baseScore = Math.Min(6f, Math.Max(1f, 1f + (livingCells * 5f / 50f)));
                    
                    // Bonus if player has Reclamation Rhizomorphs (synergy)
                    bool hasRhizomorphs = player.PlayerMycovariants.Any(pm =>
                        pm.MycovariantId == MycovariantIds.ReclamationRhizomorphsId);
                    float synergyBonus = hasRhizomorphs ? MycovariantGameBalance.MycovariantSynergyBonus : 0f;
                    
                    return baseScore + synergyBonus;
                }
            };

        public static Mycovariant PlasmidBounty() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyId,
                Name = "Plasmid Bounty I",
                Description = $"Instantly gain {MycovariantGameBalance.PlasmidBountyMutationPointAward} mutation points as foreign DNA infuses the colony.",
                FlavorText = "Horizontal gene transfer introduces novel genetic material, accelerating the colony's evolutionary potential.",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy, // Direct mutation point boost
                IsUniversal = true,
                AutoMarkTriggered = true, // Instant effect, always considered triggered
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    // Always apply the effect - works for both Unity and simulation
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                    {
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyMutationPointAward);
                    }
                },
                AIPrioritizeEarly = true,
                AIScore = (player, board) => 5f
            };

        public static Mycovariant PlasmidBountyII() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyIIId,
                Name = "Plasmid Bounty II",
                Description = $"Instantly gain {MycovariantGameBalance.PlasmidBountyIIMutationPointAward} mutation points as foreign DNA infuses the colony.",
                FlavorText = "Multiple plasmid integrations trigger a cascade of genetic recombination events across the mycelial network.",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy, // Direct mutation point boost
                IsUniversal = false,
                AutoMarkTriggered = true, // Instant effect, always considered triggered
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    // Always apply the effect - works for both Unity and simulation
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                    {
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyIIMutationPointAward);
                    }
                },
                AIPrioritizeEarly = true,
                AIScore = (player, board) => 7f
            };

        public static Mycovariant PlasmidBountyIII() =>
            new Mycovariant
            {
                Id = MycovariantIds.PlasmidBountyIIIId,
                Name = "Plasmid Bounty III",
                Description = $"Instantly gain {MycovariantGameBalance.PlasmidBountyIIIMutationPointAward} mutation points as foreign DNA infuses the colony.",
                FlavorText = "Massive genetic influx overwhelms cellular repair mechanisms, creating unprecedented mutation rates throughout the colony.",
                Type = MycovariantType.Economy,
                Category = MycovariantCategory.Economy, // Direct mutation point boost
                IsUniversal = false,
                AutoMarkTriggered = true, // Instant effect, always considered triggered
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    // Always apply the effect - works for both Unity and simulation
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    if (player != null)
                    {
                        player.AddMutationPoints(MycovariantGameBalance.PlasmidBountyIIIMutationPointAward);
                    }
                },
                AIPrioritizeEarly = true,
                AIScore = (player, board) => 10f
            };

        public static Mycovariant NeutralizingMantle() =>
        new Mycovariant
        {
            Id = MycovariantIds.NeutralizingMantleId,
            Name = "Neutralizing Mantle",
            Description = $"Whenever an enemy toxin is placed orthogonally adjacent to your living cells, you have a {MycovariantGameBalance.NeutralizingMantleNeutralizeChance * 100f:0}% chance to neutralize (remove) it instantly.",
            FlavorText = "A protective sheath of hyphae, secreting enzymes to break down hostile compounds.",
            Type = MycovariantType.Passive,
            Category = MycovariantCategory.Defense,
            IsUniversal = false,
            AutoMarkTriggered = true, // Passive effect, always considered triggered
            AIScore = (player, board) => MycovariantGameBalance.AIDraftModeratePriority
        };

        public static Mycovariant MycelialBastionI() =>
            new Mycovariant
            {
                Id = MycovariantIds.MycelialBastionIId,
                Name = "Mycelial Bastion I",
                Description = $"Immediately select up to {MycovariantGameBalance.MycelialBastionIMaxResistantCells} of your living cells to become Resistant (invincible). These cells cannot be killed, replaced, or converted for the rest of the game.",
                FlavorText = "A fortified network of hyphae, woven to withstand any threat.",
                Type = MycovariantType.Active,
                Category = MycovariantCategory.Resistance, // Makes cells invincible
                IsUniversal = true,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    if (player.PlayerType == PlayerTypeEnum.AI)
                    {
                        MycovariantEffectProcessor.ResolveMycelialBastion(playerMyco, board, rng, observer);
                    }
                },
                SynergyWith = new List<int> { 
                    MycovariantIds.HyphalResistanceTransferId,
                    MycovariantIds.SurgicalInoculationId
                },
                AIScore = (player, board) => MycovariantGameBalance.MycelialBastionIBaseAIScore
            };

        public static Mycovariant MycelialBastionII() =>
            new Mycovariant
            {
                Id = MycovariantIds.MycelialBastionIIId,
                Name = "Mycelial Bastion II",
                Description = $"Immediately select up to {MycovariantGameBalance.MycelialBastionIIMaxResistantCells} of your living cells to become Resistant (invincible). These cells cannot be killed, replaced, or converted for the rest of the game.",
                FlavorText = "Advanced fortification techniques create an impenetrable mycelial bulwark.",
                Type = MycovariantType.Active,
                Category = MycovariantCategory.Resistance, // Makes cells invincible
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    if (player.PlayerType == PlayerTypeEnum.AI)
                    {
                        MycovariantEffectProcessor.ResolveMycelialBastion(playerMyco, board, rng, observer);
                    }
                },
                SynergyWith = new List<int> { 
                    MycovariantIds.HyphalResistanceTransferId,
                    MycovariantIds.SurgicalInoculationId
                },
                AIScore = (player, board) => MycovariantGameBalance.MycelialBastionIIBaseAIScore
            };

        public static Mycovariant MycelialBastionIII() =>
            new Mycovariant
            {
                Id = MycovariantIds.MycelialBastionIIIId,
                Name = "Mycelial Bastion III",
                Description = $"Immediately select up to {MycovariantGameBalance.MycelialBastionIIIMaxResistantCells} of your living cells to become Resistant (invincible). These cells cannot be killed, replaced, or converted for the rest of the game.",
                FlavorText = "Master-level mycelial engineering creates an unassailable fortress of living tissue.",
                Type = MycovariantType.Active,
                Category = MycovariantCategory.Resistance, // Makes cells invincible
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    if (player.PlayerType == PlayerTypeEnum.AI)
                    {
                        MycovariantEffectProcessor.ResolveMycelialBastion(playerMyco, board, rng, observer);
                    }
                },
                SynergyWith = new List<int> { 
                    MycovariantIds.HyphalResistanceTransferId,
                    MycovariantIds.SurgicalInoculationId
                },
                AIScore = (player, board) => MycovariantGameBalance.MycelialBastionIIIBaseAIScore
            };

        public static Mycovariant SurgicalInoculation() =>
            new Mycovariant
            {
                Id = 1011,
                Name = "Surgical Inoculation",
                Description = "Place a single Resistant (invincible) fungal cell anywhere on the board, except on top of another Resistant cell.",
                FlavorText = "A single spore, delivered with surgical precision, takes root where none could before.",
                Type = MycovariantType.Active,
                Category = MycovariantCategory.Resistance, // Places invincible cell
                IsUniversal = false,
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.FirstOrDefault(p => p.PlayerId == playerMyco.PlayerId);
                    
                    bool shouldUseCoreLogic = player?.PlayerType == PlayerTypeEnum.AI;
                    
                    if (shouldUseCoreLogic)
                    {
                        // AI or Simulation: Core handles everything (selection + effect application)
                        MycovariantEffectProcessor.ResolveSurgicalInoculationAI(playerMyco, board, rng, observer);
                    }
                    // Human in Unity: UI layer handles selection + effect application
                    // (ApplyEffect does nothing, avoiding double execution)
                },
                SynergyWith = new List<int> { 
                    MycovariantIds.MycelialBastionIId,
                    MycovariantIds.MycelialBastionIIId,
                    MycovariantIds.MycelialBastionIIIId,
                    MycovariantIds.HyphalResistanceTransferId
                },
                AIScore = (player, board) => MycovariantGameBalance.AIDraftModeratePriority
            };

        public static Mycovariant PerimeterProliferator() =>
            new Mycovariant
            {
                Id = MycovariantIds.PerimeterProliferatorId,
                Name = "Perimeter Proliferator",
                Description = $"Multiplies the growth rate of your mold by {MycovariantGameBalance.PerimeterProliferatorEdgeMultiplier}x when it is within {MycovariantGameBalance.PerimeterProliferatorEdgeDistance} tiles of the edge of the board (the crust).",
                FlavorText = "At the bread's edge, the colony finds untapped vigor, racing along the crust in a surge of expansion.",
                Type = MycovariantType.Passive,
                Category = MycovariantCategory.Growth, // Enhances growth at board edges
                IsUniversal = false,
                AutoMarkTriggered = true, // Passive effect, always considered triggered
                AIScore = (player, board) =>
                {
                    // Count living cells within PerimeterProliferatorEdgeDistance of the border
                    int borderCells = 0;
                    foreach (var tile in board.AllTiles())
                    {
                        if (BoardUtilities.IsWithinEdgeDistance(tile, board.Width, board.Height, MycovariantGameBalance.PerimeterProliferatorEdgeDistance) &&
                            tile.FungalCell?.IsAlive == true &&
                            tile.FungalCell.OwnerPlayerId == player.PlayerId)
                        {
                            borderCells++;
                        }
                    }
                    // Scale from 1 to 10: 0 cells = 1, 100+ cells = 10
                    float score = Math.Min(10f, Math.Max(1f, 1f + (borderCells * 9f / 100f)));
                    return score;
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
                Category = MycovariantCategory.Resistance, // Spreads resistance to adjacent cells
                IsUniversal = false,
                AutoMarkTriggered = true, // Passive effect, always considered triggered
                SynergyWith = new List<int> {
                    MycovariantIds.MycelialBastionIId,
                    MycovariantIds.MycelialBastionIIId,
                    MycovariantIds.MycelialBastionIIIId,
                    MycovariantIds.SurgicalInoculationId
                },
                AIScore = (player, board) => MycovariantGameBalance.HyphalResistanceTransferBaseAIScoreEarly
            };

        public static Mycovariant EnduringToxaphores() =>
            new Mycovariant
            {
                Id = MycovariantIds.EnduringToxaphoresId,
                Name = "Enduring Toxaphores",
                Description = $"Immediately adds {MycovariantGameBalance.EnduringToxaphoresExistingToxinExtension} growth cycles to all your existing toxins. Additionally, all toxins you place after acquiring this will last {MycovariantGameBalance.EnduringToxaphoresNewToxinExtension} cycles longer than normal.",
                FlavorText = "Through secreted compounds, the colony's toxins linger long after their release, defying the march of time.",
                Type = MycovariantType.Passive,
                Category = MycovariantCategory.Fungicide, // Enhances toxin effectiveness
                IsUniversal = false,
                AutoMarkTriggered = true, // Immediate effect, always considered triggered
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
                            // Use the new age-based system instead of the old cycle-based system
                            cell.ToxinExpirationAge += extension;
                            extendedCount += extension;
                        }
                    }
                    if (extendedCount > 0)
                    {
                        playerMyco.IncrementEffectCount(MycovariantEffectType.ExistingExtensions, extendedCount);
                        if (observer != null)
                            observer.RecordEnduringToxaphoresExistingExtensions(player.PlayerId, extendedCount);
                    }
                },
                // AI score: 1 if no toxins, scales up to 10 based on toxin count and toxin-placing mutations
                AIScore = (player, board) =>
                {
                    int toxinCount = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => c.IsToxin);
                    if (toxinCount == 0) return 1f;
                    // Only count true toxin-dropping mutations
                    int toxinMutations = 0;
                    if (player.GetMutationLevel(MutationIds.MycotoxinTracer) > 0) toxinMutations++;
                    if (player.GetMutationLevel(MutationIds.SporocidalBloom) > 0) toxinMutations++;
                    // Score: base is log-scaled on toxin count, bonus for toxin mutations
                    double baseScore = 1.0 + 3.0 * Math.Log10(1 + toxinCount);
                    double mutationBonus = toxinMutations * 1.5;
                    double total = baseScore + mutationBonus;
                    return (float)Math.Min(10.0, Math.Max(1.0, total));
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
                Category = MycovariantCategory.Reclamation, // Improves reclamation success
                IsUniversal = false,
                AutoMarkTriggered = true, // Passive effect, always considered triggered
                AIScore = (player, board) => {
                    bool hasBastion = player.PlayerMycovariants.Any(pm =>
                        pm.MycovariantId == MycovariantIds.MycelialBastionIId ||
                        pm.MycovariantId == MycovariantIds.MycelialBastionIIId ||
                        pm.MycovariantId == MycovariantIds.MycelialBastionIIIId);
                    float baseScore = board.CurrentRound < 20 ? MycovariantGameBalance.ReclamationRhizomorphsBaseAIScoreEarly : MycovariantGameBalance.ReclamationRhizomorphsBaseAIScoreLate;
                    return baseScore + (hasBastion ? MycovariantGameBalance.ReclamationRhizomorphsBonusAIScore : 0f);
                }
            };

        public static Mycovariant BallistosporeDischargeI() =>
            new Mycovariant
            {
                Id = MycovariantIds.BallistosporeDischargeIId,
                Name = "Ballistospore Discharge I",
                Description = $"Immediately drop up to {MycovariantGameBalance.BallistosporeDischargeISpores} toxin spores on any empty space (or less, if fewer than that are available).",
                FlavorText = "The colony's fruiting bodies tense, launching a volley of toxin-laden spores across the substrate.",
                Type = MycovariantType.Active,
                Category = MycovariantCategory.Fungicide, // Deploys toxin spores
                IsUniversal = true,
                SynergyWith = new List<int> { MycovariantIds.EnduringToxaphoresId },
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

        public static Mycovariant BallistosporeDischargeII() =>
            new Mycovariant
            {
                Id = MycovariantIds.BallistosporeDischargeIIId,
                Name = "Ballistospore Discharge II",
                Description = $"Immediately drop up to {MycovariantGameBalance.BallistosporeDischargeIISpores} toxin spores on any empty space (or less, if fewer than that are available).",
                FlavorText = "A thunderous burst of spores erupts, blanketing the battlefield in a toxic haze.",
                Type = MycovariantType.Active,
                Category = MycovariantCategory.Fungicide, // Deploys toxin spores
                IsUniversal = false,
                SynergyWith = new List<int> { MycovariantIds.EnduringToxaphoresId },
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

        public static Mycovariant BallistosporeDischargeIII() =>
            new Mycovariant
            {
                Id = MycovariantIds.BallistosporeDischargeIIIId,
                Name = "Ballistospore Discharge III",
                Description = $"Immediately drop up to {MycovariantGameBalance.BallistosporeDischargeIIISpores} toxin spores on any empty space (or less, if fewer than that are available).",
                FlavorText = "The ultimate actinic volley: a storm of spores rains down, saturating the terrain with lethal intent.",
                Type = MycovariantType.Active,
                Category = MycovariantCategory.Fungicide, // Deploys toxin spores
                IsUniversal = false,
                SynergyWith = new List<int> { MycovariantIds.EnduringToxaphoresId },
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
            
        public static Mycovariant CytolyticBurst() =>
            new Mycovariant
            {
                Id = MycovariantIds.CytolyticBurstId,
                Name = "Cytolytic Burst",
                Description = $"Select one of your toxins to explode in a {MycovariantGameBalance.CytolyticBurstRadius}-tile radius. Each tile in the area has a {MycovariantGameBalance.CytolyticBurstToxinChance * 100f:0}% chance to receive a toxin, killing anything in its path.",
                FlavorText = "The toxin's cellular membrane ruptures catastrophically, releasing cytolytic enzymes in a violent cascade that spreads destruction through the surrounding substrate.",
                Type = MycovariantType.Active,
                Category = MycovariantCategory.Fungicide, // Area-of-effect toxin creation
                IsUniversal = false,
                SynergyWith = new List<int> { MycovariantIds.EnduringToxaphoresId },
                ApplyEffect = (playerMyco, board, rng, observer) =>
                {
                    var player = board.Players.First(p => p.PlayerId == playerMyco.PlayerId);
                    
                    bool shouldUseCoreLogic = player.PlayerType == PlayerTypeEnum.AI;
                    
                    if (shouldUseCoreLogic)
                    {
                        // AI or Simulation: Use helper to find best toxin to explode
                        var bestToxin = CytolyticBurstHelper.FindBestToxinToExplode(player, board);
                        
                        if (bestToxin.HasValue)
                        {
                            MycovariantEffectProcessor.ResolveCytolyticBurst(
                                playerMyco, board, bestToxin.Value.tileId, rng, observer);
                        }
                    }
                    // Human in Unity: UI layer handles selection + effect application
                    // (ApplyEffect does nothing, avoiding double execution)
                },
                AIScore = (player, board) =>
                {
                    // Use helper to find the best possible explosion and score accordingly
                    var bestToxin = CytolyticBurstHelper.FindBestToxinToExplode(player, board);
                    
                    if (!bestToxin.HasValue)
                        return 1f; // No toxins available
                    
                    int explosionScore = bestToxin.Value.score;
                    float baseScore = MycovariantGameBalance.CytolyticBurstBaseAIScore;
                    
                    if (explosionScore <= 0)
                    {
                        // Subtract 3 points if explosion would hurt more friendlies than enemies
                        return Math.Max(1f, baseScore - 3f);
                    }
                    else if (explosionScore < 20)
                    {
                        // Add 1 point for positive but modest benefit
                        return Math.Min(10f, baseScore + 1f);
                    }
                    else
                    {
                        // Add 2 points for high-value explosions
                        return Math.Min(10f, baseScore + 2f);
                    }
                }
            };

        public static Mycovariant ChemotacticMycotoxins() =>
            new Mycovariant
            {
                Id = MycovariantIds.ChemotacticMycotoxinsId,
                Name = "Chemotactic Mycotoxins",
                Description = $"At the end of each decay phase, your toxins that are no longer next to living enemy cells have an X% chance to relocate to a living enemy cell, where X is {MycovariantGameBalance.ChemotacticMycotoxinsMycotoxinTracerMultiplier} times your Mycotoxin Tracer level. Toxins relocate following the same rules as Mycotoxin Tracer.",
                FlavorText = "Sensing the absence of targets, the colony's toxic spores drift through microscopic gradients, seeking new hosts to poison.",
                Type = MycovariantType.Passive,
                Category = MycovariantCategory.Fungicide, // Enhances toxin effectiveness
                IsUniversal = false,
                AutoMarkTriggered = true, // Passive effect, always considered triggered
                SynergyWith = new List<int> { 
                    MycovariantIds.EnduringToxaphoresId // Works well with longer-lasting toxins
                },
                AIScore = (player, board) =>
                {
                    int toxinCount = board.GetAllCellsOwnedBy(player.PlayerId).Count(c => c.IsToxin);
                    int mycotoxinTracerLevel = player.GetMutationLevel(MutationIds.MycotoxinTracer);
                    
                    if (toxinCount == 0 || mycotoxinTracerLevel == 0) 
                        return 1f; // No benefit without toxins or Mycotoxin Tracer
                    
                    // Base score is 3
                    float score = 3f;
                    
                    // Bonus: +1 point if player has at least 10 toxins on the board
                    if (toxinCount >= 10)
                    {
                        score += 1f;
                    }
                    
                    // Bonus: +1 point per 5 levels of Mycotoxin Tracer
                    float tracerBonus = (mycotoxinTracerLevel / 5) * 1f;
                    score += tracerBonus;
                    
                    // Cap at maximum of 10
                    return Math.Min(10f, score);
                }
            };
    }
}
