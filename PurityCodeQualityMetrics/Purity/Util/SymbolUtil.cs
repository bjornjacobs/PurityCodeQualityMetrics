using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            MethodKind.PropertyGet => symbol.GetNameWithClass(),
            MethodKind.PropertySet => symbol.GetNameWithClass(),
            MethodKind.ExplicitInterfaceImplementation => symbol.GetNameWithClass(),
            MethodKind.UserDefinedOperator => symbol.GetNameWithClass(),
            _ => symbol.GetNameWithClass()
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

    public static bool? IsFresh(this ILocalSymbol fs, SyntaxNode node, SemanticModel model)
    {
        var df = model.AnalyzeDataFlow(node);
        var method = node.GetMethodThatBelongsTo();
        var ass = method.DescendantNodesInThisFunction()
            .Where(x => x is AssignmentExpressionSyntax or VariableDeclaratorSyntax)
            .Where(x => 
                x is AssignmentExpressionSyntax ass && (ass.Left as IdentifierNameSyntax)?.Identifier.Text == fs.Name|| 
                x is VariableDeclaratorSyntax vd && vd.Identifier.Text == fs.Name)
            .ToList();

        var isConstructor = ass.Select(x => x switch
        {
            AssignmentExpressionSyntax ass => model.GetSymbolInfo(ass.Right).Symbol,
            VariableDeclaratorSyntax vd => vd.Initializer == null ? null : model.GetSymbolInfo(vd.Initializer.Value).Symbol
        })
            .Select(x => x is IMethodSymbol ms ? new bool?(ms.MethodKind == MethodKind.Constructor) : null )
            .ToList();

        //If any if the assignments isn't a method we assume it isn't fresh
        if (isConstructor.Any(x => x == null))
            return false;
        
        //If all assignment are constructors we know it's fresh
        if (isConstructor.All(x => x == true))
            return true;
        

        // Otherwise the fresh analyser has wil figure it out
        return null;
    }

}