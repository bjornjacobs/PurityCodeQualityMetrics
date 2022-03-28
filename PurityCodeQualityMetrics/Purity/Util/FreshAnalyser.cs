using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.Purity;

public static class FreshAnalyser
{
    public static (bool IsFresh, List<MethodDependency> Dependencies) IsReturnFresh(this SyntaxNode node, SemanticModel model, Solution solution)
    {
        IMethodSymbol? method = node.GetMethodSymbol(model);
        
        if (method.ReturnsVoid || !method.ReturnType.IsReferenceType) return (true, new List<MethodDependency>());

        var m = (MethodDeclarationSyntax) node;
        var controlFlow = model.AnalyzeControlFlow(m.Body);

        var returns = controlFlow.ReturnStatements
            .Cast<ReturnStatementSyntax>()
            .Select(x => x.Expression)
            .ToList();

        var process = new Stack<SyntaxNode>();
        returns.ForEach(x => process.Push(x));

        var dependencies = new List<MethodDependency>();
        while (process.Count > 0)
        {
            var currentNode = process.Pop();
            var currentSymbol = model.GetSymbolInfo(currentNode).Symbol!;
            
            if (currentSymbol.Kind == SymbolKind.Method)
            {
                var methodRef = (IMethodSymbol) currentSymbol;
                if (methodRef.MethodKind != MethodKind.Constructor)
                    dependencies.Add(new MethodDependency(methodRef.GetUniqueMethodName(currentNode),
                        methodRef.ContainingNamespace.ToUniqueString(),
                        methodRef.ReturnType.ToUniqueString(),
                        methodRef.Parameters.Select(x => x.Type.ToUniqueString()).ToList(),
                        methodRef.MethodKind.ToMethodType(),
                        methodRef.IsAbstract,
                        Scoping.Field,
                        true));
                
                continue;
            }
            
            //Check if variable is local
            if (currentSymbol.Kind is not (SymbolKind.Local or SymbolKind.Parameter))
                return (false, new List<MethodDependency>());
            
            var id = currentNode as IdentifierNameSyntax;

            var declarationSyntax = m.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Where(x => x.Identifier.Text == id.Identifier.Text)
                .Select(x => x.Initializer?.Value).Single();
            
            if(declarationSyntax != null)
                process.Push(declarationSyntax);

            //Check all assignments

            var assignments = m.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Where(x => (x.Left as IdentifierNameSyntax)?.Identifier.Text == id?.Identifier.Text)
                .Select(x => x.Right).ToList();
            
            assignments.ForEach(process.Push);
        }

        return (true, dependencies);
    }
}