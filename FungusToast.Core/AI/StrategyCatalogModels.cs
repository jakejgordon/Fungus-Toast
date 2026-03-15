using System;
using System.Collections.Generic;

namespace FungusToast.Core.AI
{
    public enum StrategyArchetype
    {
        Balanced,
        EconomyRamp,
        Reclamation,
        Offense,
        SurgeTempo,
        Defense,
        Control,
        Mobility,
        Attrition,
        Counterplay,
        LateGameSpike,
        TierCap
    }

    public enum StrategyPowerTier
    {
        Weak,
        Standard,
        Strong,
        Spike
    }

    public enum StrategyRole
    {
        Baseline,
        Training,
        Spice,
        Boss,
        Experimental
    }

    public enum StrategyLifecycle
    {
        Draft,
        Active,
        NeedsTuning,
        Retired
    }

    public enum DifficultyBand
    {
        Easy,
        Normal,
        Hard,
        Elite
    }

    [Flags]
    public enum StrategyPool
    {
        None = 0,
        SimulationBaseline = 1 << 0,
        SimulationExperimental = 1 << 1,
        Campaign = 1 << 2,
        MycovariantLab = 1 << 3
    }

    public sealed class CounterTag : IEquatable<CounterTag>
    {
        public CounterTag(StrategyArchetype? archetype = null, string? strategyName = null, string reason = "")
        {
            Archetype = archetype;
            StrategyName = strategyName;
            Reason = reason ?? string.Empty;
        }

        public StrategyArchetype? Archetype { get; }
        public string? StrategyName { get; }
        public string Reason { get; }

        public bool Equals(CounterTag? other)
        {
            if (other is null)
            {
                return false;
            }

            return Archetype == other.Archetype
                && string.Equals(StrategyName, other.StrategyName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj) => Equals(obj as CounterTag);

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Archetype,
                StrategyName?.ToUpperInvariant(),
                Reason);
        }
    }

    public sealed class StrategyCatalogEntry
    {
        public StrategyCatalogEntry(
            string strategyName,
            StrategySetEnum strategySet,
            StrategyArchetype archetype,
            StrategyStatus status,
            StrategyPowerTier powerTier,
            StrategyRole role,
            StrategyLifecycle lifecycle,
            IReadOnlyCollection<DifficultyBand> difficultyBands,
            StrategyPool pools,
            string intent,
            string notes,
            IReadOnlyCollection<CounterTag>? favoredAgainst = null,
            IReadOnlyCollection<CounterTag>? weakAgainst = null)
        {
            StrategyName = strategyName;
            StrategySet = strategySet;
            Archetype = archetype;
            Status = status;
            PowerTier = powerTier;
            Role = role;
            Lifecycle = lifecycle;
            DifficultyBands = difficultyBands;
            Pools = pools;
            Intent = intent;
            Notes = notes;
            FavoredAgainst = favoredAgainst ?? Array.Empty<CounterTag>();
            WeakAgainst = weakAgainst ?? Array.Empty<CounterTag>();
        }

        public string StrategyName { get; }
        public StrategySetEnum StrategySet { get; }
        public StrategyArchetype Archetype { get; }
        public StrategyStatus Status { get; }
        public StrategyPowerTier PowerTier { get; }
        public StrategyRole Role { get; }
        public StrategyLifecycle Lifecycle { get; }
        public IReadOnlyCollection<DifficultyBand> DifficultyBands { get; }
        public StrategyPool Pools { get; }
        public string Intent { get; }
        public string Notes { get; }
        public IReadOnlyCollection<CounterTag> FavoredAgainst { get; }
        public IReadOnlyCollection<CounterTag> WeakAgainst { get; }
    }
}
