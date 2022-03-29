using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.Purity.Violations;

public class StaticFieldViolationPolicy : IViolationPolicy
{
    public List<PurityViolation> Check(SyntaxNode method, SyntaxTree tree, SemanticModel model)
    {
        IEnumerable<IdentifierNameSyntax> identifiers = method
            .DescendantNodesInThisFunction()
            .OfType<IdentifierNameSyntax>();

        return identifiers.Where(x =>
        {
            ISymbol? symbol = model.GetSymbolInfo(x).Symbol;
            if (symbol == null) return false;
            
            bool isStatic = symbol.IsStatic;
            bool isField = symbol.Kind == SymbolKind.Field;
            bool isProperty = symbol.Kind == SymbolKind.Property;
            bool isMethod = symbol.Kind == SymbolKind.Method;
            bool isEnumConstant = symbol.IsEnumConstant();

            return isStatic && (isField || isProperty) && !isMethod && !isEnumConstant;
        })
            .Select(x => x.IsAssignedTo() ? PurityViolation.ModifiesGlobalState : PurityViolation.ReadsGlobalState)
            .ToList();
        
    }
}