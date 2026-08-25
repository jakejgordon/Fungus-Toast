using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using FungusToast.Core.Mutations;
using FungusToast.Core.Phases;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Mutations;

public class MycotoxinFissionMutationTests
{
    [Fact]
    public void ToxinborneSeeding_is_a_tier5_ecology_mutation_with_only_necrophytic_and_sporicidal_bloom_prerequisites()
    {
        var mutation = Assert.IsType<Mutation>(MutationRegistry.GetById(MutationIds.MycotoxinFission));

        Assert.Equal("Toxinborne Seeding", mutation.Name);
        Assert.Equal(MutationCategory.SubstrateEcology, mutation.Category);
        Assert.Equal(MutationTier.Tier5, mutation.Tier);
        Assert.Equal(MutationType.ToxinborneSeedingGrowthChance, mutation.Type);
        Assert.Equal(GameBalance.ToxinborneSeedingMaxLevel, mutation.MaxLevel);
        Assert.Equal(2, mutation.Prerequisites.Count);
        Assert.Contains(mutation.Prerequisites, prerequisite => prerequisite.MutationId == MutationIds.NecrophyticBloom && prerequisite.RequiredLevel == 1);
        Assert.Contains(mutation.Prerequisites, prerequisite => prerequisite.MutationId == MutationIds.SporicidalBloom && prerequisite.RequiredLevel == 1);
    }

    [Fact]
    public void ToxinborneSeeding_grants_ten_percent_per_level_only_next_to_a_friendly_toxin()
    {
        var (board, player, enemy, colonizedTile) = CreateBoard();
        player.SetMutationLevel(MutationIds.MycotoxinFission, 2, currentRound: 1);
        ToxinHelper.ConvertToToxin(board, tileId: 8, GrowthSource.MycotoxinTracer, player);

        Assert.True(SubstrateEcologyMutationProcessor.QualifiesForToxinborneSeeding(player, board, colonizedTile));
        Assert.Equal(0.20f, SubstrateEcologyMutationProcessor.GetToxinborneSeedingGrowthBonus(player, board, colonizedTile));

        ToxinHelper.ConvertToToxin(board, tileId: 8, GrowthSource.MycotoxinTracer, enemy);

        Assert.False(SubstrateEcologyMutationProcessor.QualifiesForToxinborneSeeding(player, board, colonizedTile));
        Assert.Equal(0f, SubstrateEcologyMutationProcessor.GetToxinborneSeedingGrowthBonus(player, board, colonizedTile));
    }

    [Fact]
    public void ToxinborneSeeding_bonus_can_make_an_otherwise_failed_growth_succeed_and_relocate_a_toxin_with_a_carried_cell()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2, permanentlyBlockedTileIds: new[] { 7, 11, 17 });
        var player = new Player(0, "Player", PlayerTypeEnum.AI);
        var enemy = new Player(1, "Enemy", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 12, GrowthSource.InitialSpore));
        Assert.True(board.SpawnSporeForPlayer(enemy, tileId: 18, GrowthSource.InitialSpore));
        player.SetMutationLevel(MutationIds.MycotoxinFission, 2, currentRound: 1);
        ToxinHelper.ConvertToToxin(board, tileId: 8, GrowthSource.MycotoxinTracer, player);
        var observer = new SeedingObserver();

        GrowthEngine.ExecuteGrowthCycle(
            board,
            board.Players,
            new FixedRollRandom(GameBalance.BaseGrowthChance + 0.15f),
            new RoundContext(),
            observer);

        Assert.Null(board.GetTileById(13)!.FungalCell);
        Assert.Null(board.GetTileById(8)!.FungalCell);
        FungalCell toxin = Assert.Single(board.AllToxinFungalCells());
        FungalCell carriedCell = Assert.Single(board.AllLivingFungalCells().Where(cell => cell.SourceOfGrowth == GrowthSource.ToxinborneSeeding));
        Assert.Contains(board.GetOrthogonalNeighbors(toxin.TileId), tile => tile.TileId == carriedCell.TileId);
        Assert.Equal(1, observer.Attempts);
        Assert.Equal(1, observer.BonusGrowths);
        Assert.Equal(1, observer.Relocations);
        Assert.Equal(1, observer.CarriedCellLandings);
    }

    [Fact]
    public void ToxinborneSeeding_relocates_one_toxin_preserves_remaining_lifespan_and_moves_the_new_cell_to_its_landing_site()
    {
        var (board, player, _, colonizedTile) = CreateHighCapacityBoard();
        player.SetMutationLevel(MutationIds.MycotoxinFission, GameBalance.ToxinborneSeedingMaxLevel, currentRound: 1);
        ToxinHelper.ConvertToToxin(board, tileId: 18, GrowthSource.MycotoxinTracer, player);
        board.GetTileById(18)!.FungalCell!.SetGrowthCycleAge(2);
        var observer = new SeedingObserver();

        ToxinborneSeedingResult result = SubstrateEcologyMutationProcessor.TryResolveToxinborneSeeding(
            board,
            player,
            colonizedTile.TileId,
            new LowestIndexRandom(),
            observer);

        Assert.True(result.ToxinRelocated);
        Assert.True(result.CarriedCellLanded);
        Assert.Null(board.GetTileById(18)!.FungalCell);
        Assert.Null(board.GetTileById(colonizedTile.TileId)!.FungalCell);
        FungalCell toxin = Assert.Single(board.AllToxinFungalCells());
        FungalCell carriedCell = Assert.Single(board.AllLivingFungalCells().Where(cell => cell.SourceOfGrowth == GrowthSource.ToxinborneSeeding));
        Assert.Equal(GameBalance.DefaultToxinDuration - 2, toxin.ToxinExpirationAge);
        Assert.Contains(board.GetOrthogonalNeighbors(toxin.TileId), tile => tile.TileId == carriedCell.TileId);
        Assert.Equal(1, observer.Relocations);
        Assert.Equal(1, observer.CarriedCellLandings);
    }

    [Fact]
    public void ToxinborneSeeding_leaves_the_original_growth_and_toxin_untouched_when_no_enemy_adjacent_landing_tile_exists()
    {
        var (board, player, _, colonizedTile) = CreateBoard();
        player.SetMutationLevel(MutationIds.MycotoxinFission, 1, currentRound: 1);
        ToxinHelper.ConvertToToxin(board, tileId: 8, GrowthSource.MycotoxinTracer, player);
        var observer = new SeedingObserver();

        ToxinborneSeedingResult result = SubstrateEcologyMutationProcessor.TryResolveToxinborneSeeding(
            board,
            player,
            colonizedTile.TileId,
            new LowestIndexRandom(),
            observer);

        Assert.False(result.ToxinRelocated);
        Assert.False(result.CarriedCellLanded);
        Assert.True(board.GetTileById(8)!.FungalCell!.IsToxin);
        Assert.True(board.GetTileById(colonizedTile.TileId)!.FungalCell!.IsAlive);
        Assert.Equal(0, observer.Relocations);
    }

    private static (GameBoard board, Player player, Player enemy, BoardTile colonizedTile) CreateBoard()
    {
        var board = new GameBoard(width: 5, height: 5, playerCount: 2);
        var player = new Player(0, "Player", PlayerTypeEnum.AI);
        var enemy = new Player(1, "Enemy", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 13, GrowthSource.InitialSpore));
        return (board, player, enemy, board.GetTileById(13)!);
    }

    private static (GameBoard board, Player player, Player enemy, BoardTile colonizedTile) CreateHighCapacityBoard()
    {
        var board = new GameBoard(width: 7, height: 7, playerCount: 2);
        var player = new Player(0, "Player", PlayerTypeEnum.AI);
        var enemy = new Player(1, "Enemy", PlayerTypeEnum.AI);
        board.Players.Add(player);
        board.Players.Add(enemy);
        Assert.True(board.SpawnSporeForPlayer(player, tileId: 25, GrowthSource.InitialSpore));
        Assert.True(board.SpawnSporeForPlayer(enemy, tileId: 3, GrowthSource.InitialSpore));
        Assert.True(board.SpawnSporeForPlayer(enemy, tileId: 7, GrowthSource.InitialSpore));
        Assert.True(board.SpawnSporeForPlayer(enemy, tileId: 43, GrowthSource.InitialSpore));
        Assert.True(board.SpawnSporeForPlayer(enemy, tileId: 47, GrowthSource.InitialSpore));
        return (board, player, enemy, board.GetTileById(25)!);
    }

    private sealed class LowestIndexRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class FixedRollRandom : Random
    {
        private readonly double roll;

        public FixedRollRandom(double roll) => this.roll = roll;

        public override double NextDouble() => roll;
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class SeedingObserver : TestSimulationObserver
    {
        public int Relocations { get; private set; }
        public int CarriedCellLandings { get; private set; }
        public int Attempts { get; private set; }
        public int BonusGrowths { get; private set; }

        public override void RecordToxinborneSeedingAttempt(int playerId) => Attempts++;
        public override void RecordToxinborneSeedingBonusGrowth(int playerId) => BonusGrowths++;

        public override void RecordToxinborneSeeding(int playerId, bool toxinRelocated, bool carriedCellLanded)
        {
            if (toxinRelocated)
            {
                Relocations++;
            }
            if (carriedCellLanded)
            {
                CarriedCellLandings++;
            }
        }
    }
}
