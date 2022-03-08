using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.Violations;

public class ThrowsExceptionViolationPolicy : IViolationPolicy
{
    public List<PurityViolation> Check(MethodDeclarationSyntax method, SyntaxTree tree, SemanticModel model)
    {
        IEnumerable<ThrowStatementSyntax> throws = method
            .DescendantNodes()
            .OfType<ThrowStatementSyntax>();
        return throws.Select(x => PurityViolation.ThrowsException).ToList();
    }
}