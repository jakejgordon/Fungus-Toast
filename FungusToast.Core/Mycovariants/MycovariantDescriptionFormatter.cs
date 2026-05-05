using FungusToast.Core.Config;
using FungusToast.Core.Players;

namespace FungusToast.Core.Mycovariants
{
    public static class MycovariantDescriptionFormatter
    {
        private const int LaterDraftScalingStartRound = 15;
        private const int LaterDraftScalingRoundsPerBonusTile = 5;

        public static string GetDraftPreviewDescription(Mycovariant mycovariant, int currentRound)
        {
            if (mycovariant == null)
            {
                return string.Empty;
            }

            return mycovariant.Id switch
            {
                var id when IsCornerConduit(id) => BuildCornerConduitDescription(
                    GetEffectiveConduitTilesPerPhase(mycovariant.Id, currentRound)),
                var id when IsAggressotropicConduit(id) => BuildAggressotropicConduitDescription(
                    GetEffectiveConduitTilesPerPhase(mycovariant.Id, currentRound)),
                _ => mycovariant.Description,
            };
        }

        public static string GetOwnedTooltipDescription(PlayerMycovariant playerMycovariant)
        {
            if (playerMycovariant?.Mycovariant == null)
            {
                return string.Empty;
            }

            return playerMycovariant.Mycovariant.Id switch
            {
                var id when IsCornerConduit(id) => BuildCornerConduitDescription(
                    GetEffectiveConduitTilesPerPhase(playerMycovariant)),
                var id when IsAggressotropicConduit(id) => BuildAggressotropicConduitDescription(
                    GetEffectiveConduitTilesPerPhase(playerMycovariant)),
                _ => playerMycovariant.Mycovariant.Description,
            };
        }

        public static int GetEffectiveConduitTilesPerPhase(PlayerMycovariant playerMycovariant)
        {
            if (playerMycovariant?.Mycovariant == null)
            {
                return 0;
            }

            return GetEffectiveConduitTilesPerPhase(playerMycovariant.MycovariantId, playerMycovariant.DraftedRound);
        }

        public static int GetEffectiveConduitTilesPerPhase(int mycovariantId, int draftedRound)
            => GetBaseConduitTilesPerPhase(mycovariantId) + GetDraftRoundBonusTiles(draftedRound);

        public static int GetBaseConduitTilesPerPhase(int mycovariantId)
        {
            return mycovariantId switch
            {
                var id when id == MycovariantIds.CornerConduitIId => MycovariantGameBalance.CornerConduitIReplacementsPerPhase,
                var id when id == MycovariantIds.CornerConduitIIId => MycovariantGameBalance.CornerConduitIIReplacementsPerPhase,
                var id when id == MycovariantIds.CornerConduitIIIId => MycovariantGameBalance.CornerConduitIIIReplacementsPerPhase,
                var id when id == MycovariantIds.AggressotropicConduitIId => MycovariantGameBalance.AggressotropicConduitIReplacementsPerPhase,
                var id when id == MycovariantIds.AggressotropicConduitIIId => MycovariantGameBalance.AggressotropicConduitIIReplacementsPerPhase,
                var id when id == MycovariantIds.AggressotropicConduitIIIId => MycovariantGameBalance.AggressotropicConduitIIIReplacementsPerPhase,
                _ => 0,
            };
        }

        public static int GetDraftRoundBonusTiles(int round)
        {
            if (round <= LaterDraftScalingStartRound)
            {
                return 0;
            }

            return (round - LaterDraftScalingStartRound) / LaterDraftScalingRoundsPerBonusTile;
        }

        public static string BuildGenericCornerConduitDescription(int tilesPerPhase)
            => BuildCornerConduitDescription(tilesPerPhase);

        public static string BuildGenericAggressotropicConduitDescription(int tilesPerPhase)
            => BuildAggressotropicConduitDescription(tilesPerPhase);

        public static bool IsCornerConduit(int mycovariantId)
            => mycovariantId == MycovariantIds.CornerConduitIId
                || mycovariantId == MycovariantIds.CornerConduitIIId
                || mycovariantId == MycovariantIds.CornerConduitIIIId;

        public static bool IsAggressotropicConduit(int mycovariantId)
            => mycovariantId == MycovariantIds.AggressotropicConduitIId
                || mycovariantId == MycovariantIds.AggressotropicConduitIIId
                || mycovariantId == MycovariantIds.AggressotropicConduitIIIId;

        private static string BuildCornerConduitDescription(int tilesPerPhase)
            => $"Before each growth phase, grow up to {tilesPerPhase} tiles from your starting spore toward the nearest corner. Skips your living cells and enemy Resistant cells. Effect is more powerful if drafted later in the game.";

        private static string BuildAggressotropicConduitDescription(int tilesPerPhase)
            => $"Before each growth phase, grow up to {tilesPerPhase} tiles from your starting spore toward the enemy starting spore with the most living cells (random tie-break), reclaiming dead cells along the path. The last cell placed becomes Resistant. Skips your living cells and enemy Resistant cells. Stacks with other Aggressotropic Mycovariants. Effect is more powerful if drafted later in the game.";
    }
}
