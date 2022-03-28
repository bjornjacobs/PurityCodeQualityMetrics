using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.Util;

public static class GeneralUtil
{
    public static MethodType ToMethodType(this MethodKind mk)
    {
        return mk switch
        {
            MethodKind.AnonymousFunction => MethodType.Lambda,
            MethodKind.Ordinary => MethodType.Method,
            MethodKind.LocalFunction => MethodType.Local,
            MethodKind.ReducedExtension => MethodType.Method, //Extension
            MethodKind.DelegateInvoke => MethodType.Local, 
            MethodKind.PropertyGet => MethodType.Getter, 
            MethodKind.PropertySet => MethodType.Setter, 
            var kind => throw new NotImplementedException($"MethodKind of {kind} is not implemented")
        };
    }

    public static bool IsSimpleProperty(this PropertyDeclarationSyntax property)
    {
        return false;
    }
}