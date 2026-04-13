using FungusToast.Core.Campaign;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Campaign;

/// <summary>
/// Tests for the starting-adaptation feature: per-slot adaptation assignment at game init,
/// deduplication, unknown-ID safety, and boss-level seed determinism.
/// </summary>
public class StartingAdaptationTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // TryAddAdaptation — core grant behaviour
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryAddAdaptation_grants_adaptation_to_player()
    {
        var player = MakePlayer();
        var def = RequireAdaptation(AdaptationIds.MycotoxicHalo);

        var result = player.TryAddAdaptation(def);

        Assert.True(result);
        Assert.True(player.HasAdaptation(AdaptationIds.MycotoxicHalo));
    }

    [Fact]
    public void TryAddAdaptation_is_idempotent_duplicate_grants_return_false()
    {
        var player = MakePlayer();
        var def = RequireAdaptation(AdaptationIds.MycotoxicHalo);

        player.TryAddAdaptation(def);
        var second = player.TryAddAdaptation(def);

        Assert.False(second);
        Assert.Single(player.PlayerAdaptations);
    }

    [Fact]
    public void TryAddAdaptation_multiple_distinct_adaptations_all_granted()
    {
        var player = MakePlayer();
        var ids = new[]
        {
            AdaptationIds.MycotoxicHalo,
            AdaptationIds.AegisHyphae,
            AdaptationIds.ApicalYield
        };

        foreach (var id in ids)
            player.TryAddAdaptation(RequireAdaptation(id));

        Assert.Equal(3, player.PlayerAdaptations.Count);
        foreach (var id in ids)
            Assert.True(player.HasAdaptation(id), $"Expected player to have adaptation {id}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AdaptationRepository — unknown IDs
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AdaptationRepository_TryGetById_returns_false_for_unknown_id()
    {
        var found = AdaptationRepository.TryGetById("adaptation_does_not_exist", out var def);

        Assert.False(found);
        Assert.Null(def);
    }

    [Fact]
    public void AdaptationRepository_TryGetById_returns_true_for_every_known_id()
    {
        var knownIds = new[]
        {
            AdaptationIds.ConidialRelay, AdaptationIds.HyphalEconomy,
            AdaptationIds.MycotoxicHalo, AdaptationIds.MycotoxicLash,
            AdaptationIds.RetrogradeBloom, AdaptationIds.AegisHyphae,
            AdaptationIds.SaprophageRing, AdaptationIds.MarginalClamp,
            AdaptationIds.ApicalYield, AdaptationIds.CrustalCallus,
            AdaptationIds.DistalSpore, AdaptationIds.AscusPrimacy,
            AdaptationIds.SporeSalvo, AdaptationIds.VesicleBurst,
            AdaptationIds.HyphalBridge, AdaptationIds.ObliqueFilament,
            AdaptationIds.ThanatrophicRebound, AdaptationIds.ToxinPrimacy,
            AdaptationIds.CentripetalGermination, AdaptationIds.SignalEconomy,
            AdaptationIds.LiminalSporemeal, AdaptationIds.PutrefactiveResilience,
            AdaptationIds.CompoundReserve,
        };

        foreach (var id in knownIds)
        {
            var found = AdaptationRepository.TryGetById(id, out var def);
            Assert.True(found, $"Expected {id} to exist in AdaptationRepository");
            Assert.NotNull(def);
            Assert.Equal(id, def!.Id);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Slot-aligned adaptation application (simulating what GameSimulator does)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Slot_aligned_application_grants_correct_adaptations_per_player()
    {
        var players = new[] { MakePlayer(0), MakePlayer(1), MakePlayer(2) };

        // Slot 0: no adaptations (proxy slot)
        // Slot 1: MycotoxicHalo + AegisHyphae
        // Slot 2: ApicalYield
        var slotAdaptations = new List<List<string>>
        {
            new(),
            new() { AdaptationIds.MycotoxicHalo, AdaptationIds.AegisHyphae },
            new() { AdaptationIds.ApicalYield },
        };

        ApplyStartingAdaptations(players, slotAdaptations);

        Assert.Empty(players[0].PlayerAdaptations);
        Assert.Equal(2, players[1].PlayerAdaptations.Count);
        Assert.True(players[1].HasAdaptation(AdaptationIds.MycotoxicHalo));
        Assert.True(players[1].HasAdaptation(AdaptationIds.AegisHyphae));
        Assert.Single(players[2].PlayerAdaptations);
        Assert.True(players[2].HasAdaptation(AdaptationIds.ApicalYield));
    }

    [Fact]
    public void Slot_aligned_application_ignores_unknown_ids_silently()
    {
        var players = new[] { MakePlayer(0), MakePlayer(1) };

        var slotAdaptations = new List<List<string>>
        {
            new() { "adaptation_does_not_exist", AdaptationIds.AegisHyphae },
            new() { "another_bad_id" },
        };

        ApplyStartingAdaptations(players, slotAdaptations);

        // Only the valid id should have been applied
        Assert.Single(players[0].PlayerAdaptations);
        Assert.True(players[0].HasAdaptation(AdaptationIds.AegisHyphae));
        Assert.Empty(players[1].PlayerAdaptations);
    }

    [Fact]
    public void Slot_aligned_application_tolerates_more_players_than_slots()
    {
        var players = new[] { MakePlayer(0), MakePlayer(1), MakePlayer(2) };

        // Only one slot defined — players[1] and [2] should get nothing
        var slotAdaptations = new List<List<string>>
        {
            new() { AdaptationIds.ConidialRelay },
        };

        ApplyStartingAdaptations(players, slotAdaptations);

        Assert.Single(players[0].PlayerAdaptations);
        Assert.Empty(players[1].PlayerAdaptations);
        Assert.Empty(players[2].PlayerAdaptations);
    }

    [Fact]
    public void Slot_aligned_application_tolerates_more_slots_than_players()
    {
        var players = new[] { MakePlayer(0) };

        var slotAdaptations = new List<List<string>>
        {
            new() { AdaptationIds.ConidialRelay },
            new() { AdaptationIds.AegisHyphae }, // no player for this slot
        };

        // Should not throw
        ApplyStartingAdaptations(players, slotAdaptations);

        Assert.Single(players[0].PlayerAdaptations);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Boss level seed determinism (the seed math from CampaignController)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Boss_selection_seed_is_deterministic_for_same_run_seed_and_level()
    {
        int runSeed = 12345;
        int levelIndex = 14;
        int bossCount = 3;

        int pick1 = PickBossIndex(runSeed, levelIndex, bossCount);
        int pick2 = PickBossIndex(runSeed, levelIndex, bossCount);

        Assert.Equal(pick1, pick2);
    }

    [Fact]
    public void Boss_selection_seed_produces_valid_index_in_range()
    {
        int bossCount = 3;

        foreach (var seed in new[] { 0, 1, 99999, -42, int.MaxValue, int.MinValue })
        {
            int pick = PickBossIndex(seed, levelIndex: 14, bossCount);
            Assert.InRange(pick, 0, bossCount - 1);
        }
    }

    [Fact]
    public void Boss_selection_varies_across_different_run_seeds()
    {
        // With 3 bosses and enough seeds, we should see at least 2 distinct picks
        int bossCount = 3;
        var picks = Enumerable.Range(0, 30)
            .Select(i => PickBossIndex(runSeed: i * 397, levelIndex: 14, bossCount))
            .Distinct()
            .Count();

        Assert.True(picks > 1, "Expected boss selection to vary across different run seeds.");
    }

    [Fact]
    public void Boss_selection_is_independent_for_different_level_indices()
    {
        // Same run seed, different level indices → can produce different picks (they aren't required
        // to be the same, and this just validates the seed derivation doesn't collapse them)
        int runSeed = 20260329;
        int bossCount = 3;

        var picks = Enumerable.Range(0, 10)
            .Select(levelIndex => PickBossIndex(runSeed, levelIndex, bossCount))
            .ToList();

        // At least two distinct values across 10 level indices
        Assert.True(picks.Distinct().Count() > 1,
            "Expected boss selection to vary across different level indices.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static Player MakePlayer(int id = 0) =>
        new Player(playerId: id, playerName: $"Player {id}", playerType: PlayerTypeEnum.AI);

    private static AdaptationDefinition RequireAdaptation(string id)
    {
        Assert.True(AdaptationRepository.TryGetById(id, out var def), $"Adaptation {id} not found.");
        return def!;
    }

    /// <summary>
    /// Mirrors the slot-application logic in GameSimulator.InitializeGame and PlayerInitializer.
    /// </summary>
    private static void ApplyStartingAdaptations(
        IReadOnlyList<Player> players,
        IReadOnlyList<IReadOnlyList<string>> slotAdaptations)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (i >= slotAdaptations.Count) break;
            foreach (var id in slotAdaptations[i])
            {
                if (AdaptationRepository.TryGetById(id, out var def))
                    players[i].TryAddAdaptation(def);
            }
        }
    }

    /// <summary>
    /// Mirrors the boss-index derivation in CampaignController.ResolveBoardPreset.
    /// seed = unchecked((runSeed * 397) ^ levelIndex)
    /// </summary>
    private static int PickBossIndex(int runSeed, int levelIndex, int bossCount)
    {
        int seed = unchecked((runSeed * 397) ^ levelIndex);
        return new Random(seed).Next(bossCount);
    }
}
