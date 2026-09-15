namespace FungusToast.Core.Mycovariants
{
    /// <summary>
    /// Player-facing copy for Mycovariant tags (badges shown on draft cards).
    /// Tag copy carries the rule shared by every card with that tag, so individual
    /// card descriptions only describe their own effect. See GAMEPLAY_TERMINOLOGY.md ("Tags").
    /// </summary>
    public static class MycovariantTagCopy
    {
        public const string BaitLabel = "Bait";

        /// <summary>
        /// Tooltip for the Bait tag. States the draft rule enforced by
        /// <see cref="MycovariantPoolManager"/> (only the Human or the last AI in draft order is
        /// offered a Bait card) and by the bait cards' AI scores (that AI always takes one).
        /// </summary>
        public const string BaitTooltip =
            "Bait Mycovariant. Only you and the AI that drafts last (the AI with the most living cells) are ever offered a Bait card, and that AI always takes one when it can. " +
            "Draft it yourself for the Human reward, or leave it and let the leading AI spring the trap on itself. " +
            "Bait cards unlock through campaign Moldiness rewards.";
    }
}
