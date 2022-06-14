using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.Purity.Violations;

public class ThrowsExceptionViolationPolicy : IViolationPolicy
{
    public List<PurityViolation> Check(SyntaxNode method, SyntaxTree tree, SemanticModel model)
    {
        IEnumerable<ThrowStatementSyntax> throws = method
            .DescendantNodesInThisFunction()
            .OfType<ThrowStatementSyntax>();
        return throws.Select(x => PurityViolation.ThrowsException).ToList();
    }
}