using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.Purity.Violations;

public class IdentifierViolationPolicy : IViolationPolicy
{
    public List<PurityViolation> Check(SyntaxNode method, SyntaxTree tree, SemanticModel model)
    {
        var violations = method.DescendantNodesInThisFunction()
            .OfType<IdentifierNameSyntax>()
            .Select(x => new {Node = x, model.GetSymbolInfo(x).Symbol})
            .Where(x => x.Symbol != null)
            .Where(x => x.Node.IsTopLevel() || x.Symbol!.IsStatic)
            .Select(x =>
            {
                
                var symbol = x.Symbol!;
                var type = model.GetTypeInfo(x.Node).Type;


                if (symbol.IsEnumConstant() || symbol is IFieldSymbol {IsConst: true}) return PurityViolation.UnknownMethod;

                if (symbol.IsStatic)
                {
                    return x.Node.IsAssignedTo() ? PurityViolation.ModifiesGlobalState : PurityViolation.ReadsGlobalState;
                }
                if (symbol is IFieldSymbol || symbol is IParameterSymbol {IsThis: true})
                {
                    return x.Node.IsAssignedTo() ? PurityViolation.ModifiesLocalState : PurityViolation.ReadsLocalState;
                }

                if (symbol is IParameterSymbol)
                {
                    return x.Node.IsAssignedToField() && type.IsReferenceType ? PurityViolation.ModifiesParameter : PurityViolation.UnknownMethod;
                }
                

                return PurityViolation.UnknownMethod;
            })
            .Where(x => x != PurityViolation.UnknownMethod)
            .ToList();


        return violations;
    }
}