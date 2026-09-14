using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using FungusToast.Core.Tests.Mutations;

namespace FungusToast.Core.Tests.AI;

public class SurgeOpportunityEvaluatorTests
{
    private static readonly Mutation Autolytic = MutationRegistry.GetById(MutationIds.HyphalSurge)!;
    private static readonly Mutation Chitin = MutationRegistry.GetById(MutationIds.ChitinFortification)!;
    private static readonly Mutation Necrotic = MutationRegistry.GetById(MutationIds.NecroticClearance)!;
    private static readonly Mutation Mimetic = MutationRegistry.GetById(MutationIds.MimeticResilience)!;
    private static readonly Mutation Antagonism = MutationRegistry.GetById(MutationIds.CompetitiveAntagonism)!;

    [Fact]
    public void Autolytic_is_declined_for_a_walled_in_colony()
    {
        var (board, player, enemy) = CreateBoard(width: 8, height: 8);
        // A 4x4 block of the player's cells sealed by a ring of enemy cells: no open growth target anywhere.
        for (int x = 1; x <= 6; x++)
        for (int y = 1; y <= 6; y++)
        {
            bool interior = x >= 2 && x <= 5 && y >= 2 && y <= 5;
            PlaceLiving(board, interior ? player : enemy, x, y);
        }

        var opportunity = SurgeOpportunityEvaluator.Evaluate(player, Autolytic, board);

        Assert.False(opportunity.IsWorthActivating);
        Assert.True(opportunity.Value < 0f, "A sealed colony pays the decay penalty for nothing.");
    }

    [Fact]
    public void Autolytic_is_worth_it_for_a_colony_with_a_wide_open_frontier()
    {
        var (board, player, _) = CreateBoard(width: 12, height: 12);
        // A checkerboard of 20 cells, every one with four open targets.
        int placed = 0;
        for (int x = 1; x < 11 && placed < 20; x += 2)
        for (int y = 1; y < 11 && placed < 20; y += 2)
        {
            PlaceLiving(board, player, x, y);
            placed++;
        }

        var opportunity = SurgeOpportunityEvaluator.Evaluate(player, Autolytic, board);

        Assert.True(opportunity.IsWorthActivating, opportunity.Reason);
        Assert.True(opportunity.IsStrong, opportunity.Reason);
    }

    [Fact]
    public void Autolytic_death_synergy_discount_grows_with_corpse_payoffs_and_is_capped()
    {
        var player = new Player(0, "AI", PlayerTypeEnum.AI);
        Assert.Equal(0f, SurgeOpportunityEvaluator.GetDeathSynergyDiscount(player));

        player.SetMutationLevel(MutationIds.Necrosporulation, GameBalance.NecrosporulationMaxLevel, currentRound: 1);
        float withNecrosporulation = SurgeOpportunityEvaluator.GetDeathSynergyDiscount(player);
        Assert.Equal(GameBalance.NecrosporulationMaxLevel * GameBalance.NecrosporulationEffectPerLevel, withNecrosporulation, precision: 4);

        player.SetMutationLevel(MutationIds.NecrophyticBloom, 1, currentRound: 1);
        player.SetMutationLevel(MutationIds.DetritalEnzymes, 1, currentRound: 1);
        player.SetMutationLevel(MutationIds.RegenerativeHyphae, GameBalance.RegenerativeHyphaeMaxLevel, currentRound: 1);
        Assert.Equal(GameBalance.AiAutolyticMaxDeathSynergyDiscount, SurgeOpportunityEvaluator.GetDeathSynergyDiscount(player));
    }

    [Fact]
    public void Autolytic_synergy_discount_turns_a_penalty_heavy_colony_into_an_opportunity()
    {
        var (board, player, _) = CreateBoard(width: 10, height: 10);
        // A dense 6x6 block: lots of unprotected cells, a small perimeter.
        for (int x = 2; x < 8; x++)
        for (int y = 2; y < 8; y++)
            PlaceLiving(board, player, x, y);

        float before = SurgeOpportunityEvaluator.Evaluate(player, Autolytic, board).Value;
        player.SetMutationLevel(MutationIds.Necrosporulation, GameBalance.NecrosporulationMaxLevel, currentRound: 1);
        player.SetMutationLevel(MutationIds.NecrophyticBloom, 1, currentRound: 1);
        float after = SurgeOpportunityEvaluator.Evaluate(player, Autolytic, board).Value;

        Assert.True(after > before, $"Expected the corpse-synergy discount to raise the value ({before} -> {after}).");
    }

    [Fact]
    public void Chitin_waits_until_the_colony_can_absorb_two_full_rounds_of_fortification()
    {
        var (board, player, _) = CreateBoard(width: 10, height: 10);
        int required = GameBalance.ChitinFortificationCellsPerLevel * GameBalance.AiChitinMinimumFullRoundsOfCapacity;
        for (int i = 0; i < required - 1; i++)
            PlaceLiving(board, player, i, 0);

        Assert.False(SurgeOpportunityEvaluator.Evaluate(player, Chitin, board).IsWorthActivating);

        PlaceLiving(board, player, required - 1, 0);

        Assert.True(SurgeOpportunityEvaluator.Evaluate(player, Chitin, board).IsWorthActivating);
    }

    [Fact]
    public void Chitin_is_worth_more_when_the_colony_is_in_enemy_contact()
    {
        var (quietBoard, quietPlayer, _) = CreateBoard(width: 10, height: 10);
        var (contactBoard, contactPlayer, contactEnemy) = CreateBoard(width: 10, height: 10);
        for (int i = 0; i < 8; i++)
        {
            PlaceLiving(quietBoard, quietPlayer, i, 0);
            PlaceLiving(contactBoard, contactPlayer, i, 0);
            PlaceLiving(contactBoard, contactEnemy, i, 1);
        }

        float quiet = SurgeOpportunityEvaluator.Evaluate(quietPlayer, Chitin, quietBoard).Value;
        float contact = SurgeOpportunityEvaluator.Evaluate(contactPlayer, Chitin, contactBoard).Value;

        Assert.True(contact > quiet, $"Expected enemy contact to raise Chitin's value ({quiet} -> {contact}).");
    }

    [Fact]
    public void Necrotic_is_declined_for_a_single_uncontested_corpse()
    {
        var (board, player, _) = CreateBoard(width: 4, height: 4);
        PlaceLiving(board, player, 0, 0);
        var corpse = PlaceLiving(board, player, 1, 0);
        board.KillFungalCell(corpse, DeathReason.Randomness);

        var opportunity = SurgeOpportunityEvaluator.Evaluate(player, Necrotic, board);

        Assert.False(opportunity.IsWorthActivating, opportunity.Reason);
    }

    [Fact]
    public void Necrotic_is_worth_it_along_a_contested_corpse_front()
    {
        var (board, player, enemy) = CreateBoard(width: 10, height: 3);
        player.SetMutationLevel(MutationIds.NecroticClearance, 1, currentRound: 1);
        for (int x = 0; x < 10; x++)
        {
            PlaceLiving(board, player, x, 0);
            var corpse = PlaceLiving(board, player, x, 1);
            board.KillFungalCell(corpse, DeathReason.Randomness);
            PlaceLiving(board, enemy, x, 2);
        }

        var opportunity = SurgeOpportunityEvaluator.Evaluate(player, Necrotic, board);

        Assert.True(opportunity.IsWorthActivating, opportunity.Reason);
    }

    [Fact]
    public void Mimetic_is_declined_when_the_larger_rival_has_nothing_resistant_to_copy()
    {
        var (board, player, rival) = CreateBoard(width: 8, height: 8);
        PlaceLiving(board, player, 0, 0);
        PlaceLiving(board, player, 1, 0);
        for (int x = 0; x < 8; x++)
            PlaceLiving(board, rival, x, 7);

        var opportunity = SurgeOpportunityEvaluator.Evaluate(player, Mimetic, board);

        Assert.False(opportunity.IsWorthActivating, opportunity.Reason);
        Assert.Equal(0f, opportunity.Value);
    }

    [Fact]
    public void Mimetic_value_scales_with_the_rival_resistant_cells()
    {
        var (board, player, rival) = CreateBoard(width: 8, height: 8);
        PlaceLiving(board, player, 0, 0);
        PlaceLiving(board, player, 1, 0);
        for (int x = 0; x < 8; x++)
        {
            var cell = PlaceLiving(board, rival, x, 7);
            if (x < 3)
                cell.MakeResistant();
        }

        var opportunity = SurgeOpportunityEvaluator.Evaluate(player, Mimetic, board);

        Assert.True(opportunity.IsWorthActivating, opportunity.Reason);
        float expectedPerRound = 1f + 0.95f + 0.90f;
        Assert.Equal(expectedPerRound * Mimetic.SurgeDuration * GameBalance.AiMimeticValuePerPlacement, opportunity.Value, precision: 3);
    }

    [Fact]
    public void Antagonism_needs_toxin_output_to_redirect()
    {
        var (board, player, rival) = CreateBoard(width: 8, height: 8);
        PlaceLiving(board, player, 0, 0);
        for (int x = 0; x < 8; x++)
            PlaceLiving(board, rival, x, 7);

        Assert.False(SurgeOpportunityEvaluator.Evaluate(player, Antagonism, board).IsWorthActivating);

        player.SetMutationLevel(MutationIds.MycotoxinTracer, 15, currentRound: 1);

        Assert.True(SurgeOpportunityEvaluator.Evaluate(player, Antagonism, board).IsWorthActivating);
    }

    [Fact]
    public void Effective_rounds_are_clipped_by_the_round_cap()
    {
        var player = new Player(0, "AI", PlayerTypeEnum.AI);
        int cap = GameBalance.MaxNumberOfRoundsBeforeGameEndTrigger;

        Assert.Equal(Autolytic.SurgeDuration, SurgeOpportunityEvaluator.GetEffectiveRounds(player, Autolytic, cap - Autolytic.SurgeDuration));
        Assert.Equal(1, SurgeOpportunityEvaluator.GetEffectiveRounds(player, Autolytic, cap - 1));
        Assert.Equal(0, SurgeOpportunityEvaluator.GetEffectiveRounds(player, Autolytic, cap));
    }

    [Fact]
    public void Nothing_is_worth_activating_once_no_rounds_remain()
    {
        var (board, player, _) = CreateBoard(width: 12, height: 12);
        for (int x = 1; x < 11; x += 2)
        for (int y = 1; y < 11; y += 2)
            PlaceLiving(board, player, x, y);
        board.RestoreRoundState(GameBalance.MaxNumberOfRoundsBeforeGameEndTrigger, currentGrowthCycle: 0, necrophyticBloomActivated: false, pendingHypervariationDraftPlayerIds: null);

        Assert.False(SurgeOpportunityEvaluator.Evaluate(player, Autolytic, board).IsWorthActivating);
        Assert.False(SurgeOpportunityEvaluator.Evaluate(player, Chitin, board).IsWorthActivating);
    }

    [Fact]
    public void Threshold_rises_with_the_escalating_activation_cost()
    {
        var player = new Player(0, "AI", PlayerTypeEnum.AI);
        float atLevelZero = SurgeOpportunityEvaluator.GetThreshold(player, Autolytic);
        player.SetMutationLevel(MutationIds.HyphalSurge, 5, currentRound: 1);
        float atLevelFive = SurgeOpportunityEvaluator.GetThreshold(player, Autolytic);

        Assert.True(atLevelFive > atLevelZero);
        Assert.Equal(player.GetMutationPointCost(Autolytic) * GameBalance.AiSurgeMinimumValuePerMutationPoint, atLevelFive, precision: 4);
    }

    private static (GameBoard Board, Player Player, Player Enemy) CreateBoard(int width, int height)
    {
        var board = new GameBoard(width, height, playerCount: 2);
        board.RestoreRoundState(10, currentGrowthCycle: 0, necrophyticBloomActivated: false, pendingHypervariationDraftPlayerIds: null);
        var player = new Player(0, "AI", PlayerTypeEnum.AI);
        var enemy = new Player(1, "Rival", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);
        return (board, player, enemy);
    }

    private static FungalCell PlaceLiving(GameBoard board, Player player, int x, int y)
    {
        int tileId = board.GetTile(x, y)!.TileId;
        var cell = new FungalCell(player.PlayerId, tileId, GrowthSource.InitialSpore, lastOwnerPlayerId: null);
        board.PlaceFungalCell(cell);
        player.AddControlledTile(tileId);
        return cell;
    }
}
