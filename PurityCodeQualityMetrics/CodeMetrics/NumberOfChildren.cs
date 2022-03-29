using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeQualityAnalyzer.CodeMetrics
{
    public static class NumberOfChildren
    {
        public static int GetCount(ClassDeclarationSyntax classDeclaration, SemanticModel model, Dictionary<INamedTypeSymbol, int> classExtensions)
        {
            INamedTypeSymbol className = (INamedTypeSymbol)model.GetDeclaredSymbol(classDeclaration);

            return !classExtensions.ContainsKey(className) ? 0 : classExtensions[className];
        }

        public static Dictionary<INamedTypeSymbol, int> GetClassExtensions(List<SyntaxTree> syntaxTrees, Compilation comp)
        {
            var bla = new Dictionary<INamedTypeSymbol, int>();

            foreach (SyntaxTree syntaxTree in syntaxTrees)
            {
                var model = comp.GetSemanticModel(syntaxTree);
                IEnumerable<ClassDeclarationSyntax> classDeclarations = MetricRunner.GetClassesFromRoot(syntaxTree.GetRoot());
                foreach (var classDeclaration in classDeclarations)
                {
                    var self = ((ITypeSymbol) model.GetDeclaredSymbol(classDeclaration));
                    INamedTypeSymbol parent = self.BaseType;
                    if (bla.ContainsKey(parent))
                    {
                        bla[parent]++;
                    }
                    else
                    {
                        bla.Add(parent, 1);    
                    }

                }
            }

            return bla;
        }
    }
}
