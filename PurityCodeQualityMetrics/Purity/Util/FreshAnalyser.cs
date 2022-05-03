using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.Util;

public static class FreshAnalyser
{
    public static (bool IsFresh, List<string> Dependencies, List<string> ShouldBeFresh) IsReturnFresh(this SyntaxNode node, SemanticModel model)
    {
        IMethodSymbol? method = node.GetMethodSymbol(model);
       // if (method.ReturnsVoid || !method.ReturnType.IsReferenceType) return (true, new List<string>());

        var returns = node.DescendantNodesInThisFunction().OfType<ReturnStatementSyntax>()
            .Select(x => x.Expression!)
            .ToList();

        var dec = node.DescendantNodesInThisFunction()
            .OfType<VariableDeclaratorSyntax>()
            .Select(x => x.Initializer?.Value)
            .ToList();
        
        
        var ass = node.DescendantNodesInThisFunction()
            .OfType<AssignmentExpressionSyntax>()
            .Select(x => x.Right)
            .ToList();
        
        

        bool returnFresh = true;
        
        var process = new Stack<(SyntaxNode, int)>();
        returns.ForEach(x => process.Push((x, 0)));
        ass.ForEach(x => process.Push((x, 1)));
        dec.ForEach(x => process.Push((x, 1)));

        var dependencies = new List<string>();
        var shouldBeFresh = new List<string>();
        while (process.Count > 0)
        {
            var currentNode = process.Pop();
            if (currentNode.Item1 == null) continue;
            var currentSymbol = model.GetSymbolInfo(currentNode.Item1).Symbol;
            if (currentSymbol == null)
                continue;


            if (currentSymbol.Kind == SymbolKind.Method)
            {
                var methodRef = (IMethodSymbol) currentSymbol;
                if (methodRef.MethodKind != MethodKind.Constructor &&
                    methodRef.MethodKind != MethodKind.BuiltinOperator)
                {
                    var name = methodRef.ContainingNamespace.ToUniqueString() + "." +
                               methodRef.GetUniqueMethodName(currentNode.Item1);
                    if (currentNode.Item2 == 0)
                    {
                        dependencies.Add(name);
                    }
                    else
                    {
                        shouldBeFresh.Add(name);
                    }
                }

                continue;
            }

            //Check if variable is local
            if (currentSymbol.Kind is not (SymbolKind.Local or SymbolKind.Parameter))
                returnFresh = false;

            var id = currentNode.Item1 as IdentifierNameSyntax;

            var declarationSyntax = node.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Where(x => x.Identifier.Text == id?.Identifier.Text)
                .Select(x => x.Initializer?.Value).FirstOrDefault();

            if (declarationSyntax != null)
                process.Push((declarationSyntax, currentNode.Item2));

            //Check all assignments
            var assignments = node.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Select(x => x.Right).ToList();

            assignments.ForEach(x => process.Push((x, currentNode.Item2)));
        }

        return (returnFresh, dependencies, shouldBeFresh);
    }
}