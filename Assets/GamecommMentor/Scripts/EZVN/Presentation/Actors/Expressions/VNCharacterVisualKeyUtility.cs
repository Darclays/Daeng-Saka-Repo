using System;
using System.Text;

namespace ProjectVN.Presentation.Actors
{
    public static class VNCharacterVisualKeyUtility
    {
        public static string NormalizeKey(
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var source =
                value.Trim();

            var builder =
                new StringBuilder(
                    source.Length);

            var previousWasSeparator =
                false;

            for (var i = 0;
                 i < source.Length;
                 i++)
            {
                var character =
                    source[i];

                if (char.IsLetterOrDigit(
                        character))
                {
                    builder.Append(
                        char.ToLowerInvariant(
                            character));

                    previousWasSeparator =
                        false;

                    continue;
                }

                if (previousWasSeparator ||
                    builder.Length == 0)
                {
                    continue;
                }

                builder.Append('_');
                previousWasSeparator = true;
            }

            while (builder.Length > 0 &&
                   builder[
                       builder.Length - 1] == '_')
            {
                builder.Length--;
            }

            return builder.ToString();
        }

        public static string SanitizeLabel(
            string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        public static bool EqualsKey(
            string left,
            string right)
        {
            return string.Equals(
                NormalizeKey(left),
                NormalizeKey(right),
                StringComparison.Ordinal);
        }

        public static string CreateDisplayName(
            string key)
        {
            var normalized =
                NormalizeKey(key);

            if (string.IsNullOrEmpty(
                    normalized))
            {
                return string.Empty;
            }

            var builder =
                new StringBuilder(
                    normalized.Length);

            var capitalizeNext = true;

            for (var i = 0;
                 i < normalized.Length;
                 i++)
            {
                var character =
                    normalized[i];

                if (character == '_')
                {
                    builder.Append(' ');
                    capitalizeNext = true;
                    continue;
                }

                builder.Append(
                    capitalizeNext
                        ? char.ToUpperInvariant(
                            character)
                        : character);

                capitalizeNext = false;
            }

            return builder.ToString();
        }
    }
}
