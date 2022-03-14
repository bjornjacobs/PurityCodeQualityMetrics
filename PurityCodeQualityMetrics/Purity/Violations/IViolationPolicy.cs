using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.Violations;

public interface IViolationPolicy
{
    List<PurityViolation> Check(SyntaxNode method, SyntaxTree tree, SemanticModel model);
}