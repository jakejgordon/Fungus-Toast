using FungusToast.Core.Common;

namespace FungusToast.Core.Tests.Common;

public sealed class RandomStreamContractTests
{
    [Fact]
    public void AiDrawCountCannotPerturbGameplayStream()
    {
        var control = new RandomStreamContract(12345);
        var treatment = new RandomStreamContract(12345);

        var treatmentDecision = treatment.CreateAiDecisionRandom(2, 7, "mutation-spending");
        for (int draw = 0; draw < 1000; draw++)
        {
            treatmentDecision.Next();
        }

        var controlGameplay = Enumerable.Range(0, 64).Select(_ => control.Gameplay.Next()).ToArray();
        var treatmentGameplay = Enumerable.Range(0, 64).Select(_ => treatment.Gameplay.Next()).ToArray();

        Assert.Equal(controlGameplay, treatmentGameplay);
    }

    [Fact]
    public void SameDecisionIdentityReplaysAndOccurrenceSeparatesStreams()
    {
        var streams = new RandomStreamContract(-42);

        var firstStream = streams.CreateAiDecisionRandom(1, 3, "mycovariant-draft", 0);
        var replayStream = streams.CreateAiDecisionRandom(1, 3, "mycovariant-draft", 0);
        var first = Enumerable.Range(0, 8).Select(_ => firstStream.Next()).ToArray();
        var replay = Enumerable.Range(0, 8).Select(_ => replayStream.Next()).ToArray();
        var nextOccurrence = streams.CreateAiDecisionRandom(1, 3, "mycovariant-draft", 1).Next();

        Assert.Equal(first, replay);
        Assert.NotEqual(first[0], nextOccurrence);
    }
}
