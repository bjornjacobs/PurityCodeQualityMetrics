using Microsoft.CodeAnalysis;

namespace PurityCodeQualityMetrics.Purity;

public static class HelperExtensions
{
    public static bool IsEnumConstant(this ISymbol symbol)
    {
        return symbol.ContainingType is {TypeKind: TypeKind.Enum};
    }
}