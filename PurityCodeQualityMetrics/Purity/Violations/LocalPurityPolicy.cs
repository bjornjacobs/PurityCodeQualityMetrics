using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.Purity.Violations;

public class LocalPurityPolicy : IViolationPolicy
{
    private int aaa = 5;
    public List<PurityViolation> Check(SyntaxNode method, SyntaxTree tree, SemanticModel model)
    {
        //1. Local field or property
        //2. What is the source? (variable, paramter, local, global)
        //3. Are you allowed to modify (if fresh)
        //4. Are you allowed to read (parameter or derived from parameter)


        var methodSymbol = method.GetMethodSymbol(model);
        
        var local = method
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Select(x => new {Node = x, Symbol = model.GetSymbolInfo(x).Symbol})
            .Where(x => x.Symbol != null)
            .Where(x => SymbolEqualityComparer.Default.Equals(x.Symbol!.ContainingType, methodSymbol.ContainingType) && 
                        !x.Symbol.IsStatic && 
                        x.Symbol.Kind is SymbolKind.Field or SymbolKind.Property)
            .ToList();
        
        var fieldViolations = local
            .Select(x => x.Node.IsAssignedTo() ? PurityViolation.ModifiesLocalState : PurityViolation.ReadsLocalState)
            .ToList();

        var properties = local
            .Where(x => x.Symbol is IPropertySymbol).Select(x => new {x.Node, Symbol = x.Symbol as IPropertySymbol})
            .ToList();


        return fieldViolations;
    }
}