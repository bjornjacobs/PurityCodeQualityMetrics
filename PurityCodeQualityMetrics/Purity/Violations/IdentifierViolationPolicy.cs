using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.Purity.Violations;

public class IdentifierViolationPolicy : IViolationPolicy
{
    public List<PurityViolation> Check(SyntaxNode method, SyntaxTree tree, SemanticModel model)
    {
        var ms = model.GetSymbolInfo(method).Symbol ?? model.GetDeclaredSymbol(method);
        
        var violations = method.DescendantNodesInThisFunction()
            .Where(x => !x.IsLogging()) //Ignore logging
            .OfType<IdentifierNameSyntax>()
            .Select(x => new {Node = x, model.GetSymbolInfo(x).Symbol})
            .Where(x => x.Symbol != null)
            .Where(x => x.Node.IsTopLevel() || x.Symbol!.IsStatic)
            .Select(x => //UnkownMethod here is used as a surrogate for 'no violation found'
            {
                
                var symbol = x.Symbol!;
                var type = model.GetTypeInfo(x.Node).Type;

        
                if (symbol.IsEnumConstant() || symbol is IFieldSymbol {IsConst: true}) return PurityViolation.UnknownMethod;

                if (symbol.IsStatic)
                {
                    return x.Node.IsAssignedTo() ? PurityViolation.ModifiesGlobalState : PurityViolation.ReadsGlobalState;
                }
                
                //Check if local fields or properties are used. This is a parametersymbol but is also a local field
                if (symbol is IFieldSymbol || symbol is IParameterSymbol {IsThis: true} || symbol is IPropertySymbol)
                {
                    return x.Node.IsAssignedTo() ? PurityViolation.ModifiesLocalState : PurityViolation.ReadsLocalState;
                }
                //Check if lambda or local function uses parameters of containing method
                if(symbol is IParameterSymbol && !SymbolEqualityComparer.Default.Equals(symbol.ContainingSymbol, ms))
                {
                    return x.Node.IsAssignedTo() ? PurityViolation.ModifiesLocalState : PurityViolation.ReadsLocalState;
                }
                if (symbol is IParameterSymbol && x.Node.IsAssignedToField()  && type.IsReferenceType )
                {
                    return PurityViolation.ModifiesParameter;
                }
                if (symbol is ILocalSymbol ls && ls.IsFresh(x.Node, model) == false  && x.Node.IsAssignedToField() && type.IsReferenceType )
                {
                    return PurityViolation.ModifiesNonFreshObject;
                }
                

                return PurityViolation.UnknownMethod;
            })
            .Where(x => x != PurityViolation.UnknownMethod)
            .ToList();


        return violations;
    }
}