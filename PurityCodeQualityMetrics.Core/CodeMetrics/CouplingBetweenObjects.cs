using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.CodeMetrics
{
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1024:Symbols should be compared for equality")]
    public static class CouplingBetweenObjects
    {
        public static int GetCount(SyntaxNode classNode, SemanticModel model, Dictionary<INamedTypeSymbol, int> classCouplings)
        {
            INamedTypeSymbol className = (INamedTypeSymbol)model.GetDeclaredSymbol(classNode);

            return classCouplings[className];
        }

        public static Dictionary<INamedTypeSymbol, int> CalculateCouplings(List<SyntaxTree> syntaxTrees, Compilation comp)
        {
            var outgoingReferences = new Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>>();

            foreach (SyntaxTree syntaxTree in syntaxTrees)
            {
                var model = comp.GetSemanticModel(syntaxTree);
                IEnumerable<ClassDeclarationSyntax> classDeclarations =syntaxTree.GetRoot().GetClassesFromRoot();
                foreach (var classDeclaration in classDeclarations)
                {
                    var self = model.GetDeclaredSymbol(classDeclaration);
                 
                    var outgoingCoupling = GetReferences(classDeclaration, model);
                    // This scenario happens when there is a class split into partial classes
                    if (outgoingReferences.ContainsKey(self))
                    {
                        outgoingReferences[self].UnionWith(outgoingCoupling);
                    }
                    else
                    {
                        outgoingReferences.Add(self, outgoingCoupling);
                    }
                }
            }

            Dictionary<INamedTypeSymbol, int> classCouplings = new Dictionary<INamedTypeSymbol, int>();

            foreach (INamedTypeSymbol className in outgoingReferences.Keys)
            {
                if (!classCouplings.ContainsKey(className))
                {
                    classCouplings.Add(className, 0);
                }
                foreach (INamedTypeSymbol reference in outgoingReferences[className])
                {
                    if (!outgoingReferences.ContainsKey(reference)) continue;
                    classCouplings[className]++;

                    if (classCouplings.ContainsKey(reference))
                    {
                        classCouplings[reference]++;
                    }
                    else
                    {
                        classCouplings.Add(reference, 1);
                    }
                }
            }

            return classCouplings;
        }

        private static HashSet<INamedTypeSymbol> GetReferences(SyntaxNode classNode, SemanticModel model)
        {
            HashSet<INamedTypeSymbol> references = new HashSet<INamedTypeSymbol>();
            IEnumerable<SyntaxNode> identifierNames = classNode.DescendantNodes()
                .Where(x => x is IdentifierNameSyntax);

            foreach (var identifierName in identifierNames)
            {
                var identifier = model.GetTypeInfo(identifierName).Type;
                if (identifier is null) continue;
                if(identifier is INamedTypeSymbol) references.Add((INamedTypeSymbol)identifier);
            }

            return references;
        }
    }
}
