using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeQualityAnalyzer.CodeMetrics
{
    public static class SourceLinesOfCode
    {
        private const string NewlineToken = "_NEWLINETOKEN_";

        public static int GetCount(ClassDeclarationSyntax classDec)
        {
            return classDec
                    .ToString()
                    .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                    .StringJoin(NewlineToken)
                    .RegexReplace(@"\/\*[\W\w]*?\*\/", "")
                    .Split(new[] { NewlineToken }, StringSplitOptions.RemoveEmptyEntries)
                    .Count(NotEmptyNorComment)
                ;
        }

        private static bool NotEmptyNorComment(string s)
        {
            string trimmed = s.Trim();

            if (trimmed == "") return false;
            if (trimmed.Length == 1) return true;

            return trimmed[0] != '/' || trimmed[1] != '/';
        }
    }
}
