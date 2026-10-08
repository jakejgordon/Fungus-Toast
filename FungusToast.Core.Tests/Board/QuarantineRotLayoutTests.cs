using System.Globalization;
using System.Text.RegularExpressions;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Campaign;
using FungusToast.Core.Growth;
using FungusToast.Core.Players;

namespace FungusToast.Core.Tests.Board;

public class QuarantineRotLayoutTests
{
    // Derive the fixture from the real asset rather than tracking an exported mask.
    // This mirrors validate_board_backgrounds.py's outline clip/coverage sampling.
    private static readonly Lazy<int[]> CanonicalBlocked = new(ReadCanonicalBlockedMask);
    private static GameBoard CreateBoard() => new(120, 120, 7, CanonicalBlocked.Value);
    private static int Id(int x, int y) => y * 120 + x;

    [Fact]
    public void Canonical_asset_mask_has_the_measured_playable_area_and_fifteen_percent_pocket()
    {
        var board = CreateBoard();
        var plan = QuarantineRotLayout.Build(board);
        Assert.Equal(5011, board.PlayableTileCount);
        Assert.Equal(761, plan.PocketTileIds.Count);
        Assert.InRange(plan.PocketTileIds.Count / (double)board.PlayableTileCount, 0.14, 0.16);
        Assert.Equal(998, plan.RotTileIds.Count);
        Assert.Equal(141, plan.CorridorTileIds.Count);
        Assert.Equal(Id(60, 60), plan.EntranceTileId);
        Assert.Equal(Id(94, 60), plan.ExitTileId);

        var rot = plan.RotTileIds.ToHashSet();
        var pocket = plan.PocketTileIds.ToHashSet();
        var corridor = plan.CorridorTileIds.ToHashSet();
        Assert.Empty(rot.Intersect(pocket));
        Assert.Empty(rot.Intersect(corridor));
        Assert.Empty(pocket.Intersect(corridor));
        Assert.All(rot.Concat(pocket).Concat(corridor), id => Assert.False(board.GetTileById(id)!.IsBlocked));
        Assert.Equal(board.PlayableTileCount, board.AllTiles().Count(t => !t.IsBlocked
                && !rot.Contains(t.TileId) && !pocket.Contains(t.TileId) && !corridor.Contains(t.TileId))
            + rot.Count + pocket.Count + corridor.Count);
    }

    [Fact]
    public void Build_is_deterministic_read_only_and_independent_of_existing_occupancy()
    {
        var board = CreateBoard();
        var player = new Player(0, "Human", PlayerTypeEnum.Human);
        board.Players.Add(player);
        board.PlaceInitialSpore(0, 47, 76);
        Assert.True(board.TryPlaceRot(Id(61, 40)));
        Assert.True(board.PlaceNutrientPatch(Id(30, 60), NutrientPatch.CreateHypervariationCluster(1, 1)));
        var cells = board.GetAllCells().ToArray();
        var rotBefore = board.RotTileIds.ToArray();
        var blockedBefore = board.GetPermanentlyBlockedTileIds().ToArray();
        int rotEvents = 0;
        board.RotPlaced += _ => rotEvents++;

        var first = QuarantineRotLayout.Build(board);
        var second = QuarantineRotLayout.Build(CreateBoard());
        Assert.Equal(first.RotTileIds, second.RotTileIds);
        Assert.Equal(first.PocketTileIds, second.PocketTileIds);
        Assert.Equal(first.CorridorTileIds, second.CorridorTileIds);
        Assert.Equal(first.StartingPositions, second.StartingPositions);
        Assert.Equal(rotBefore, board.RotTileIds);
        Assert.Equal(blockedBefore, board.GetPermanentlyBlockedTileIds());
        Assert.Equal(cells, board.GetAllCells());
        Assert.True(board.GetTileById(Id(30, 60))!.HasNutrientPatch);
        Assert.Equal(0, rotEvents);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)first.RotTileIds).Add(0));
        Assert.Throws<NotSupportedException>(() => ((IList<(int x, int y)>)first.StartingPositions)[0] = (0, 0));
    }

    [Theory]
    [InlineData(false, 74)]
    [InlineData(true, 64)]
    public void Only_the_long_zigzag_connects_main_and_pocket_even_with_diagonal_growth(bool diagonal, int distance)
    {
        var board = CreateBoard();
        var plan = QuarantineRotLayout.Build(board);
        var open = board.AllTiles().Where(t => !t.IsBlocked).Select(t => t.TileId).Except(plan.RotTileIds).ToHashSet();
        var corridor = plan.CorridorTileIds.ToHashSet();
        var pocket = plan.PocketTileIds.ToHashSet();
        int human = Id(plan.StartingPositions[0].x, plan.StartingPositions[0].y);
        Assert.Equal(open.Count, Reach(board, open, human, diagonal).Count);
        Assert.Equal(pocket.Count, Reach(board, pocket, pocket.Min(), diagonal).Count);
        var distances = Reach(board, corridor, plan.EntranceTileId, diagonal);
        Assert.Equal(corridor.Count, distances.Count);
        Assert.Equal(distance, distances[plan.ExitTileId]);
        Assert.Equal(distance, diagonal ? plan.CorridorDiagonalDistance : plan.CorridorOrthogonalDistance);
        Assert.InRange(distance, 60, 100); // much longer than the 34-cell straight displacement

        foreach (var cut in new[]
        {
            new[] { plan.EntranceTileId },
            new[] { plan.ExitTileId },
            new[] { Id(75, 55), Id(76, 55) }, // cut the middle return leg, not just its mouth
            plan.CorridorTileIds.ToArray()
        })
        {
            var allowed = open.Except(cut).ToHashSet();
            Assert.Empty(Reach(board, allowed, human, diagonal).Keys.Intersect(pocket));
        }
    }

    [Fact]
    public void Corridor_has_two_cell_lanes_and_bends_but_no_three_cell_square_or_extra_entrance()
    {
        var board = CreateBoard();
        var plan = QuarantineRotLayout.Build(board);
        var corridor = plan.CorridorTileIds.ToHashSet();
        Assert.Contains(Id(65, 65), corridor);
        Assert.Contains(Id(66, 65), corridor);
        Assert.All(new[] { Id(65, 69), Id(66, 69), Id(65, 70), Id(66, 70) }, id => Assert.Contains(id, corridor));
        Assert.All(new[] { Id(75, 48), Id(76, 48), Id(75, 49), Id(76, 49) }, id => Assert.Contains(id, corridor));
        for (int y = 0; y < 118; y++)
            for (int x = 0; x < 118; x++)
                Assert.False(Enumerable.Range(0, 3).All(dy => Enumerable.Range(0, 3).All(dx => corridor.Contains(Id(x + dx, y + dy)))),
                    $"Corridor is wider than two cells at ({x},{y}).");
        var main = board.AllTiles().Where(t => !t.IsBlocked && !plan.RotTileIds.Contains(t.TileId)
            && !plan.PocketTileIds.Contains(t.TileId) && !corridor.Contains(t.TileId)).Select(t => t.TileId).ToHashSet();
        var pocket = plan.PocketTileIds.ToHashSet();
        Assert.Equal(new[] { plan.EntranceTileId }, corridor.Where(id => board.GetAdjacentTiles(id).Any(t => main.Contains(t.TileId))).OrderBy(id => id));
        Assert.Equal(new[] { plan.ExitTileId }, corridor.Where(id => board.GetAdjacentTiles(id).Any(t => pocket.Contains(t.TileId))).OrderBy(id => id));
    }

    [Fact]
    public void Belt_is_pinned_to_both_actual_silhouette_edges_in_every_column()
    {
        var board = CreateBoard();
        var plan = QuarantineRotLayout.Build(board);
        var rot = plan.RotTileIds.ToHashSet();
        for (int x = RotBalance.QuarantineOuterBeltStartX; x < 95; x++)
        {
            var column = board.AllTiles().Where(t => !t.IsBlocked && t.X == x).OrderBy(t => t.Y).ToArray();
            Assert.Contains(column.First().TileId, rot);
            Assert.Contains(column.Last().TileId, rot);
            Assert.True(board.IsPlayableEdgeTile(column.First().TileId));
            Assert.True(board.IsPlayableEdgeTile(column.Last().TileId));
        }
        Assert.All(rot, id => Assert.InRange(board.GetTileById(id)!.X, 60, 94));
        Assert.Contains(Id(60, 73), rot); // thick central bulge
        Assert.Contains(Id(60, 45), rot);
        Assert.DoesNotContain(Id(65, 76), rot); // useful bread remains above/below the bulge
        Assert.DoesNotContain(Id(65, 42), rot);
        Assert.DoesNotContain(rot, id => board.GetTileById(id)!.Y is 0 or 119);
    }

    [Fact]
    public void Starts_have_clearance_separation_and_no_human_mouth_advantage()
    {
        var board = CreateBoard();
        var plan = QuarantineRotLayout.Build(board);
        var starts = plan.StartingPositions.Select(p => board.GetTile(p.x, p.y)!).ToArray();
        Assert.Equal(new[] { (47, 76), (49, 60), (28, 60), (47, 43), (27, 77), (27, 43), (10, 60) }, plan.StartingPositions);
        Assert.All(starts, tile =>
        {
            Assert.False(tile.IsBlocked);
            Assert.DoesNotContain(tile.TileId, plan.RotTileIds);
            Assert.DoesNotContain(tile.TileId, plan.PocketTileIds);
            Assert.DoesNotContain(tile.TileId, plan.CorridorTileIds);
            Assert.InRange(board.GetPlayableEdgeDistance(tile.TileId)!.Value, RotBalance.QuarantineStartingEdgeClearance, int.MaxValue);
            Assert.All(plan.RotTileIds, id => Assert.InRange(tile.DistanceTo(board.GetTileById(id)!), RotBalance.QuarantineStartingRotClearance, int.MaxValue));
        });
        for (int i = 0; i < starts.Length; i++)
            for (int j = i + 1; j < starts.Length; j++)
                Assert.InRange(starts[i].DistanceTo(starts[j]), RotBalance.QuarantineStartingSeparation, int.MaxValue);
        var mainWithMouth = board.AllTiles().Where(t => !t.IsBlocked && !plan.RotTileIds.Contains(t.TileId)
            && !plan.PocketTileIds.Contains(t.TileId) && !plan.CorridorTileIds.Contains(t.TileId))
            .Select(t => t.TileId).Append(plan.EntranceTileId).ToHashSet();
        var distances = Reach(board, mainWithMouth, plan.EntranceTileId, true);
        Assert.Equal(new[] { 16, 11, 32, 17, 33, 33, 50 }, starts.Select(t => distances[t.TileId]));
        Assert.True(distances[starts[0].TileId] > distances[starts[1].TileId]);
    }

    [Fact]
    public void All_mixed_Centripetal_loadouts_keep_real_starts_in_main_region_with_safe_clearance()
    {
        var plan = QuarantineRotLayout.Build(CreateBoard());
        Assert.True(AdaptationRepository.TryGetById(AdaptationIds.CentripetalGermination,out var adaptation));
        for (int loadout = 0; loadout < (1<<7); loadout++)
        {
            var board = CreateBoard();
            var players = Enumerable.Range(0,7).Select(id => new Player(id,"Mold",id==0?PlayerTypeEnum.Human:PlayerTypeEnum.AI)).ToList();
            board.Players.AddRange(players);
            foreach (int id in plan.RotTileIds) Assert.True(board.TryPlaceRot(id));
            foreach (var player in players)
                if ((loadout & (1<<player.PlayerId)) != 0) Assert.True(player.TryAddAdaptation(adaptation));
            var preferred = plan.StartingPositions.Select((position,id)=>(position,id)).ToDictionary(x=>x.id,x=>x.position);
            StartingSporeUtility.PlaceStartingSpores(board,players,new Random(17),
                edgeOffsets:new int[7],preferredPositionsByPlayerId:preferred,
                enforceMinimumPlayableEdgeDistanceForPreferredPositions:true,
                ignoreMinimumPlayableEdgeDistancePlayerIds:players.Select(p=>p.PlayerId).ToHashSet());
            QuarantineRotLayout.ValidateEffectiveStartingPositions(board,plan);
            foreach (var player in players)
            {
                var actual=board.GetXYFromTileId(player.StartingTileId!.Value);
                if ((loadout & (1<<player.PlayerId)) != 0) Assert.NotEqual(plan.StartingPositions[player.PlayerId],actual);
                else Assert.Equal(plan.StartingPositions[player.PlayerId],actual);
            }
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void Smaller_player_counts_keep_the_same_geometry_and_stable_slot_prefix(int count)
    {
        var full = QuarantineRotLayout.Build(CreateBoard());
        var plan = QuarantineRotLayout.Build(CreateBoard(), count);
        Assert.Equal(full.RotTileIds, plan.RotTileIds);
        Assert.Equal(full.CorridorTileIds, plan.CorridorTileIds);
        Assert.Equal(full.PocketTileIds, plan.PocketTileIds);
        Assert.Equal(full.StartingPositions.Take(count), plan.StartingPositions);
    }

    [Fact]
    public void Remote_seeding_and_nutrient_placement_in_pocket_remain_legal_using_real_board_apis()
    {
        var board = CreateBoard();
        var plan = QuarantineRotLayout.Build(board);
        var player = new Player(0, "Human", PlayerTypeEnum.Human);
        board.Players.Add(player);
        foreach (int id in plan.RotTileIds) Assert.True(board.TryPlaceRot(id));
        var start = plan.StartingPositions[0];
        board.PlaceInitialSpore(0, start.x, start.y);
        Assert.Contains(Id(105, 60), plan.PocketTileIds);
        Assert.Empty(Reach(board, board.AllTiles().Where(t => !t.IsBlocked && !t.HasRot && t.TileId != plan.EntranceTileId)
            .Select(t => t.TileId).ToHashSet(), Id(start.x, start.y), true).Keys.Intersect(plan.PocketTileIds));
        Assert.True(board.SpawnSporeForPlayer(player, Id(105, 60), GrowthSource.SurgicalInoculation));
        Assert.Equal(0, board.GetCell(Id(105, 60))!.OwnerPlayerId);
        Assert.True(board.PlaceNutrientPatch(Id(106, 60), NutrientPatch.CreateHypervariationCluster(1, 1)));
        Assert.False(board.SpawnSporeForPlayer(player, plan.RotTileIds[0], GrowthSource.SurgicalInoculation));
    }

    [Fact]
    public void Unsupported_dimensions_counts_clipped_corridor_and_disconnected_masks_fail_explicitly()
    {
        Assert.Throws<ArgumentNullException>(() => QuarantineRotLayout.Build(null!));
        Assert.Throws<ArgumentException>(() => QuarantineRotLayout.Build(new GameBoard(115, 115, 7)));
        Assert.Throws<ArgumentException>(() => QuarantineRotLayout.Build(new GameBoard(120, 119, 7)));
        Assert.Throws<ArgumentException>(() => QuarantineRotLayout.Build(new GameBoard(120, 120, 7)));
        foreach (int count in new[] { -1, 0, 8 })
            Assert.Throws<ArgumentOutOfRangeException>(() => QuarantineRotLayout.Build(CreateBoard(), count));
        var clipped = new GameBoard(120, 120, 7, CanonicalBlocked.Value.Append(Id(75, 55)));
        Assert.Contains("clips", Assert.Throws<ArgumentException>(() => QuarantineRotLayout.Build(clipped)).Message);
        var disconnected = new GameBoard(120, 120, 7, CanonicalBlocked.Value.Except(new[] { 0 }));
        Assert.Contains("disconnected", Assert.Throws<ArgumentException>(() => QuarantineRotLayout.Build(disconnected)).Message);
        var splitPocket = new GameBoard(120, 120, 7, CanonicalBlocked.Value.Concat(Enumerable.Range(0, 120).Select(y => Id(105, y))));
        Assert.Throws<ArgumentException>(() => QuarantineRotLayout.Build(splitPocket));
    }

    // Independent graph traversal using the board's real neighbor API, not the planner's BFS.
    private static Dictionary<int, int> Reach(GameBoard board, HashSet<int> allowed, int seed, bool diagonal)
    {
        var distances = new Dictionary<int, int> { [seed] = 0 };
        var queue = new Queue<int>();
        queue.Enqueue(seed);
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (var neighbor in (diagonal ? board.GetAdjacentTiles(current) : board.GetOrthogonalNeighbors(current)))
                if (allowed.Contains(neighbor.TileId) && distances.TryAdd(neighbor.TileId, distances[current] + 1))
                    queue.Enqueue(neighbor.TileId);
        }
        return distances;
    }

    private static int[] ReadCanonicalBlockedMask()
    {
        const string relative = "FungusToast.Unity/Assets/Configs/Toast Configs/ToastBoardMedium.asset";
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, relative))) root = root.Parent;
        Assert.NotNull(root);
        string asset = File.ReadAllText(Path.Combine(root!.FullName, relative));
        const string sprite = "7c52d39ad7868934692e68e52adcc4e9";
        var sizeOverride = Regex.Match(asset, @"- minBoardWidth: 120\r?\n(?<body>.*?)(?=\r?\n  - minBoardWidth:)", RegexOptions.Singleline).Groups["body"].Value;
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
        for (int y = 0; y < 120; y++)
            for (int x = 0; x < 120; x++)
            {
                bool clip = offsets.All(dy => offsets.All(dx => Inside(bx + (x + 0.5 + dx) / 120 * bw, by + (y + 0.5 + dy) / 120 * bh)));
                int coverage = 0;
                for (int sy = 0; sy < 5; sy++)
                    for (int sx = 0; sx < 5; sx++)
                        if (Inside(bx + (x + (sx + 0.5) / 5) / 120 * bw, by + (y + (sy + 0.5) / 5) / 120 * bh)) coverage++;
                if (!clip || coverage / 25d < 0.12) blocked.Add(Id(x, y));
            }
        return blocked.ToArray();
    }
}
