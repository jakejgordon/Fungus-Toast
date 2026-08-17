using FungusToast.Core.Mutations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FungusToast.Unity.UI.MutationTree
{
    /// <summary>
    /// Ordered mutation-lane presentation metadata. The planned Substrate Ecology lane
    /// intentionally has no Core category until its first real mechanic is implemented.
    /// </summary>
    public static class MutationCategoryPresentationCatalog
    {
        public static readonly MutationCategoryPresentation Growth = new(
            key: "Growth",
            displayName: "Growth",
            iconKey: "growth",
            iconGlyph: "✦",
            tooltipText: "<b>Growth</b>\nMutations that help your colony spread faster across the toast.",
            accent: UIStyleTokens.Category.Growth,
            preferredWidth: 200f,
            coreCategory: MutationCategory.Growth);

        public static readonly MutationCategoryPresentation CellularResilience = new(
            key: "CellularResilience",
            displayName: "Cellular Resilience",
            iconKey: "cellular-resilience",
            iconGlyph: "⬡",
            tooltipText: "<b>Cellular Resilience</b>\nMutations that help your mold endure damage and reclaim dead cells.",
            accent: UIStyleTokens.Category.CellularResilience,
            preferredWidth: 190f,
            coreCategory: MutationCategory.CellularResilience);

        public static readonly MutationCategoryPresentation Fungicide = new(
            key: "Fungicide",
            displayName: "Fungicide",
            iconKey: "fungicide",
            iconGlyph: "×",
            tooltipText: "<b>Fungicide</b>\nAggressive mutations that harry rival molds and weaken their foothold.",
            accent: UIStyleTokens.Category.Fungicide,
            preferredWidth: 190f,
            coreCategory: MutationCategory.Fungicide);

        public static readonly MutationCategoryPresentation GeneticDrift = new(
            key: "GeneticDrift",
            displayName: "Genetic Drift",
            iconKey: "genetic-drift",
            iconGlyph: "≈",
            tooltipText: "<b>Genetic Drift</b>\nMutations that warp your evolution, granting extra mutation points or unexpected gifts.",
            accent: UIStyleTokens.Category.GeneticDrift,
            preferredWidth: 190f,
            coreCategory: MutationCategory.GeneticDrift);

        public static readonly MutationCategoryPresentation MycelialSurges = new(
            key: "MycelialSurges",
            displayName: "Mycelial Surges",
            iconKey: "mycelial-surges",
            iconGlyph: "↯",
            tooltipText: "<b>Mycelial Surges</b>\nMutations that unleash brief but potent bursts of colony strength.",
            accent: UIStyleTokens.Category.MycelialSurges,
            preferredWidth: 190f,
            coreCategory: MutationCategory.MycelialSurges);

        public static readonly MutationCategoryPresentation SubstrateEcology = new(
            key: "SubstrateEcology",
            displayName: "Substrate Ecology",
            iconKey: "substrate-ecology",
            iconGlyph: "○",
            tooltipText: "<b>Substrate Ecology</b>\nA planned category about nutrient patches, dead zones, crowding, edges, and environmental opportunity. Its roster is still in design and cannot be purchased yet.",
            accent: UIStyleTokens.Category.SubstrateEcology,
            preferredWidth: 190f,
            coreCategory: null);

        public static readonly IReadOnlyList<MutationCategoryPresentation> Ordered = new[]
        {
            Growth,
            CellularResilience,
            Fungicide,
            GeneticDrift,
            MycelialSurges,
            SubstrateEcology
        };

        public static MutationCategoryPresentation Get(MutationCategory category)
        {
            return Ordered.First(entry => entry.CoreCategory == category);
        }
    }

    public sealed class MutationCategoryPresentation
    {
        public MutationCategoryPresentation(
            string key,
            string displayName,
            string iconKey,
            string iconGlyph,
            string tooltipText,
            Color accent,
            float preferredWidth,
            MutationCategory? coreCategory)
        {
            Key = key;
            DisplayName = displayName;
            IconKey = iconKey;
            IconGlyph = iconGlyph;
            TooltipText = tooltipText;
            Accent = accent;
            PreferredWidth = preferredWidth;
            CoreCategory = coreCategory;
        }

        public string Key { get; }
        public string DisplayName { get; }
        public string IconKey { get; }
        public string IconGlyph { get; }
        public string TooltipText { get; }
        public Color Accent { get; }
        public float PreferredWidth { get; }
        public MutationCategory? CoreCategory { get; }
        public bool IsPlanned => !CoreCategory.HasValue;
    }
}
