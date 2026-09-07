#nullable enable

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FungusToast.Unity.UI.PlayerInspector
{
    /// <summary>
    /// Renders <see cref="PlayerInspectorSection"/> content as TextMeshPro rich text for the
    /// hover tooltip. Kept separate from the content builder so a docked inspector can lay the
    /// same sections out with real UI objects instead of markup.
    /// </summary>
    public static class PlayerInspectorMarkup
    {
        public static string Render(IEnumerable<PlayerInspectorSection> sections)
        {
            var sb = new StringBuilder();
            string headingColor = ColorUtility.ToHtmlStringRGB(UIStyleTokens.Text.Muted);

            foreach (var section in sections)
            {
                if (section == null || section.Lines.Count == 0)
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                if (section.HasHeading)
                {
                    sb.AppendLine($"<color=#{headingColor}><b>{section.Heading}</b></color>");
                }

                foreach (var line in section.Lines)
                {
                    sb.AppendLine(line.HasLabel ? $"<b>{line.Label}:</b> {line.Value}" : line.Value);
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
