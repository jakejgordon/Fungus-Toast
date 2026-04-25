using FungusToast.Core.Campaign;

namespace FungusToast.Core.Tests.Campaign;

public class AdaptationDraftEligibilityTests
{
    [Fact]
    public void GetEligibleAdaptations_excludes_locked_adaptations_until_they_are_unlocked()
    {
        var eligible = CampaignDraftEligibility.GetEligibleAdaptations(
            AdaptationRepository.All,
            activeAdaptationIds: Array.Empty<string>(),
            unlockedAdaptationIds: Array.Empty<string>(),
            currentUnlockLevel: 1);

        Assert.DoesNotContain(eligible, adaptation => adaptation.Id == AdaptationIds.HyphalPriming);
    }

    [Fact]
    public void GetEligibleAdaptations_includes_locked_adaptations_after_unlock()
    {
        var eligible = CampaignDraftEligibility.GetEligibleAdaptations(
            AdaptationRepository.All,
            activeAdaptationIds: Array.Empty<string>(),
            unlockedAdaptationIds: new[] { AdaptationIds.HyphalPriming },
            currentUnlockLevel: 1);

        Assert.Contains(eligible, adaptation => adaptation.Id == AdaptationIds.HyphalPriming);
    }

    [Fact]
    public void GetEligibleAdaptations_excludes_starting_and_already_selected_adaptations()
    {
        var eligible = CampaignDraftEligibility.GetEligibleAdaptations(
            AdaptationRepository.All,
            activeAdaptationIds: new[] { AdaptationIds.ConidialRelay },
            unlockedAdaptationIds: Array.Empty<string>(),
            currentUnlockLevel: 1);

        Assert.DoesNotContain(eligible, adaptation => adaptation.Id == AdaptationIds.ObliqueFilament);
        Assert.DoesNotContain(eligible, adaptation => adaptation.Id == AdaptationIds.ConidialRelay);
    }
}