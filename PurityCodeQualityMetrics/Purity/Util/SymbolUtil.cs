using Microsoft.CodeAnalysis;

namespace PurityCodeQualityMetrics.Purity.Util;

public static class SymbolUtil
{
    public static bool IsEnumConstant(this ISymbol symbol)
    {
        return symbol.ContainingType is {TypeKind: TypeKind.Enum};
    }
    
    public static string GetUniqueMethodName(this IMethodSymbol symbol, SyntaxNode node)
    {
        return symbol.MethodKind switch
        {
            MethodKind.Ordinary => symbol.GetNameWithClass(),
            MethodKind.LocalFunction =>  symbol.ContainingSymbol.GetNameWithClass() + ".<local>." + symbol.Name,
            MethodKind.AnonymousFunction => symbol.ContainingSymbol.GetNameWithClass() + ".<lambda>." + node.GetLambdaCount(),
            MethodKind.ReducedExtension => symbol.GetNameWithClass(),
            MethodKind.DelegateInvoke => symbol.GetNameWithClass(),
            _ => throw new NotImplementedException()
        };
    }

    public static string ToUniqueString(this ISymbol node)
    {
        return node.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
    }
    
    public static string GetNameWithClass(this ISymbol node)
    {
        return node.ContainingSymbol.Name + "." + node.Name;
    }

}