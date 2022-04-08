using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.Purity.Violations;

public class ParameterViolationPolicy : IViolationPolicy
{
    public List<PurityViolation> Check(SyntaxNode method, SyntaxTree tree, SemanticModel model)
    {
        return method
            .DescendantNodesInThisFunction()
            .OfType<AssignmentExpressionSyntax>()
            .Select(x => x.Left)
            .OfType<MemberAccessExpressionSyntax>()
            .Select(x => model.GetSymbolInfo(x.Expression).Symbol)
            .OfType<IParameterSymbol>()
            .Where(x => !x.Name.Equals("this"))
            .Select(x => PurityViolation.ModifiesParameters).ToList();
    }
}