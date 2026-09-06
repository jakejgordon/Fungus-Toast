using System.Text;

namespace FungusToast.Simulation.Experiments;

/// <summary>
/// Derives the per-condition artifact identity that names a run's export folder and its
/// run-state file.
///
/// This is derived from the condition ID because that is the one field the manifest already
/// guarantees to be unique. The earlier derivation rebuilt the condition's *shape* instead
/// (players, board, strategy set), which is unique only by coincidence: the CLI happens to encode
/// exactly that shape into the condition ID it generates, so it can never emit two conditions that
/// collide. A hand-authored manifest can. Two conditions differing only in lineup - which is
/// precisely what a paired control/treatment comparison is - derived one artifact ID, so the
/// second run overwrote the first's export, or, under --resume, failed with a confusing execution
/// fingerprint mismatch.
/// </summary>
public static class ExperimentArtifactId
{
    /// <summary>
    /// Batch runs get one artifact per condition; a single-condition run keeps the bare experiment
    /// ID so its folder still reads as the experiment itself.
    /// </summary>
    public static string Derive(string experimentId, string conditionId, bool isBatchMode)
    {
        if (string.IsNullOrWhiteSpace(experimentId))
            throw new ArgumentException("Experiment ID is required.", nameof(experimentId));

        if (!isBatchMode) return experimentId;

        if (string.IsNullOrWhiteSpace(conditionId))
            throw new ArgumentException("Condition ID is required for a batch run.", nameof(conditionId));

        return experimentId + "__" + Sanitize(conditionId);
    }

    /// <summary>
    /// Condition IDs allow '.', which reads poorly in a folder name; everything else the manifest
    /// permits is already path-safe.
    /// </summary>
    private static string Sanitize(string conditionId)
    {
        var builder = new StringBuilder(conditionId.Length);
        foreach (var character in conditionId)
            builder.Append(char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_');
        return builder.ToString();
    }
}
