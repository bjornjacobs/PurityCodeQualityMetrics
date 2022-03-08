﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.Violations;

public class ReadsStaticFieldViolationPolicy : IViolationPolicy
{
    public List<PurityViolation> Check(MethodDeclarationSyntax method, SyntaxTree tree, SemanticModel model)
    {
        IEnumerable<IdentifierNameSyntax> identifiers = method
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>();

        return identifiers
            .Where(x => x.Parent is not AssignmentExpressionSyntax)
            .Where(x =>
        {
            ISymbol? symbol = model.GetSymbolInfo(x).Symbol;
            if (symbol == null) return false;
            
            bool isStatic = symbol.IsStatic;
            bool isField = symbol.Kind == SymbolKind.Field;
            bool isProperty = symbol.Kind == SymbolKind.Property;
            bool isMethod = symbol.Kind == SymbolKind.Method;
            bool isEnumConstant = symbol.IsEnumConstant();

            return isStatic && (isField || isProperty) && !isMethod && !isEnumConstant;
        }).Select(x => PurityViolation.ReadsGlobalState).ToList();
        
    }
}