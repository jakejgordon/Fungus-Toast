using System.Globalization;

namespace FungusToast.Simulation.Calibration;

public enum PlayerCountClass
{
    Duel,
    SmallTable,
    Crowded,
    Swarm
}

public enum BoardScaleClass
{
    Small,
    Medium,
    Large
}

public enum AspectClass
{
    Square,
    Wide,
    Tall
}

public enum GeometryClass
{
    Rectangle,
    Masked
}

public enum StartRegimeClass
{
    Generated,
    Exact,
    PreferredPool
}

/// <summary>
/// The classes a context rolls up into, all derived rather than declared.
/// </summary>
public sealed record ContextClasses(
    PlayerCountClass PlayerCount,
    BoardScaleClass BoardScale,
    AspectClass Aspect,
    GeometryClass Geometry,
    StartRegimeClass StartRegime);

/// <summary>
/// Classifies a measured context into comparable rollups.
///
/// The simulation accepts any positive board size and 2-8 players, so these classes are derived
/// from resolved values rather than matched against a list of presets - a preset list would go
/// stale the moment someone ran a board size nobody had thought of. Exact dimensions, geometry
/// fingerprint, and player count are always retained alongside; the classes exist to pool
/// comparable results, not to replace what was actually run.
///
/// Thresholds are the ones frozen in the plan's P3.4 taxonomy and are deliberately not tunable:
/// re-drawing class boundaries after seeing results is how a contextual specialist gets relabelled
/// as a generalist.
/// </summary>
public static class ContextTaxonomy
{
    /// <summary>80x80 anchors the top of the small class.</summary>
    public const int SmallBoardMaximumArea = 6_400;

    /// <summary>160x160 anchors the top of the medium class.</summary>
    public const int MediumBoardMaximumArea = 25_600;

    public const double SquareAspectMinimum = 0.9;
    public const double SquareAspectMaximum = 1.1;

    public static PlayerCountClass ClassifyPlayerCount(int playerCount) => playerCount switch
    {
        2 => PlayerCountClass.Duel,
        3 or 4 => PlayerCountClass.SmallTable,
        5 or 6 => PlayerCountClass.Crowded,
        7 or 8 => PlayerCountClass.Swarm,
        _ => throw new ArgumentOutOfRangeException(
            nameof(playerCount), playerCount, "Only 2-8 players are supported.")
    };

    public static BoardScaleClass ClassifyBoardScale(int width, int height)
    {
        RequirePositive(width, height);
        var area = (long)width * height;
        if (area <= SmallBoardMaximumArea) return BoardScaleClass.Small;
        return area <= MediumBoardMaximumArea ? BoardScaleClass.Medium : BoardScaleClass.Large;
    }

    public static AspectClass ClassifyAspect(int width, int height)
    {
        RequirePositive(width, height);
        var ratio = (double)width / height;
        if (ratio > SquareAspectMaximum) return AspectClass.Wide;
        return ratio < SquareAspectMinimum ? AspectClass.Tall : AspectClass.Square;
    }

    /// <summary>
    /// Masked results never pool across different fingerprints, so the fingerprint travels with the
    /// class rather than being collapsed into it.
    /// </summary>
    public static GeometryClass ClassifyGeometry(string geometryId, IReadOnlyCollection<int>? blockedTileIds)
        => string.Equals(geometryId, "rectangle", StringComparison.OrdinalIgnoreCase) && (blockedTileIds?.Count ?? 0) == 0
            ? GeometryClass.Rectangle
            : GeometryClass.Masked;

    public static StartRegimeClass ClassifyStartRegime(int exactStartingPositionCount, int preferredPositionPoolCount)
    {
        if (exactStartingPositionCount > 0 && preferredPositionPoolCount > 0)
            throw new ArgumentException("A context cannot use both exact starts and preferred pools.");
        if (exactStartingPositionCount > 0) return StartRegimeClass.Exact;
        return preferredPositionPoolCount > 0 ? StartRegimeClass.PreferredPool : StartRegimeClass.Generated;
    }

    public static ContextClasses Classify(
        int playerCount,
        int width,
        int height,
        string geometryId,
        IReadOnlyCollection<int>? blockedTileIds,
        int exactStartingPositionCount,
        int preferredPositionPoolCount)
        => new(
            ClassifyPlayerCount(playerCount),
            ClassifyBoardScale(width, height),
            ClassifyAspect(width, height),
            ClassifyGeometry(geometryId, blockedTileIds),
            ClassifyStartRegime(exactStartingPositionCount, preferredPositionPoolCount));

    /// <summary>
    /// The canonical rollup key for a set of classes, used as a context ID so an ID cannot describe
    /// a context it does not actually match.
    /// </summary>
    public static string BuildRollupKey(ContextClasses classes)
    {
        ArgumentNullException.ThrowIfNull(classes);
        return string.Join(
            ".",
            Slug(classes.PlayerCount.ToString()),
            Slug(classes.BoardScale.ToString()),
            Slug(classes.Aspect.ToString()),
            Slug(classes.Geometry.ToString()),
            Slug(classes.StartRegime.ToString()));
    }

    private static string Slug(string value) => value.ToLowerInvariant();

    private static void RequirePositive(int width, int height)
    {
        if (width < 1 || height < 1)
            throw new ArgumentOutOfRangeException(
                nameof(width),
                $"{width.ToString(CultureInfo.InvariantCulture)}x{height.ToString(CultureInfo.InvariantCulture)}",
                "Board dimensions must be positive.");
    }
}
