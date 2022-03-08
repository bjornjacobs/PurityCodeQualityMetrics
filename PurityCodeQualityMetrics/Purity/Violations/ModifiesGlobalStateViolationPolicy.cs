using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.Violations;

public class ModifiesGlobalStateViolationPolicy : IViolationPolicy
{
    public List<PurityViolation> Check(MethodDeclarationSyntax method, SyntaxTree tree, SemanticModel model)
    {
        
        var assignments = method
            .DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Where(a => a.Left is not DeclarationExpressionSyntax);

        return assignments.Where(assignment =>
        {
            var identifier = assignment.Left;
            
            ISymbol? symbol = model.GetSymbolInfo(identifier).Symbol;

            return symbol != null && !IsIdentifierIsFresh(identifier, model) && symbol.IsStatic;
        }).Select(x => PurityViolation.ModifiesGlobalState).ToList();
    }
    
    public bool IsIdentifierIsFresh(ExpressionSyntax identifier, SemanticModel model)
    {
        ISymbol? symbol = model.GetSymbolInfo(identifier).Symbol;

        // If declaration could not be found the identifier cannot be fresh
        if (symbol == null || !symbol.DeclaringSyntaxReferences.Any())
        {
            return false;
        }

        // If symbol is a parameter it cannot be fresh
        if (symbol.Kind == SymbolKind.Parameter) return false;

        var symbolMethodAncestor = symbol
            .DeclaringSyntaxReferences
            .First() // TODO: check all trees
            .GetSyntax()
            .Ancestors()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        var identifierMethodAncestor = identifier
            .Ancestors()
            .OfType<MethodDeclarationSyntax>()
            .First();

        // Identifier is fresh if it lies inside the same method as its
        // declaration does
        return symbolMethodAncestor == identifierMethodAncestor;
    }
}