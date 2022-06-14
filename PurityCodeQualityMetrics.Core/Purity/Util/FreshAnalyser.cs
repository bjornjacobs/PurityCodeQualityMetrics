using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.Util;

public static class FreshAnalyser
{
    private enum CheckType
    {
        ReturnValue,
        FreshDependency
    }
    
    public static (bool IsFresh, List<string> Dependencies, List<string> ShouldBeFresh) IsReturnFresh(this SyntaxNode node, SemanticModel model)
    {
        IMethodSymbol? method = node.GetMethodSymbol(model);
        var queue = new QueueOnlyOnce<(SyntaxNode Node, CheckType Type)>();
        
        node.DescendantNodesInThisFunction().OfType<ReturnStatementSyntax>()
            .Select(x => x.Expression!)
            .ToList()
            .ForEach(x => queue.Enqueue((x, CheckType.ReturnValue)));

        node.DescendantNodesInThisFunction()
            .OfType<VariableDeclaratorSyntax>()
            .Select(x => x.Initializer?.Value)
            .ToList()
            .ForEach(x => queue.Enqueue((x, CheckType.FreshDependency)));
        
        node.DescendantNodesInThisFunction()
            .OfType<AssignmentExpressionSyntax>()
            .Select(x => x.Right)
            .ToList()
            .ForEach(x => queue.Enqueue((x, CheckType.FreshDependency)));
        
        
        bool returnIsFresh = true;
        var dependencies = new List<string>();
        var shouldBeFresh = new List<string>();
        while (queue.HasItems)
        {
            var currentNode = queue.Dequeue();
            if(currentNode.Node == null) continue;
            
            
            var currentSymbol = model.GetSymbolInfo(currentNode.Node).Symbol;
            if (currentSymbol == null)
                continue;
            
            if (currentSymbol.Kind == SymbolKind.Method)
            {
                var methodRef = (IMethodSymbol) currentSymbol;
                if (methodRef.MethodKind != MethodKind.Constructor &&
                    methodRef.MethodKind != MethodKind.BuiltinOperator)
                {
                    var name = methodRef.ContainingNamespace.ToUniqueString() + "." +
                               methodRef.GetUniqueMethodName(currentNode.Node);
                    if (currentNode.Type == CheckType.ReturnValue)
                        dependencies.Add(name);
                    else if(currentNode.Type == CheckType.FreshDependency)
                        shouldBeFresh.Add(name);
                }

                continue;
            }
            else if (currentSymbol.Kind is not SymbolKind.Local)
            {
                returnIsFresh = false;
            }
            
            
            var id = currentNode.Node as IdentifierNameSyntax;
            var declarationSyntax = node.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Where(x => x.Identifier.Text == id?.Identifier.Text)
                .Select(x => x.Initializer?.Value).FirstOrDefault();

            if (declarationSyntax != null)
                queue.Enqueue((declarationSyntax, currentNode.Type));

            //Check all assignments
            var assignments = node.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Select(x => x.Right).ToList();

            assignments.ForEach(x => queue.Enqueue((x, currentNode.Type)));
        }

        //If void or value type the returns value is always fresh
        if (method.ReturnsVoid || !method.ReturnType.IsReferenceType)
            returnIsFresh = true;
        
        return (returnIsFresh, dependencies, shouldBeFresh);
    }
}

/// <summary>
/// A queue but a object is only allowed to be in the queue once.
/// Used to keep a memory of what items have been checked in the past.
/// </summary>
public class QueueOnlyOnce<T>
{
    private readonly HashSet<T> _memory = new ();
    private readonly Queue<T> _queue = new ();

    public int Count => _queue.Count;
    public bool HasItems => Count > 0;

    public T Dequeue()
    {
        return _queue.Dequeue();
    }

    public void Enqueue(T val)
    {
        if(val == null)
            return;
        
        if(_memory.Contains(val))
            return;
        
        _queue.Enqueue(val);
        _memory.Add(val);
    }
}