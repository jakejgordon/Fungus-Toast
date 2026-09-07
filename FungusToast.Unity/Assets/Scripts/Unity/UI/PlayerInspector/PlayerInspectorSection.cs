#nullable enable

using System.Collections.Generic;

namespace FungusToast.Unity.UI.PlayerInspector
{
    /// <summary>
    /// One labelled line inside a <see cref="PlayerInspectorSection"/>. A null or empty
    /// <see cref="Label"/> renders the value on its own, which keeps list continuations
    /// (mutation names, mycovariant preferences) from repeating their heading.
    /// </summary>
    public readonly struct PlayerInspectorLine
    {
        public PlayerInspectorLine(string? label, string value)
        {
            Label = label;
            Value = value;
        }

        public string? Label { get; }
        public string Value { get; }

        public bool HasLabel => !string.IsNullOrEmpty(Label);

        public static PlayerInspectorLine Plain(string value) => new PlayerInspectorLine(null, value);
    }

    /// <summary>
    /// A presentation-neutral block of inspector content. The hover tooltip renders these as
    /// rich text; a future docked player inspector can render the same sections as real UI
    /// without the two surfaces drifting apart.
    /// </summary>
    public sealed class PlayerInspectorSection
    {
        public PlayerInspectorSection(string? heading, IReadOnlyList<PlayerInspectorLine> lines)
        {
            Heading = heading;
            Lines = lines;
        }

        /// <summary>Null or empty for an unheaded block such as the player identity lines.</summary>
        public string? Heading { get; }

        public IReadOnlyList<PlayerInspectorLine> Lines { get; }

        public bool HasHeading => !string.IsNullOrEmpty(Heading);
    }
}
