using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.CodeMetrics
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
            var result = new Dictionary<INamedTypeSymbol, int>();

            foreach (SyntaxTree syntaxTree in syntaxTrees)
            {
                var model = comp.GetSemanticModel(syntaxTree);
                IEnumerable<ClassDeclarationSyntax> classDeclarations = syntaxTree.GetRoot().GetClassesFromRoot();
                foreach (var classDeclaration in classDeclarations)
                {
                    var self = ((ITypeSymbol) model.GetDeclaredSymbol(classDeclaration));
                    INamedTypeSymbol parent = self.BaseType;
                    if (result.ContainsKey(parent))
                    {
                        result[parent]++;
                    }
                    else
                    {
                        result.Add(parent, 1);    
                    }

                }
            }

            return result;
        }
    }
}
