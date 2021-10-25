using System.Diagnostics.CodeAnalysis;

namespace NuGetUtilities.Core.Extensions
{
    public static class StringExtensions
    {
        public static string Indent(this string value, int indentLevel, int indentSpaces = 4)
        {
            return $"{new string(' ', indentLevel * indentSpaces)}{value}";
        }

        public static string PadRightOrTruncate(this string value, int maxCharacters)
        {
            if (NeedsPadding(ref value, maxCharacters))
            {
                value = value.PadRight(maxCharacters);
            }
            return value;
        }

        [ExcludeFromCodeCoverage]
        private static bool NeedsPadding(ref string value, int maxCharacters)
        {
            if (value.Length < maxCharacters)
            {
                return true;
            }

            if (value.Length == maxCharacters || maxCharacters == 0)
            {
                return false;
            }

            value = value[..maxCharacters];

            return false;
        }
    }
}