using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.CodeMetrics
{
    public static class CommentDensity
    {
        public static int GetCount(SyntaxNode node, int sourceLinesOfCode)
        {
            int counter = 0;

            var lines = node
                    .ToString()
                    .Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None)
                ;

            bool inComment = false;
            foreach (var line in lines)
            {
                if (line.Contains("//"))
                {
                    counter++;
                    continue;
                }

                if (line.Contains(@"/*"))
                {
                    inComment = true;
                }

                if (inComment && line.Trim() != "")
                {
                    counter++;
                }

                if (line.Contains(@"*/")) inComment = false;
            }

            return (int)((double) counter / (sourceLinesOfCode + counter) * 100);
        }
    }
}
