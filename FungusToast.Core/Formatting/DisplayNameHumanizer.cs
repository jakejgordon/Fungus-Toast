using System.Text.RegularExpressions;

namespace FungusToast.Core.Formatting
{
    public static class DisplayNameHumanizer
    {
        private static readonly Regex PascalCaseBoundaryRegex = new(
            @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
            RegexOptions.Compiled);

        public static string HumanizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return PascalCaseBoundaryRegex.Replace(value, " ");
        }
    }
}
