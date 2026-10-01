using System;
using System.Collections.Generic;
using System.Linq;
using FungusToast.Core.Campaign;

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

    /// <summary>How well a measurement supports the band it reports.</summary>
    public enum BandEvidence
    {
        /// <summary>Enough games and a tight enough interval to place the strategy.</summary>
        Sufficient,

        /// <summary>Measured, but on too few games to place.</summary>
        TooFewGames,

        /// <summary>Measured on enough games, but the interval spans too many bands to choose one.</summary>
        IntervalTooWide,

        /// <summary>Never measured in this context.</summary>
        NotMeasured
    }

    public enum CampaignDifficulty
    {
        Training,
        Easy,
        Medium,
        Hard,
        Elite,
        Boss
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
            StrategyRole role,
            StrategyLifecycle lifecycle,
            IReadOnlyCollection<DifficultyBand> intendedBands,
            CampaignDifficulty? campaignDifficulty,
            StrategyPool pools,
            string friendlyName,
            string aiPlayerIntentions,
            string intent,
            string notes,
            IReadOnlyCollection<CounterTag>? favoredAgainst = null,
            IReadOnlyCollection<CounterTag>? weakAgainst = null,
            IReadOnlyCollection<AdaptationSynergySet>? suggestedAdaptationSets = null,
            string mutationPlan = "",
            string mycovariantPlan = "")
        {
            StrategyName = strategyName;
            StrategySet = strategySet;
            Archetype = archetype;
            Status = status;
            Role = role;
            Lifecycle = lifecycle;
            IntendedBands = intendedBands;
            CampaignDifficulty = campaignDifficulty;
            Pools = pools;
            FriendlyName = friendlyName;
            AIPlayerIntentions = aiPlayerIntentions;
            Intent = intent;
            Notes = notes;
            FavoredAgainst = favoredAgainst ?? Array.Empty<CounterTag>();
            WeakAgainst = weakAgainst ?? Array.Empty<CounterTag>();
            SuggestedAdaptationSets = suggestedAdaptationSets ?? Array.Empty<AdaptationSynergySet>();
            MutationPlan = mutationPlan ?? string.Empty;
            MycovariantPlan = mycovariantPlan ?? string.Empty;
        }

        public string StrategyName { get; }
        public StrategySetEnum StrategySet { get; }
        public StrategyArchetype Archetype { get; }
        public StrategyStatus Status { get; }
        public StrategyRole Role { get; }
        public StrategyLifecycle Lifecycle { get; }
        /// <summary>
        /// The band the author designed this strategy to play at. Measured performance lives on
        /// <see cref="StrategyDefinition.MeasuredBand"/>; a gap between the two means the strategy is
        /// not doing what it was built to do. Empty when no intent has been recorded.
        /// </summary>
        public IReadOnlyCollection<DifficultyBand> IntendedBands { get; }
        public CampaignDifficulty? CampaignDifficulty { get; }
        public StrategyPool Pools { get; }
        public string FriendlyName { get; }
        public string AIPlayerIntentions { get; }
        public string Intent { get; }
        public string Notes { get; }
        public IReadOnlyCollection<CounterTag> FavoredAgainst { get; }
        public IReadOnlyCollection<CounterTag> WeakAgainst { get; }
        public IReadOnlyCollection<AdaptationSynergySet> SuggestedAdaptationSets { get; }
        public string MutationPlan { get; }
        public string MycovariantPlan { get; }
    }

    public sealed class StrategyCatalogFilter
    {
        public IReadOnlyCollection<StrategyArchetype> Archetypes { get; set; } = Array.Empty<StrategyArchetype>();
        public IReadOnlyCollection<StrategyRole> Roles { get; set; } = Array.Empty<StrategyRole>();
        public IReadOnlyCollection<StrategyLifecycle> Lifecycles { get; set; } = Array.Empty<StrategyLifecycle>();
        public IReadOnlyCollection<DifficultyBand> IntendedBands { get; set; } = Array.Empty<DifficultyBand>();
        public IReadOnlyCollection<CampaignDifficulty> CampaignDifficulties { get; set; } = Array.Empty<CampaignDifficulty>();
        public IReadOnlyCollection<StrategyPool> Pools { get; set; } = Array.Empty<StrategyPool>();

        public bool IsEmpty => Archetypes.Count == 0
            && Roles.Count == 0
            && Lifecycles.Count == 0
            && IntendedBands.Count == 0
            && CampaignDifficulties.Count == 0
            && Pools.Count == 0;

        public bool Matches(StrategyCatalogEntry entry)
        {
            if (Archetypes.Count > 0 && !Archetypes.Contains(entry.Archetype))
            {
                return false;
            }

            if (Roles.Count > 0 && !Roles.Contains(entry.Role))
            {
                return false;
            }

            if (Lifecycles.Count > 0 && !Lifecycles.Contains(entry.Lifecycle))
            {
                return false;
            }

            if (IntendedBands.Count > 0 && !entry.IntendedBands.Any(IntendedBands.Contains))
            {
                return false;
            }

            if (CampaignDifficulties.Count > 0 && (!entry.CampaignDifficulty.HasValue || !CampaignDifficulties.Contains(entry.CampaignDifficulty.Value)))
            {
                return false;
            }

            if (Pools.Count > 0 && !Pools.Any(pool => (entry.Pools & pool) == pool))
            {
                return false;
            }

            return true;
        }
    }
}
