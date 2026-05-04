using FungusToast.Core.Config;
using FungusToast.Core.Players;

namespace FungusToast.Core.Mycovariants
{
    public static class MycovariantDescriptionFormatter
    {
        private const int LaterDraftScalingStartLevel = 15;
        private const int LaterDraftScalingLevelsPerBonusTile = 5;

        public static string GetOwnedTooltipDescription(PlayerMycovariant playerMycovariant)
        {
            if (playerMycovariant?.Mycovariant == null)
            {
                return string.Empty;
            }

            return playerMycovariant.Mycovariant.Id switch
            {
                var id when IsCornerConduit(id) => BuildCornerConduitDescription(
                    GetEffectiveConduitTilesPerPhase(playerMycovariant),
                    playerMycovariant.DraftedCampaignLevelDisplay,
                    GetDraftedLevelBonusTiles(playerMycovariant.DraftedCampaignLevelDisplay)),
                var id when IsAggressotropicConduit(id) => BuildAggressotropicConduitDescription(
                    GetEffectiveConduitTilesPerPhase(playerMycovariant),
                    playerMycovariant.DraftedCampaignLevelDisplay,
                    GetDraftedLevelBonusTiles(playerMycovariant.DraftedCampaignLevelDisplay)),
                _ => playerMycovariant.Mycovariant.Description,
            };
        }

        public static int GetEffectiveConduitTilesPerPhase(PlayerMycovariant playerMycovariant)
        {
            if (playerMycovariant?.Mycovariant == null)
            {
                return 0;
            }

            return GetBaseConduitTilesPerPhase(playerMycovariant.MycovariantId)
                + GetDraftedLevelBonusTiles(playerMycovariant.DraftedCampaignLevelDisplay);
        }

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

        public static int GetDraftedLevelBonusTiles(int draftedCampaignLevelDisplay)
        {
            if (draftedCampaignLevelDisplay <= LaterDraftScalingStartLevel)
            {
                return 0;
            }

            return (draftedCampaignLevelDisplay - LaterDraftScalingStartLevel) / LaterDraftScalingLevelsPerBonusTile;
        }

        public static string BuildGenericCornerConduitDescription(int tilesPerPhase)
            => BuildCornerConduitDescription(tilesPerPhase, draftedCampaignLevelDisplay: 0, bonusTiles: 0);

        public static string BuildGenericAggressotropicConduitDescription(int tilesPerPhase)
            => BuildAggressotropicConduitDescription(tilesPerPhase, draftedCampaignLevelDisplay: 0, bonusTiles: 0);

        public static bool IsCornerConduit(int mycovariantId)
            => mycovariantId == MycovariantIds.CornerConduitIId
                || mycovariantId == MycovariantIds.CornerConduitIIId
                || mycovariantId == MycovariantIds.CornerConduitIIIId;

        public static bool IsAggressotropicConduit(int mycovariantId)
            => mycovariantId == MycovariantIds.AggressotropicConduitIId
                || mycovariantId == MycovariantIds.AggressotropicConduitIIId
                || mycovariantId == MycovariantIds.AggressotropicConduitIIIId;

        private static string BuildCornerConduitDescription(int tilesPerPhase, int draftedCampaignLevelDisplay, int bonusTiles)
        {
            string bonusText = BuildDraftedBonusSuffix(draftedCampaignLevelDisplay, bonusTiles);
            return $"Before each growth phase, grow up to {tilesPerPhase} tiles from your starting spore toward the nearest corner. Skips your living cells and enemy Resistant cells. Effect is more powerful if drafted later in the game.{bonusText}";
        }

        private static string BuildAggressotropicConduitDescription(int tilesPerPhase, int draftedCampaignLevelDisplay, int bonusTiles)
        {
            string bonusText = BuildDraftedBonusSuffix(draftedCampaignLevelDisplay, bonusTiles);
            return $"Before each growth phase, grow up to {tilesPerPhase} tiles from your starting spore toward the enemy starting spore with the most living cells (random tie-break). The last cell placed becomes Resistant. Skips your living cells and enemy Resistant cells. Stacks with other Aggressotropic Mycovariants. Effect is more powerful if drafted later in the game.{bonusText}";
        }

        private static string BuildDraftedBonusSuffix(int draftedCampaignLevelDisplay, int bonusTiles)
        {
            return draftedCampaignLevelDisplay > 0 && bonusTiles > 0
                ? $" Locked-in bonus: +{bonusTiles} tile{(bonusTiles == 1 ? string.Empty : "s")} from drafting this on campaign level {draftedCampaignLevelDisplay}."
                : string.Empty;
        }
    }
}
