using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeQualityAnalyzer.CodeMetrics
{
    public static class CyclomaticComplexity
    {
        public static int GetCount(SyntaxNode classNode)
        {
            CyclomaticComplexityWalker walker = new CyclomaticComplexityWalker();
            
            return walker.CalculateComplexity(classNode);
        }

        // Definition of CC from: https://ir.cwi.nl/pub/23938

        // https://stackoverflow.com/questions/47102263/cyclomatic-complexity-of-c-sharp-linq


        private class CyclomaticComplexityWalker : CSharpSyntaxWalker
        {
            private int _cyclomaticComplexity = 1;

            public int CalculateComplexity(SyntaxNode node)
            {
                base.Visit(node);

                return _cyclomaticComplexity;
            }

            public override void VisitIfStatement(IfStatementSyntax node)
            {
                _cyclomaticComplexity++;
                base.VisitIfStatement(node);
            }
            public override void VisitWhileStatement(WhileStatementSyntax node)
            {
                _cyclomaticComplexity++;
                base.VisitWhileStatement(node);
            }
            public override void VisitForEachStatement(ForEachStatementSyntax node)
            {
                _cyclomaticComplexity++;
                base.VisitForEachStatement(node);
            }
            public override void VisitForStatement(ForStatementSyntax node)
            {
                _cyclomaticComplexity++;
                base.VisitForStatement(node);
            }
            public override void VisitSwitchSection(SwitchSectionSyntax node)
            {
                _cyclomaticComplexity++;
                base.VisitSwitchSection(node);
            }

            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                if (node.Kind() == SyntaxKind.LogicalAndExpression || node.Kind() == SyntaxKind.LogicalOrExpression) _cyclomaticComplexity++;
                base.VisitBinaryExpression(node);
            }

            public override void VisitCatchClause(CatchClauseSyntax node)
            {
                _cyclomaticComplexity++;
                base.VisitCatchClause(node);
            }

            public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
            {
                _cyclomaticComplexity++;
                base.VisitConditionalExpression(node);
            }

            public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
            {
                _cyclomaticComplexity++;
                base.VisitConditionalAccessExpression(node);
            }

        }
    }
}
