using System;

namespace FungusToast.Core.Common;

public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random random;

    public SystemRandomSource(Random random)
    {
        this.random = random;
    }

    public double NextDouble() => random.NextDouble();

    public int Next(int maxValue) => random.Next(maxValue);
}
