using FungusToast.Core.Board;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class NutrientPatchExclusionTests
{
    [Fact]
    public void Starting_cluster_seeds_and_frontiers_respect_reserved_corridor_tiles()
    {
        var excluded = Enumerable.Range(0,30).SelectMany(y => new[] {y*30+14,y*30+15}).ToHashSet();
        foreach (int seed in Enumerable.Range(0,30))
        {
            var board = new GameBoard(30,30,1);
            var players = new List<Player> { new(0,"Mold",PlayerTypeEnum.AI) };
            board.Players.AddRange(players);
            board.PlaceInitialSpore(0,3,3);
            int placed = NutrientPatchPlacementUtility.PlaceStartingNutrientPatches(board,players,new Random(seed),excludedTileIds:excluded);
            Assert.True(placed > 0);
            Assert.All(excluded,id => Assert.False(board.GetTileById(id)!.HasNutrientPatch));
        }
    }

    [Fact]
    public void Null_exclusions_preserve_existing_rng_and_placement_behavior()
    {
        var a = new GameBoard(20,20,1);
        var b = new GameBoard(20,20,1);
        var players = new List<Player>();
        NutrientPatchPlacementUtility.PlaceStartingNutrientPatches(a,players,new Random(123));
        NutrientPatchPlacementUtility.PlaceStartingNutrientPatches(b,players,new Random(123),excludedTileIds:null);
        Assert.Equal(a.AllTiles().Where(t=>t.HasNutrientPatch).Select(t=>t.TileId),
            b.AllTiles().Where(t=>t.HasNutrientPatch).Select(t=>t.TileId));
    }
}
