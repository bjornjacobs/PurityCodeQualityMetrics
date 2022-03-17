using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.Violations;

public class ParameterViolationPolicy : IViolationPolicy
{
    public List<PurityViolation> Check(SyntaxNode method, SyntaxTree tree, SemanticModel model)
    {
        IEnumerable<IdentifierNameSyntax> identifiers = method
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>();

        return identifiers.Where(x =>
            {
                ISymbol? symbol = model.GetSymbolInfo(x).Symbol;
                var type = model.GetTypeInfo(x);
                return symbol != null && type.Type != null && symbol.Kind == SymbolKind.Parameter && type.Type.IsReferenceType;
            })
            .Select(x =>  PurityViolation.ModifiesParameters) //Todo: check if return type is getter/ read operation
            .ToList();
    }


}