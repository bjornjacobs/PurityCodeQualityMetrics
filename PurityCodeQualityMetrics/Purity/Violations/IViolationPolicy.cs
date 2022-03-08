using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.Violations;

public interface IViolationPolicy
{
    List<PurityViolation> Check(MethodDeclarationSyntax method, SyntaxTree tree,  SemanticModel model);
}