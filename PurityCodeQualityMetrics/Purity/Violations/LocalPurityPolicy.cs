using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.Purity.Violations;

public class LocalPurityPolicy : IViolationPolicy
{
    public List<PurityViolation> Check(SyntaxNode method, SyntaxTree tree, SemanticModel model)
    {
        IEnumerable<IdentifierNameSyntax> identifiers = method
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>();

        return identifiers.Where(x =>
            {
                ISymbol? symbol = model.GetSymbolInfo(x).Symbol;
                
                return symbol != null && symbol.Kind == SymbolKind.Field & !symbol.IsStatic;
            })
            .Select(x => x.IsAssignedTo() ? PurityViolation.ModifiesLocalState : PurityViolation.ReadsLocalState)
            .ToList();
    }
}