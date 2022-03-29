using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeQualityAnalyzer.CodeMetrics
{
    public static class LackOfCohesionOfMethods
    {
        public static int GetCount(ClassDeclarationSyntax classNode, SemanticModel semanticModel)
        {
            var methods = classNode.DescendantNodes()
                .OfType<MethodDeclarationSyntax>();
//                .Where(x => x is MethodDeclarationSyntax);

            Dictionary<string, HashSet<string>> methodFieldVariables = new Dictionary<string, HashSet<string>>();

            foreach (MethodDeclarationSyntax method in methods)
            {
                string methodName = method.Identifier.ValueText;

                IEnumerable<SyntaxNode> variablesUsed = method
                    .DescendantNodes()
                    .Where(x => x is IdentifierNameSyntax);
                HashSet<string> fieldVariables = new HashSet<string>();
                foreach (SyntaxNode syntaxNode in variablesUsed)
                {
                    var symbol = semanticModel.GetSymbolInfo(syntaxNode);

                    if (symbol.Symbol is null) continue;

                    if (symbol.Symbol.Kind is SymbolKind.Field) fieldVariables.Add(symbol.Symbol.Name);

                }

                if (methodFieldVariables.ContainsKey(methodName))
                {
                    methodFieldVariables[methodName].UnionWith(fieldVariables);
                }
                else
                {
                    methodFieldVariables.Add(methodName, fieldVariables);
                }

            }

            List<string> methodNames = methodFieldVariables.Keys.ToList();

            int pairsWithoutSharedFieldVariables = 0;
            int pairsWithSharedFieldVariables = 0;

            for (int i = 0; i < methodNames.Count - 1; i++)
            {
                for (int j = i + 1; j < methodNames.Count; j++)
                {
                    HashSet<string> a = methodFieldVariables[methodNames[i]];
                    HashSet<string> b = methodFieldVariables[methodNames[j]];
                    if (a.Intersect(b).Any())
                    {
                        pairsWithSharedFieldVariables++;
                    }
                    else
                    {
                        pairsWithoutSharedFieldVariables++;
                    }
                }

            }

            int lcom = pairsWithoutSharedFieldVariables - pairsWithSharedFieldVariables;
            return Math.Max(0, lcom);
        }
    }
}
