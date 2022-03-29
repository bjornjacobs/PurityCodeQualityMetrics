using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeQualityAnalyzer.CodeMetrics
{
    public static class LambdaMetrics
    {

        public static LambdaMetricsResults GetValueList(ClassDeclarationSyntax classDecl, SemanticModel model)
        {
            LambdaWalker walker = new LambdaWalker(model, classDecl);

            return walker.LambdaMetricsResults;
        }

        public class LambdaWalker : CSharpSyntaxWalker
        {
            private readonly SemanticModel _semanticModel;
            public LambdaMetricsResults LambdaMetricsResults;
            

            public LambdaWalker(SemanticModel model, ClassDeclarationSyntax node) : base(SyntaxWalkerDepth.Token)
            {
                _semanticModel = model;
                LambdaMetricsResults = new LambdaMetricsResults();

                base.Visit(node);
            }

            public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
            {
                LambdaExpressionHandler(node);
                base.VisitSimpleLambdaExpression(node);
            }

            public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                LambdaExpressionHandler(node);
                base.VisitParenthesizedLambdaExpression(node);
            }

            private void LambdaExpressionHandler(LambdaExpressionSyntax node)
            {
                LambdaMetricsResults.LambdaCount++;

                IEnumerable<IdentifierNameSyntax> variablesUsed = node
                    .DescendantNodes()
                    .OfType<IdentifierNameSyntax>();

                foreach (IdentifierNameSyntax variableUsed in variablesUsed)
                {
                    var symbol = _semanticModel.GetSymbolInfo(variableUsed);

                    if (symbol.Symbol is null) continue;

                    switch (symbol.Symbol.Kind)
                    {
                        case SymbolKind.Local when !((ILocalSymbol) symbol.Symbol).HasConstantValue:
                            LambdaMetricsResults.LocalVariableUsageCount++;
                            break;
                        case SymbolKind.Field when !((IFieldSymbol) symbol.Symbol).HasConstantValue:
                            LambdaMetricsResults.FieldVariableUsageCount++;
                            break;
                        default:
                            continue;
                    }

                    if (IsMutated(variableUsed)) LambdaMetricsResults.SideEffects++;
                }
            }

            private bool IsMutated(IdentifierNameSyntax variable)
            {
                // meaning parent = "variable++" or "variable--"
                var parent = variable.Parent;
                if (parent is PostfixUnaryExpressionSyntax) return true;
                
                // meaning the variable is the first variable in an assignment, therefore its mutated.
                return parent is AssignmentExpressionSyntax && parent.ChildNodes().First() == variable;
            }
        }
    }

    public class LambdaMetricsResults
    {
        public int LambdaCount;
        public int LocalVariableUsageCount;
        public int FieldVariableUsageCount;
        public int SideEffects;

    }
}
