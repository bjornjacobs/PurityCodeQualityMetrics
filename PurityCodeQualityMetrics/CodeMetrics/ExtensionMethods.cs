using System.Text.RegularExpressions;

namespace PurityCodeQualityMetrics.CodeMetrics
{
    internal static class ExtensionMethods
    {
        public static int LimitToOne(this int i) => Math.Min(i, 1);

        public static string StringJoin(this string[] s, string separator) => string.Join(separator, s);

        public static string RegexReplace(this string s, string pattern, string replacement) => Regex.Replace(s, pattern, replacement);
    }
}
