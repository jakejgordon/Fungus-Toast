using FungusToast.Core.Board;
using FungusToast.Core.Players;
using FungusToast.Core.Mutations;
using System.Collections.Generic;
using System.Linq;

namespace FungusToast.Core.Phases
{
    /// <summary>
    /// Provides optimized colony size categorization for Competitive Antagonism targeting during the decay phase.
    /// Builds all categorizations in a single pass when first needed.
    /// </summary>
    public class DecayPhaseContext
    {
        private readonly GameBoard board;
        private readonly List<Player> allPlayers;
        
        // Cache for colony size categorizations
        // Key: Player ID, Value: (largerColonies, smallerColonies) relative to that player
        private Dictionary<int, (List<Player> largerColonies, List<Player> smallerColonies)>? colonySizeCache;
        
        public DecayPhaseContext(GameBoard board, List<Player> allPlayers)
        {
            this.board = board;
            this.allPlayers = allPlayers;
            this.colonySizeCache = null; // Will be populated on first access
        }
        
        /// <summary>
        /// Gets players with larger and smaller colonies relative to the specified player.
        /// On first access, builds all categorizations in a single pass over the board for maximum efficiency.
        /// </summary>
        /// <param name="currentPlayer">The player to compare against</param>
        /// <returns>Tuple containing lists of players with larger and smaller colonies</returns>
        public (List<Player> largerColonies, List<Player> smallerColonies) GetColonySizeCategorization(Player currentPlayer)
        {
            // Populate cache on first access using optimized single-pass method
            if (colonySizeCache == null)
            {
                colonySizeCache = BoardUtilities.BuildAllColonySizeCategorizations(allPlayers, board);
            }
            
            // Return cached result
            return colonySizeCache[currentPlayer.PlayerId];
        }
        
        /// <summary>
        /// Checks if competitive targeting is needed for any active players in this decay phase.
        /// Returns true if any player has both Necrophytic Bloom/Sporicidal Bloom/Mycotoxin Tracer AND Competitive Antagonism active.
        /// </summary>
        public bool IsCompetitiveTargetingNeeded()
        {
            foreach (var player in allPlayers)
            {
                if (player.IsSurgeActive(MutationIds.CompetitiveAntagonism))
                {
                    // Check if they have any mutations that benefit from competitive targeting
                    bool hasNecrophyticBloom = player.GetMutationLevel(MutationIds.NecrophyticBloom) > 0;
                    bool hasSporicidalBloom = player.GetMutationLevel(MutationIds.SporicidalBloom) > 0;
                    bool hasMycotoxinTracer = player.GetMutationLevel(MutationIds.MycotoxinTracer) > 0;
                    
                    if (hasNecrophyticBloom || hasSporicidalBloom || hasMycotoxinTracer)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
    }
}