using FungusToast.Core.Campaign;
using FungusToast.Core.Mycovariants;

namespace FungusToast.Core.Tests.Campaign;

public class CampaignDraftEligibilityTests
{
    [Fact]
    public void GetEligibleMycovariants_excludes_locked_mycovariants_that_are_not_unlocked()
    {
        var allMycovariants = new List<Mycovariant>
        {
            new() { Id = 1, Name = "Universal", IsLocked = false },
            new() { Id = 2, Name = "Locked", IsLocked = true, RequiredMoldinessUnlockLevel = 1 }
        };

        var eligible = CampaignDraftEligibility.GetEligibleMycovariants(
            allMycovariants,
            unlockedMycovariantIds: Array.Empty<int>(),
            currentUnlockLevel: 0);

        Assert.Contains(eligible, mycovariant => mycovariant.Id == 1);
        Assert.DoesNotContain(eligible, mycovariant => mycovariant.Id == 2);
    }

    [Fact]
    public void GetEligibleMycovariants_includes_locked_mycovariants_when_permanently_unlocked()
    {
        var allMycovariants = new List<Mycovariant>
        {
            new() { Id = 2, Name = "Locked", IsLocked = true, RequiredMoldinessUnlockLevel = 1 }
        };

        var eligible = CampaignDraftEligibility.GetEligibleMycovariants(
            allMycovariants,
            unlockedMycovariantIds: new[] { 2 },
            currentUnlockLevel: 1);

        Assert.Contains(eligible, mycovariant => mycovariant.Id == 2);
    }

    [Fact]
    public void GetEligibleMycovariants_includes_forced_locked_mycovariant_even_when_not_unlocked()
    {
        var allMycovariants = new List<Mycovariant>
        {
            new() { Id = MycovariantIds.PlasmidBountyId, Name = "Plasmid Bounty I", IsLocked = false },
            new() { Id = MycovariantIds.SporophoreDecoyId, Name = "Sporophore Decoy", IsLocked = true, RequiredMoldinessUnlockLevel = 1 }
        };

        var eligible = CampaignDraftEligibility.GetEligibleMycovariants(
            allMycovariants,
            unlockedMycovariantIds: Array.Empty<int>(),
            currentUnlockLevel: 0,
            forcedMycovariantId: MycovariantIds.SporophoreDecoyId);

        Assert.Contains(eligible, mycovariant => mycovariant.Id == MycovariantIds.PlasmidBountyId);
        Assert.Contains(eligible, mycovariant => mycovariant.Id == MycovariantIds.SporophoreDecoyId);
        Assert.Equal(2, eligible.Count);
    }
}