using System.Globalization;
using System.Text.RegularExpressions;
using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Players;
using FungusToast.Core.Persistence;

namespace FungusToast.Core.Tests.Board;
public class CentralRotLayoutTests
{
    private static readonly Lazy<int[]> Blocked = new(ReadCanonicalBlockedMask);
    private static GameBoard CreateBoard() => new(90, 90, 7, Blocked.Value);
    private static int Id(int x, int y) => y * 90 + x;

    [Fact]
    public void Island_is_twenty_percent_and_leaves_a_connected_outer_route()
    {
        var board = CreateBoard(); var plan = CentralRotLayout.Build(board);
        Assert.Equal(5726, board.PlayableTileCount); Assert.Equal(1145, plan.Count);
        Assert.Contains(Id(45,45), plan);
        Assert.All(plan, id => Assert.False(board.IsPlayableEdgeTile(id)));
        Assert.Equal(plan, CentralRotLayout.Build(board)); Assert.Empty(board.RotTileIds);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)plan).Add(0));
        CentralRotLayout.Place(board); Assert.Equal(plan, board.RotTileIds.OrderBy(id => id));
        foreach (bool diagonal in new[] { false, true })
        {
            var open = board.AllTiles().Where(t => !t.IsBlocked && !t.HasRot).Select(t => t.TileId).ToHashSet();
            var reached = new HashSet<int> { open.Min() }; var queue = new Queue<int>(reached);
            while (queue.Count > 0)
                foreach (var n in diagonal ? board.GetAdjacentTiles(queue.Dequeue()) : board.GetOrthogonalNeighbors(queue.Dequeue()))
                    if (open.Contains(n.TileId) && reached.Add(n.TileId)) queue.Enqueue(n.TileId);
            Assert.Equal(open.Count, reached.Count);
        }
    }

    [Fact]
    public void Real_starting_placement_preserves_perks_and_keeps_every_spore_outside_rot()
    {
        Assert.True(AdaptationRepository.TryGetById(AdaptationIds.CentripetalGermination, out var adaptation));
        for (int loadout = 0; loadout < 128; loadout++)
        {
            var board = CreateBoard(); CentralRotLayout.Place(board);
            var players = Enumerable.Range(0,7).Select(i => new Player(i, "P"+i, i==0 ? PlayerTypeEnum.Human : PlayerTypeEnum.AI)).ToList();
            board.Players.AddRange(players);
            for (int i=0;i<7;i++) if ((loadout & (1<<i))!=0) Assert.True(players[i].TryAddAdaptation(adaptation));
            Assert.True(CampaignBoardStartingPositionCatalog.TryGetMetadata("Campaign7", 7, out var metadata));
            var humanStart = metadata.Entries[loadout % metadata.Entries.Count];
            StartingSporeUtility.PlaceStartingSpores(board,players,new Random(loadout),edgeOffsets:new[] {0,8,0,1,3,0,2},
                preferredPositionsByPlayerId:new Dictionary<int,(int x,int y)> { [0] = (humanStart.X,humanStart.Y) },
                enforceMinimumPlayableEdgeDistanceForPreferredPositions:true);
            Assert.All(players,p => { Assert.NotNull(p.StartingTileId); Assert.False(board.GetTileById(p.StartingTileId!.Value)!.HasRot); });
            Assert.Equal(7,players.Select(p=>p.StartingTileId).Distinct().Count());
        }
    }

    [Fact]
    public void Unsupported_dimensions_fail_explicitly() => Assert.Throws<ArgumentException>(() => CentralRotLayout.Build(new GameBoard(40,40,2)));

    [Fact]
    public void Nutrient_startup_and_checkpoint_roundtrip_preserve_island_without_overlaps()
    {
        var board = CreateBoard(); CentralRotLayout.Place(board);
        var players = Enumerable.Range(0,7).Select(i => new Player(i,"P"+i,i==0?PlayerTypeEnum.Human:PlayerTypeEnum.AI)).ToList();
        board.Players.AddRange(players);
        StartingSporeUtility.PlaceStartingSpores(board,players,new Random(42));
        Assert.True(NutrientPatchPlacementUtility.PlaceStartingNutrientPatches(board,players,new Random(43)) > 0);
        Assert.All(board.RotTileIds,id => {
            var tile = board.GetTileById(id)!;
            Assert.False(tile.HasNutrientPatch); Assert.Null(tile.FungalCell);
        });
        var snapshot = RoundStartRuntimeSnapshotFactory.Export(board);
        var (restored, _) = RoundStartRuntimeSnapshotFactory.Restore(snapshot);
        Assert.Equal(board.RotTileIds.OrderBy(id=>id),restored.RotTileIds.OrderBy(id=>id));
        Assert.Equal(board.RotAdjacentDeathChance,restored.RotAdjacentDeathChance);
        Assert.Equal(snapshot.NutrientPatches.Count,RoundStartRuntimeSnapshotFactory.Export(restored).NutrientPatches.Count);
        Assert.Equal(players.Select(p=>p.StartingTileId),restored.Players.Select(p=>p.StartingTileId));
    }

    private static int[] ReadCanonicalBlockedMask()
    {
        const string relative = "FungusToast.Unity/Assets/Configs/Toast Configs/ToastBoardMedium.asset";
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, relative))) root = root.Parent;
        Assert.NotNull(root);
        string asset = File.ReadAllText(Path.Combine(root!.FullName, relative));
        const string sprite = "9a53239843f1dc34993dd627b7a63704";
        var sizeOverride = Regex.Match(asset, @"- minBoardWidth: 90\r?\n(?<body>.*?)(?=\r?\n  - minBoardWidth:)", RegexOptions.Singleline).Groups["body"].Value;
        Assert.Contains(sprite, sizeOverride);
        Assert.Contains("backgroundMaxTileClipFraction: 0", sizeOverride);
        Assert.Contains("backgroundTileClipSampleResolution: 5", sizeOverride);
        Assert.Contains("backgroundMinTileCoverage: 0.12", sizeOverride);
        Assert.Contains("composeSafeAreaWithBoardBoundsMetadata: 0", sizeOverride);
        string metadata = asset[asset.LastIndexOf("  - backgroundSprite: {fileID: 21300000, guid: " + sprite, StringComparison.Ordinal)..];
        metadata = metadata.Split("\n  - backgroundSprite:", 2, StringSplitOptions.None)[0];
        Assert.Contains("hasPlayableOutline: 1", metadata);
        Assert.Contains("bakedBlockedTileMasks: []", metadata);
        string bounds = Regex.Match(metadata, @"boardBoundsNormalized:(?<body>.*?)(?=\r?\n    [a-zA-Z])", RegexOptions.Singleline).Groups["body"].Value;
        double Number(string key) => double.Parse(Regex.Match(bounds, @"(?m)^      " + key + @": ([0-9.]+)").Groups[1].Value, CultureInfo.InvariantCulture);
        double bx = Number("x"), by = Number("y"), bw = Number("width"), bh = Number("height");
        var outline = Regex.Matches(metadata, @"- \{x: ([0-9.]+), y: ([0-9.]+)\}").Select(m =>
            (x: double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), y: double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture))).ToArray();
        Assert.InRange(outline.Length, 3, 1000);
        bool Inside(double x, double y)
        {
            bool inside = false;
            for (int i = 0; i < outline.Length; i++)
            {
                var a = outline[i];
                var b = outline[(i + 1) % outline.Length];
                if ((a.y <= y) != (b.y <= y) && a.x + (y - a.y) / (b.y - a.y) * (b.x - a.x) <= x)
                    inside = !inside;
            }
            return inside;
        }
        var blocked = new List<int>();
        double[] offsets = { -0.5, -0.25, 0, 0.25, 0.5 };
        for (int y = 0; y < 90; y++)
            for (int x = 0; x < 90; x++)
            {
                bool clip = offsets.All(dy => offsets.All(dx => Inside(bx + (x + 0.5 + dx) / 90 * bw, by + (y + 0.5 + dy) / 90 * bh)));
                int coverage = 0;
                for (int sy = 0; sy < 5; sy++)
                    for (int sx = 0; sx < 5; sx++)
                        if (Inside(bx + (x + (sx + 0.5) / 5) / 90 * bw, by + (y + (sy + 0.5) / 5) / 90 * bh)) coverage++;
                if (!clip || coverage / 25d < 0.12) blocked.Add(Id(x, y));
            }
        return blocked.ToArray();
    }
}
