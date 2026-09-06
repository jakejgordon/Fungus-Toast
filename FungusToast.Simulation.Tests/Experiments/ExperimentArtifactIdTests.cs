using FungusToast.Simulation.Experiments;
using Xunit;

namespace FungusToast.Simulation.Tests.Experiments;

public sealed class ExperimentArtifactIdTests
{
    /// <summary>
    /// The reason this derivation exists. A paired comparison runs a control and a treatment that
    /// are identical in players, board, and strategy set and differ only in lineup. Deriving the
    /// artifact ID from that shape gave both conditions one folder, so the second run overwrote
    /// the first's export - or, under --resume, failed with an execution fingerprint mismatch that
    /// pointed nowhere near the real cause.
    /// </summary>
    [Fact]
    public void ConditionsDifferingOnlyInLineup_DeriveDistinctArtifactIds()
    {
        var control = ExperimentArtifactId.Derive("exp", "paired.control", isBatchMode: true);
        var treatment = ExperimentArtifactId.Derive("exp", "paired.treatment", isBatchMode: true);

        Assert.NotEqual(control, treatment);
        Assert.Equal("exp__paired_control", control);
        Assert.Equal("exp__paired_treatment", treatment);
    }

    /// <summary>
    /// Condition IDs are unique per manifest, so artifact IDs derived from them are too. This is
    /// the property the old shape-based derivation could not offer.
    /// </summary>
    [Theory]
    [InlineData("p2.w20.h20.s.testing", "exp__p2_w20_h20_s_testing")]
    [InlineData("smoke-01", "exp__smoke-01")]
    [InlineData("with_underscores", "exp__with_underscores")]
    public void ConditionIds_AreSanitizedIntoPathSafeArtifactIds(string conditionId, string expected)
    {
        Assert.Equal(expected, ExperimentArtifactId.Derive("exp", conditionId, isBatchMode: true));
    }

    /// <summary>
    /// A single-condition run keeps the bare experiment ID, so its folder still reads as the
    /// experiment itself rather than gaining a redundant suffix.
    /// </summary>
    [Fact]
    public void SingleConditionRun_KeepsTheBareExperimentId()
    {
        Assert.Equal("exp", ExperimentArtifactId.Derive("exp", "p2.w20.h20.s.testing", isBatchMode: false));
    }

    [Fact]
    public void BatchRunWithoutAConditionId_Throws()
    {
        Assert.Throws<ArgumentException>(() => ExperimentArtifactId.Derive("exp", " ", isBatchMode: true));
    }

    [Fact]
    public void MissingExperimentId_Throws()
    {
        Assert.Throws<ArgumentException>(() => ExperimentArtifactId.Derive(" ", "condition", isBatchMode: false));
    }
}
