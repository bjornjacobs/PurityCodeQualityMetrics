using Microsoft.CodeAnalysis;

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
            var kind => throw new NotImplementedException($"MethodKind of {kind} is not implemented")
        };
    }
}