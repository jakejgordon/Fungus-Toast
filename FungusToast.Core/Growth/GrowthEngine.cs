using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Events;
using FungusToast.Core.Metrics;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;
using FungusToast.Core.Mycovariants;
using System.Linq;

namespace FungusToast.Core.Growth
{
    public static class GrowthEngine
    {
        public static Dictionary<int, int> ExecuteGrowthCycle(
            GameBoard board,
            List<Player> players,
            Random rng,
            RoundContext roundContext,
            ISimulationObserver observer)
        {
            board.OnPreGrowthCycle();

            var failedGrowthsByPlayerId = players.ToDictionary(p => p.PlayerId, _ => 0);
            var crustwardAutomaticGrowthUsedPlayerIds = new HashSet<int>();

            // PRIORITY 2 (Option B): Use ControlledTileIds instead of rescanning board per player
            int CountAlive(Player p)
            {
                int count = 0;
                foreach (var tileId in p.ControlledTileIds)
                {
                    var tile = board.GetTileById(tileId);
                    var cell = tile?.FungalCell;
                    if (cell != null && cell.IsAlive && cell.OwnerPlayerId == p.PlayerId)
                        count++;
                }
                return count;
            }
            var playerLivingCellCounts = players.ToDictionary(p => p.PlayerId, p => CountAlive(p));
            var playersOrderedByLivingCells = players.OrderBy(p => playerLivingCellCounts[p.PlayerId]).ToList();

            foreach (var player in playersOrderedByLivingCells)
            {
                // Gather this player's living cells via ControlledTileIds (avoids full-board enumeration)
                var playerCells = new List<(BoardTile tile, FungalCell cell)>();
                foreach (var tileId in player.ControlledTileIds)
                {
                    var tile = board.GetTileById(tileId);
                    var cell = tile?.FungalCell;
                    if (cell != null && cell.IsAlive && cell.OwnerPlayerId == player.PlayerId)
                        playerCells.Add((tile!, cell));
                }

                Shuffle(playerCells, rng);

                foreach (var (tile, cell) in playerCells)
                {
                    bool grewOrMoved = TryExpandFromTile(
                        board,
                        tile,
                        player,
                        rng,
                        observer,
                        crustwardAutomaticGrowthUsedPlayerIds);
                    if (!grewOrMoved)
                        failedGrowthsByPlayerId[player.PlayerId]++;
                }
            }

            board.IncrementGrowthCycle();
            AgeCells(board, players);
            board.ExpireToxinTiles(board.CurrentGrowthCycle, observer);
            return failedGrowthsByPlayerId;
        }

        /// <summary>
        /// Ages all living and toxin cells. This should happen at the end of each growth cycle.
        /// </summary>
        private static void AgeCells(GameBoard board, List<Player> players)
        {
            // Age all living cells (retain existing implementation; could be optimized later if needed)
            List<BoardTile> livingTiles = board.AllTiles()
                .Where(t => t.FungalCell is { IsAlive: true })
                .ToList();
            foreach (BoardTile tile in livingTiles)
            {
                FungalCell cell = tile.FungalCell!;
                cell.IncrementGrowthAge();
            }
            List<BoardTile> toxinTiles = board.AllTiles()
                .Where(t => t.FungalCell is { IsToxin: true })
                .ToList();
            foreach (BoardTile tile in toxinTiles)
            {
                FungalCell toxinCell = tile.FungalCell!;
                toxinCell.IncrementGrowthAge();
            }
        }



        /// <summary>
        /// Attempts to grow or move from a single tile. Tracks orthogonal, diagonal (Tendril), Creeping Mold, and Necrohyphal Infiltration.
        /// </summary>
        private static bool TryExpandFromTile(
            GameBoard board,
            BoardTile sourceTile,
            Player owner,
            Random rng,
            ISimulationObserver observer,
            ISet<int> crustwardAutomaticGrowthUsedPlayerIds)
        {
            var sourceCell = sourceTile.FungalCell;
            if (sourceCell == null)
                return false;

            (float baseChance, float surgeBonus) = GrowthMutationProcessor.GetGrowthChancesWithHyphalSurge(owner);
            var allTargets = GetAllGrowthTargets(board, sourceTile, owner, baseChance, surgeBonus);
            Shuffle(allTargets, rng);
            foreach (var target in allTargets)
            {
                if (AttemptStandardOrSurgeGrowth(
                    board,
                    owner,
                    sourceTile.TileId,
                    target,
                    rng,
                    observer,
                    crustwardAutomaticGrowthUsedPlayerIds))
                    return true;
                if (target == allTargets[0] && AttemptCreepingMold(board, owner, sourceCell, sourceTile, target.Tile, rng, observer))
                    return true;
            }
            return AttemptNecrohyphalInfiltration(board, sourceTile, sourceCell, owner, rng, observer);
        }

        private static List<GrowthTarget> GetAllGrowthTargets(
            GameBoard board,
            BoardTile sourceTile,
            Player owner,
            float baseChance,
            float surgeBonus)
        {
            var targets = new List<GrowthTarget>();
            bool hasMaxCreepingMold = owner.GetMutationLevel(MutationIds.CreepingMold) == GameBalance.CreepingMoldMaxLevel;
            float edgeMultiplier = GetEdgeGrowthMultiplier(owner, sourceTile, board);
            bool hasRhizomorphicHunger = owner.HasAdaptation(AdaptationIds.RhizomorphicHunger);
            bool hasOssifiedAdvance = owner.HasAdaptation(AdaptationIds.OssifiedAdvance);
            bool sourceIsResistant = sourceTile.FungalCell?.IsResistant == true;
            foreach (BoardTile tile in board.GetOrthogonalNeighbors(sourceTile.X, sourceTile.Y))
            {
                if (!tile.IsOccupied && !board.IsTileBlockedForOccupation(tile.TileId) && tile.TileId != sourceTile.TileId)
                {
                    float nutrientBonus = (hasRhizomorphicHunger && tile.HasNutrientPatch)
                        ? AdaptationGameBalance.RhizomorphicHungerGrowthBonus
                        : 0f;
                    float ossifiedBonus = (hasOssifiedAdvance && sourceIsResistant)
                        ? AdaptationGameBalance.OssifiedAdvanceOrthogonalBonus
                        : 0f;
                    float aeratedFrontierBonus = SubstrateEcologyMutationProcessor.GetAeratedFrontierGrowthBonus(owner, board, sourceTile);
                    float crustwardTropismBonus = SubstrateEcologyMutationProcessor.GetCrustwardTropismGrowthBonus(owner, board, sourceTile, tile);
                    float compactionPressureBonus = SubstrateEcologyMutationProcessor.GetCompactionPressureGrowthBonus(owner, board, sourceTile);
                    float detritalEnzymesBonus = SubstrateEcologyMutationProcessor.GetDetritalEnzymesGrowthBonus(owner, board, tile);
                    float detritalEnzymesDenseDeadMatterBonus = SubstrateEcologyMutationProcessor.GetDetritalEnzymesDenseDeadMatterBonus(owner, board, tile);
                    float toxinMarginBonus = SubstrateEcologyMutationProcessor.GetToxinMarginGrowthBonus(owner, board, tile);
                    float toxinborneSeedingBonus = SubstrateEcologyMutationProcessor.GetToxinborneSeedingGrowthBonus(owner, board, tile);
                    float ecologyBonus = Math.Min(
                        aeratedFrontierBonus + crustwardTropismBonus + compactionPressureBonus + detritalEnzymesBonus + toxinMarginBonus + toxinborneSeedingBonus,
                        GameBalance.SubstrateEcologyCombinedGrowthBonusCap);
                    targets.Add(new GrowthTarget(
                        tile,
                        baseChance * edgeMultiplier + nutrientBonus + ossifiedBonus + ecologyBonus,
                        null,
                        surgeBonus,
                        ecologyBonus,
                        aeratedFrontierBonus,
                        crustwardTropismBonus,
                        compactionPressureBonus,
                        detritalEnzymesBonus,
                        detritalEnzymesDenseDeadMatterBonus,
                        toxinMarginBonus,
                        toxinborneSeedingBonus));
                }
                else if (hasMaxCreepingMold && tile.FungalCell != null && tile.FungalCell.IsToxin)
                {
                    targets.Add(new GrowthTarget(tile, baseChance * edgeMultiplier, null, surgeBonus, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f));
                }
            }
            var diagonalDirs = new (int dx, int dy, DiagonalDirection dir, int mutationId)[]
            {
                (-1,  1, DiagonalDirection.Northwest, MutationIds.TendrilNorthwest),
                ( 1,  1, DiagonalDirection.Northeast, MutationIds.TendrilNortheast),
                ( 1, -1, DiagonalDirection.Southeast, MutationIds.TendrilSoutheast),
                (-1, -1, DiagonalDirection.Southwest, MutationIds.TendrilSouthwest),
            };
            foreach (var (dx, dy, dir, mutationId) in diagonalDirs)
            {
                if (owner.GetMutationLevel(mutationId) <= 0)
                    continue;
                float chance = GrowthMutationProcessor.GetEffectiveDirectionalDiagonalGrowthChance(owner, dir) * edgeMultiplier;
                if (chance <= 0f) continue;
                int nx = sourceTile.X + dx;
                int ny = sourceTile.Y + dy;
                var maybeTile = board.GetTile(nx, ny);
                if (maybeTile is { IsOccupied: false, TileId: var id } && !board.IsTileBlockedForOccupation(id) && id != sourceTile.TileId)
                {
                    float aeratedFrontierBonus = SubstrateEcologyMutationProcessor.GetAeratedFrontierGrowthBonus(owner, board, sourceTile);
                    float crustwardTropismBonus = SubstrateEcologyMutationProcessor.GetCrustwardTropismGrowthBonus(owner, board, sourceTile, maybeTile);
                    float compactionPressureBonus = SubstrateEcologyMutationProcessor.GetCompactionPressureGrowthBonus(owner, board, sourceTile);
                    float detritalEnzymesBonus = SubstrateEcologyMutationProcessor.GetDetritalEnzymesGrowthBonus(owner, board, maybeTile);
                    float detritalEnzymesDenseDeadMatterBonus = SubstrateEcologyMutationProcessor.GetDetritalEnzymesDenseDeadMatterBonus(owner, board, maybeTile);
                    float toxinMarginBonus = SubstrateEcologyMutationProcessor.GetToxinMarginGrowthBonus(owner, board, maybeTile);
                    float toxinborneSeedingBonus = SubstrateEcologyMutationProcessor.GetToxinborneSeedingGrowthBonus(owner, board, maybeTile);
                    float ecologyBonus = Math.Min(
                        aeratedFrontierBonus + crustwardTropismBonus + compactionPressureBonus + detritalEnzymesBonus + toxinMarginBonus + toxinborneSeedingBonus,
                        GameBalance.SubstrateEcologyCombinedGrowthBonusCap);
                    targets.Add(new GrowthTarget(
                        maybeTile,
                        chance + ecologyBonus,
                        dir,
                        0f,
                        ecologyBonus,
                        aeratedFrontierBonus,
                        crustwardTropismBonus,
                        compactionPressureBonus,
                        detritalEnzymesBonus,
                        detritalEnzymesDenseDeadMatterBonus,
                        toxinMarginBonus,
                        toxinborneSeedingBonus));
                }
            }
            return targets;
        }

        private static bool AttemptStandardOrSurgeGrowth(
            GameBoard board,
            Player owner,
            int sourceTileId,
            GrowthTarget target,
            Random rng,
            ISimulationObserver observer,
            ISet<int> crustwardAutomaticGrowthUsedPlayerIds)
        {
            if (target.Tile.IsOccupied || board.IsTileBlockedForOccupation(target.Tile.TileId)) return false;
            if (target.AeratedFrontierBonus > 0f)
            {
                observer.RecordAeratedFrontierAttempt(owner.PlayerId);
            }
            if (target.CrustwardTropismBonus > 0f)
                observer.RecordCrustwardTropismAttempt(owner.PlayerId);
            if (target.DetritalEnzymesBonus > 0f)
                observer.RecordDetritalEnzymesAttempt(owner.PlayerId);
            if (target.DetritalEnzymesDenseDeadMatterBonus > 0f)
                observer.RecordDetritalEnzymesDenseDeadMatterAttempt(owner.PlayerId);
            if (target.ToxinMarginBonus > 0f)
                observer.RecordToxinMarginAttempt(owner.PlayerId);
            if (target.ToxinborneSeedingBonus > 0f)
                observer.RecordToxinborneSeedingAttempt(owner.PlayerId);

            double roll = rng.NextDouble();
            var (edgeMultiplier, baseChance) = GetPerimeterProliferatorContext(board, owner, sourceTileId, target);
            GrowthSource growthSource = target.DiagonalDirection.HasValue
                ? GrowthSource.TendrilOutgrowth
                : GrowthSource.HyphalOutgrowth;
            var sourceTile = board.GetTileById(sourceTileId);
            bool useAutomaticCrustwardGrowth = sourceTile != null
                && !crustwardAutomaticGrowthUsedPlayerIds.Contains(owner.PlayerId)
                && SubstrateEcologyMutationProcessor.IsCrustwardTropismAutomaticCrustArrival(owner, board, sourceTile, target.Tile);
            if (useAutomaticCrustwardGrowth)
            {
                crustwardAutomaticGrowthUsedPlayerIds.Add(owner.PlayerId);
                if (TryGrowWithCorrectSource(board, owner.PlayerId, sourceTileId, target.Tile.TileId, growthSource))
                {
                    observer.RecordCrustwardTropismAutomaticGrowth(owner.PlayerId);
                    ResolveToxinborneSeedingAfterGrowth(board, owner, target, rng, observer);
                    if (target.DiagonalDirection.HasValue)
                        observer.RecordTendrilGrowth(owner.PlayerId, target.DiagonalDirection.Value);
                    else
                        observer.RecordStandardGrowth(owner.PlayerId);
                    return true;
                }
            }

            if (target.SurgeBonus > 0f && target.DiagonalDirection == null)
            {
                if (roll < target.Chance)
                {
                    if (TryGrowWithCorrectSource(board, owner.PlayerId, sourceTileId, target.Tile.TileId, growthSource))
                    {
                        MaybeRecordPerimeterProliferatorGrowth(observer, owner.PlayerId, edgeMultiplier, roll, baseChance, target.Chance);
                        MaybeRecordAeratedFrontierGrowth(observer, owner.PlayerId, roll, target);
                        MaybeRecordCrustwardTropismGrowth(observer, owner.PlayerId, roll, target);
                        MaybeRecordDetritalEnzymesGrowth(observer, owner.PlayerId, roll, target);
                        MaybeRecordToxinMarginGrowth(observer, owner.PlayerId, roll, target);
                        MaybeRecordToxinborneSeedingGrowth(observer, owner.PlayerId, roll, target);
                        ResolveToxinborneSeedingAfterGrowth(board, owner, target, rng, observer);
                        observer.RecordStandardGrowth(owner.PlayerId);
                        return true;
                    }
                }
                else if (roll < target.Chance + target.SurgeBonus)
                {
                    if (TryGrowWithCorrectSource(board, owner.PlayerId, sourceTileId, target.Tile.TileId, GrowthSource.HyphalSurge))
                    {
                        observer.RecordHyphalSurgeGrowth(owner.PlayerId);
                        return true;
                    }
                }
            }
            else
            {
                if (roll < target.Chance)
                {
                    if (TryGrowWithCorrectSource(board, owner.PlayerId, sourceTileId, target.Tile.TileId, growthSource))
                    {
                        MaybeRecordPerimeterProliferatorGrowth(observer, owner.PlayerId, edgeMultiplier, roll, baseChance, target.Chance);
                        MaybeRecordAeratedFrontierGrowth(observer, owner.PlayerId, roll, target);
                        MaybeRecordCrustwardTropismGrowth(observer, owner.PlayerId, roll, target);
                        MaybeRecordDetritalEnzymesGrowth(observer, owner.PlayerId, roll, target);
                        MaybeRecordToxinMarginGrowth(observer, owner.PlayerId, roll, target);
                        MaybeRecordToxinborneSeedingGrowth(observer, owner.PlayerId, roll, target);
                        ResolveToxinborneSeedingAfterGrowth(board, owner, target, rng, observer);
                        if (target.DiagonalDirection.HasValue)
                            observer.RecordTendrilGrowth(owner.PlayerId, target.DiagonalDirection.Value);
                        return true;
                    }
                }
            }
            return false;
        }

        private static void MaybeRecordAeratedFrontierGrowth(
            ISimulationObserver observer,
            int playerId,
            double roll,
            GrowthTarget target)
        {
            if (target.AeratedFrontierBonus > 0f
                && roll >= target.Chance - target.AeratedFrontierBonus
                && roll < target.Chance)
            {
                observer.RecordAeratedFrontierBonusGrowth(playerId);
            }
        }

        private static void MaybeRecordCrustwardTropismGrowth(
            ISimulationObserver observer,
            int playerId,
            double roll,
            GrowthTarget target)
        {
            if (target.CrustwardTropismBonus > 0f
                && roll >= target.Chance - target.CrustwardTropismBonus
                && roll < target.Chance)
            {
                observer.RecordCrustwardTropismBonusGrowth(playerId);
            }
        }

        private static void MaybeRecordDetritalEnzymesGrowth(
            ISimulationObserver observer,
            int playerId,
            double roll,
            GrowthTarget target)
        {
            if (target.DetritalEnzymesBonus > 0f
                && roll >= target.Chance - target.DetritalEnzymesBonus
                && roll < target.Chance)
            {
                observer.RecordDetritalEnzymesBonusGrowth(playerId);
            }

            if (target.DetritalEnzymesDenseDeadMatterBonus > 0f
                && roll >= target.Chance - target.DetritalEnzymesDenseDeadMatterBonus
                && roll < target.Chance)
            {
                observer.RecordDetritalEnzymesDenseDeadMatterBonusGrowth(playerId);
            }
        }

        private static void MaybeRecordToxinMarginGrowth(
            ISimulationObserver observer,
            int playerId,
            double roll,
            GrowthTarget target)
        {
            if (target.ToxinMarginBonus > 0f
                && roll >= target.Chance - target.ToxinMarginBonus
                && roll < target.Chance)
            {
                observer.RecordToxinMarginBonusGrowth(playerId);
            }
        }

        private static void ResolveToxinborneSeedingAfterGrowth(
            GameBoard board,
            Player owner,
            GrowthTarget target,
            Random rng,
            ISimulationObserver observer)
        {
            if (target.ToxinborneSeedingBonus > 0f)
            {
                SubstrateEcologyMutationProcessor.TryResolveToxinborneSeeding(
                    board,
                    owner,
                    target.Tile.TileId,
                    rng,
                    observer);
            }
        }

        private static void MaybeRecordToxinborneSeedingGrowth(
            ISimulationObserver observer,
            int playerId,
            double roll,
            GrowthTarget target)
        {
            if (target.ToxinborneSeedingBonus > 0f
                && roll >= target.Chance - target.ToxinborneSeedingBonus
                && roll < target.Chance)
            {
                observer.RecordToxinborneSeedingBonusGrowth(playerId);
            }
        }

        /// <summary>
        /// Helper method to grow a cell with the correct GrowthSource, creating the cell manually
        /// to ensure the source is set correctly before PlaceFungalCell is called.
        /// </summary>
        private static bool TryGrowWithCorrectSource(GameBoard board, int playerId, int sourceTileId, int targetTileId, GrowthSource growthSource)
        {
            var targetTile = board.GetTileById(targetTileId);
            if (targetTile == null || targetTile.IsOccupied || targetTile.IsResistant || board.IsTileBlockedForOccupation(targetTileId))
                return false;

            // Create new cell but DO NOT place directly on the tile here.
            // Let GameBoard.PlaceFungalCell perform authoritative placement so
            // it can correctly detect isNew == true and raise OnCellColonized.
            var newCell = new FungalCell(ownerPlayerId: playerId, tileId: targetTileId, source: growthSource, lastOwnerPlayerId: null);

            // Removed: targetTile.PlaceFungalCell(newCell); (caused isNew=false in GameBoard.PlaceFungalCell)
            board.PlaceFungalCell(newCell);
            if (growthSource == GrowthSource.HyphalOutgrowth || growthSource == GrowthSource.TendrilOutgrowth)
            {
                board.OnHyphalGrowthVisualized(playerId, sourceTileId, targetTileId);
            }
            return true;
        }

        private static void MaybeRecordPerimeterProliferatorGrowth(
            ISimulationObserver observer,
            int playerId,
            float edgeMultiplier,
            double roll,
            float baseChance,
            float targetChance)
        {
            if (edgeMultiplier > 1f && roll >= baseChance && roll < targetChance)
            {
                observer.RecordPerimeterProliferatorGrowth(playerId);
            }
        }

        // Returns (edgeMultiplier, baseChance)
        private static (float edgeMultiplier, float baseChance) GetPerimeterProliferatorContext(
            GameBoard board,
            Player owner,
            int sourceTileId,
            GrowthTarget target)
        {
            float edgeMultiplier = 1f;
            float baseChance = target.Chance;
            bool hasPerimeterProliferator = owner.PlayerMycovariants.Any(m => m.MycovariantId == MycovariantIds.PerimeterProliferatorId);
            if (hasPerimeterProliferator)
            {
                var sourceTile = board.GetTileById(sourceTileId);
                if (sourceTile != null)
                {
                    bool isWithinEdgeDistance = BoardUtilities.IsWithinEdgeDistance(
                        sourceTile, board, MycovariantGameBalance.PerimeterProliferatorEdgeDistance);
                    edgeMultiplier = isWithinEdgeDistance ? MycovariantGameBalance.PerimeterProliferatorEdgeMultiplier : 1f;
                }
            }
            if (edgeMultiplier > 1f)
            {
                baseChance = target.Chance / edgeMultiplier;
            }
            return (edgeMultiplier, baseChance);
        }

        private static bool AttemptCreepingMold(
            GameBoard board,
            Player owner,
            FungalCell sourceCell,
            BoardTile sourceTile,
            BoardTile targetTile,
            Random rng,
            ISimulationObserver observer)
        {
            if (GrowthMutationProcessor.TryCreepingMoldMove(owner, sourceCell, sourceTile, targetTile, rng, board, observer))
            {
                observer.RecordCreepingMoldMove(owner.PlayerId);
                return true;
            }
            return false;
        }

        private static bool AttemptNecrohyphalInfiltration(
            GameBoard board,
            BoardTile sourceTile,
            FungalCell sourceCell,
            Player owner,
            Random rng,
            ISimulationObserver observer)
        {
            return CellularResilienceMutationProcessor.TryNecrohyphalInfiltration(board, sourceTile, sourceCell, owner, rng, observer);
        }


        /// <summary>
        /// Helper for shuffling lists with a given RNG.
        /// </summary>
        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int j = rng.Next(i, list.Count);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Encapsulates a potential growth target, including diagonal direction if applicable.
        /// </summary>
        private sealed class GrowthTarget
        {
            public BoardTile Tile { get; }
            public float Chance { get; }
            public DiagonalDirection? DiagonalDirection { get; }
            public float SurgeBonus { get; } // Only for orthogonal targets
            public float EcologyBonus { get; }
            public float AeratedFrontierBonus { get; }
            public float CrustwardTropismBonus { get; }
            public float CompactionPressureBonus { get; }
            public float DetritalEnzymesBonus { get; }
            public float DetritalEnzymesDenseDeadMatterBonus { get; }
            public float ToxinMarginBonus { get; }
            public float ToxinborneSeedingBonus { get; }

            public GrowthTarget(
                BoardTile tile,
                float chance,
                DiagonalDirection? dir,
                float surgeBonus,
                float ecologyBonus,
                float aeratedFrontierBonus,
                float crustwardTropismBonus,
                float compactionPressureBonus,
                float detritalEnzymesBonus,
                float detritalEnzymesDenseDeadMatterBonus,
                float toxinMarginBonus,
                float toxinborneSeedingBonus)
            {
                Tile = tile;
                Chance = chance;
                DiagonalDirection = dir;
                SurgeBonus = surgeBonus;
                EcologyBonus = ecologyBonus;
                AeratedFrontierBonus = aeratedFrontierBonus;
                CrustwardTropismBonus = crustwardTropismBonus;
                CompactionPressureBonus = compactionPressureBonus;
                DetritalEnzymesBonus = detritalEnzymesBonus;
                DetritalEnzymesDenseDeadMatterBonus = detritalEnzymesDenseDeadMatterBonus;
                ToxinMarginBonus = toxinMarginBonus;
                ToxinborneSeedingBonus = toxinborneSeedingBonus;
            }
        }

        private static float GetEdgeGrowthMultiplier(Player owner, BoardTile sourceTile, GameBoard board)
        {
            bool hasPerimeterProliferator = owner.PlayerMycovariants.Any(m => m.MycovariantId == MycovariantIds.PerimeterProliferatorId);
            bool isWithinEdgeDistance = BoardUtilities.IsWithinEdgeDistance(
                sourceTile, board, MycovariantGameBalance.PerimeterProliferatorEdgeDistance);
            return (hasPerimeterProliferator && isWithinEdgeDistance)
                ? MycovariantGameBalance.PerimeterProliferatorEdgeMultiplier
                : 1f;
        }
    }
}
