using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.CsPurity;

public class CSharpMethod
{
    public string Identifier;
    public MethodDeclarationSyntax Declaration;
    public bool IsLocalFunction;
    public bool IsDelegateFunction;

    /// <summary>
    /// If <paramref name="methodInvocation"/>'s declaration was found <see
    /// cref="Declaration"/> is set to that and  <see cref="Identifier"/>
    /// set to null instead.
    ///
    /// If no declaration was found, <see cref="Declaration"/> is set to
    /// null and <see cref="Identifier"/> set to <paramref
    /// name="methodInvocation"/>'s identifier instead.
    ///
    /// If the method is a local function, i.e. declared inside a method,
    /// isLocalFunction is set to true, otherwise it is false.
    ///
    /// <param name="methodInvocation"></param>
    /// <param name="model"></param>
    public CSharpMethod(InvocationExpressionSyntax methodInvocation, SemanticModel model)
    {
        ISymbol symbol = ModelExtensions.GetSymbolInfo(model, methodInvocation).Symbol;
        if (symbol == null)
        {
            SetIdentifier(methodInvocation);
            return;
        }

        var declaringReferences = symbol.DeclaringSyntaxReferences;
        var methodSymbol = (IMethodSymbol) symbol;

        if (declaringReferences.Length < 1)
        {
            SetIdentifier(methodInvocation);
        }
        else if (methodSymbol.MethodKind == MethodKind.LocalFunction)
        {
            // Handles local functions
            IsLocalFunction = true;
            Identifier = "*local function*";
        }
        else if (
            methodSymbol.MethodKind == MethodKind.DelegateInvoke ||
            declaringReferences
                .Single()
                .GetSyntax()
                .Kind() == SyntaxKind
                .DelegateDeclaration
        )
        {
            // Handles delegates, including the case of the methods
            // BeginInvoke and EndInvoke
            Identifier = "*delegate invocation";
            IsDelegateFunction = true;
        }
        else if (
            declaringReferences.Single().GetSyntax().Kind()
            == SyntaxKind.ConversionOperatorDeclaration
        )
        {
            // Handles the rare case where GetSyntax() returns the operator
            // for an implicit conversion instead of the invoked method
            Identifier = "*conversion operator*";
        }
        else
        {
            // Not sure if this cast from SyntaxNode to
            // `MethodDeclarationSyntax` always works
            Declaration = (MethodDeclarationSyntax) declaringReferences
                .Single()
                .GetSyntax();
        }
    }

    public CSharpMethod(MethodDeclarationSyntax declaration)
        : this(declaration.Identifier.Text)
    {
        Declaration = declaration;
    }

    public CSharpMethod(string identifier)
    {
        Identifier = identifier;
    }

    void SetIdentifier(InvocationExpressionSyntax methodInvocation)
    {
        Identifier = methodInvocation.Expression.ToString();
        Identifier = Regex.Replace(Identifier, @"[\s,\n]+", "");
    }

    public bool HasKnownDeclaration()
    {
        return Declaration != null;
    }

    public SyntaxNode GetRoot()
    {
        return Declaration?.SyntaxTree.GetRoot();
    }

    public bool HasEqualSyntaxTreeTo(CSharpMethod cSharpMethod)
    {
        return GetRoot().Equals(cSharpMethod.GetRoot());
    }

    /// <summary>
    /// Checks if method is an interface method, ie a method declared
    /// inside an interface.
    /// </summary>
    /// <returns>
    /// True if method is an interace method, otherwise false.
    /// </returns>
    public bool IsInterfaceMethod()
    {
        if (Declaration == null) return false;
        return Declaration
            .Parent
            .Kind()
            .Equals(SyntaxKind.InterfaceDeclaration);
    }

    /// <summary>
    /// Checks if method has the unsafe modifier.
    /// </summary>
    /// <returns>
    /// True if this method, its class or its struct has the unsafe
    /// modifer, otherwise false.
    /// </returns>
    public bool IsUnsafe()
    {
        if (Declaration == null) return false;
        bool unsafeMethod = ContainsUnsafeKeyword(Declaration);
        bool unsafeClass = ContainsUnsafeKeyword(
            Declaration.Ancestors().OfType<ClassDeclarationSyntax>()
        );
        bool unsafeStruct = ContainsUnsafeKeyword(
            Declaration.Ancestors().OfType<StructDeclarationSyntax>()
        );
        return unsafeMethod || unsafeClass || unsafeStruct;
    }

    bool ContainsUnsafeKeyword(MemberDeclarationSyntax node)
    {
        return ContainsUnsafeKeyword(new[] {node});
    }

    bool ContainsUnsafeKeyword(IEnumerable<MemberDeclarationSyntax> nodes)
    {
        return nodes.Any(n => 
            n.Modifiers.Any(m => m.IsKind(SyntaxKind.UnsafeKeyword)));
    }

    /// <summary>
    /// Determines if method has a [Pure] attribute.
    /// </summary>
    /// <returns>
    /// True if method has a [Pure] attribute, otherwise false.
    /// </returns>
    public bool HasPureAttribute()
    {
        return Declaration.DescendantNodes()
            .OfType<AttributeListSyntax>()
            .Any(attributeList =>
                attributeList.Attributes.Any(attribute => attribute.Name.ToString().ToLower() == "pure"));
    }

    public bool HasBody()
    {
        return Declaration?.Body != null || Declaration?.ExpressionBody != null;
    }

    /// <summary>
    /// Gets the base identifier from an expression, i.e. the leftmost
    /// identifier in a member access expression, or
    /// in the case of an array access expression, the array's identifier.
    /// </summary>
    /// <param name="expression">The expression</param>
    /// <returns>
    /// <paramref name="expression"/>'s leftmost identifier (exluding
    /// <c>this</c>). If the identifier could not be found, <c>null</c>.
    /// </returns>
    public static IdentifierNameSyntax GetBaseIdentifiers(ExpressionSyntax expression)
    {
        while (true)
        {
            if (expression is MemberAccessExpressionSyntax memberExpr)
            {
                expression = memberExpr.Expression;
            }
            else if (expression is ElementAccessExpressionSyntax elementExpr)
            {
                expression = elementExpr.Expression;
            }
            else if (expression is ThisExpressionSyntax thisExpr)
            {
                return thisExpr
                    .Parent
                    .DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .First(); // Assumes that there's only one identifier
            }
            else if (expression is ParenthesizedExpressionSyntax parenExpr)
            {
                expression = parenExpr.Expression;
            }
            else if (expression is InvocationExpressionSyntax invocationExpr)
            {
                expression = invocationExpr.Expression;
            }
            else if (expression is IdentifierNameSyntax identifier)
            {
                return identifier;
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Recursively flattens a given TupleExpressionSyntax into an
    /// IEnumerable.
    /// </summary>
    /// <param name="tuple">The TupleExpressionSyntax to flatten</param>
    /// <returns>
    /// An IEnumerable with the expressions contained in <paramref
    /// name="tuple"/> and all potential subtuples.
    /// </returns>
    public static IEnumerable<ExpressionSyntax> FlattenTuple(
        TupleExpressionSyntax tuple
    )
    {
        var result = new List<ExpressionSyntax>();

        foreach (var argument in tuple.Arguments)
        {
            var expr = argument.Expression;
            if (expr is TupleExpressionSyntax subTuple)
            {
                result = result
                    .Concat(FlattenTuple(subTuple))
                    .ToList();
            }
            else
            {
                result.Add(expr);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all identifiers that are assigned to inside the method.
    /// </summary>
    /// <returns>
    /// The identifiers that are assigned to with `=`. If an assignee could
    /// not be determined, it will be set to <c>null</c>.
    /// </returns>
    public IEnumerable<IdentifierNameSyntax> GetAssignees()
    {
        if (!HasKnownDeclaration())
        {
            return Enumerable.Empty<IdentifierNameSyntax>();
        }

        var assignments = Declaration
            .DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Where(a => !(a.Left is DeclarationExpressionSyntax));

        IEnumerable<IdentifierNameSyntax> nonTuples = assignments
            .Where(a => !(a.Left is TupleExpressionSyntax))
            .Select(a => GetBaseIdentifiers(a.Left));

        IEnumerable<IdentifierNameSyntax> tupleExpressions = assignments
            .Where(a => a.Left is TupleExpressionSyntax)
            .SelectMany(t => FlattenTuple((TupleExpressionSyntax) t.Left))
            .Where(t => !(t is DeclarationExpressionSyntax))
            .Select(e => GetBaseIdentifiers(e));

        return nonTuples.Concat(tupleExpressions);
    }

    /// <summary>
    /// Gets all identifiers that are assigned to inside the method.
    /// </summary>
    /// <returns>
    /// The identifiers that are assigned to with a unary expression. If an
    /// assignee could not be determined, it will be set to <c>null</c>.
    /// </returns>
    public IEnumerable<IdentifierNameSyntax> GetUnaryAssignees()
    {
        if (!HasKnownDeclaration())
        {
            return Enumerable.Empty<IdentifierNameSyntax>();
        }

        return Declaration
            .DescendantNodes()
            .OfType<PostfixUnaryExpressionSyntax>()
            .Where(u => IsUnaryAssignment(u))
            .Select(a => GetBaseIdentifiers(a.Operand))
            .Union(Declaration
                .DescendantNodes()
                .OfType<PrefixUnaryExpressionSyntax>()
                .Where(u => IsUnaryAssignment(u))
                .Select(a => GetBaseIdentifiers(a.Operand))
            );

        // Some UnaryExpressions are not assignments
        static bool IsUnaryAssignment(ExpressionSyntax expression)
        {
            SyntaxKind kind = expression.Kind();
            return kind.Equals(SyntaxKind.PostIncrementExpression)
                   || kind.Equals(SyntaxKind.PreIncrementExpression)
                   || kind.Equals(SyntaxKind.PostDecrementExpression)
                   || kind.Equals(SyntaxKind.PreDecrementExpression);
        }
    }

    public override bool Equals(Object obj)
    {
        if (!(obj is CSharpMethod)) return false;
        CSharpMethod m = obj as CSharpMethod;
        if (HasKnownDeclaration() && m.HasKnownDeclaration())
        {
            return m.Declaration == Declaration;
        }

        if (!HasKnownDeclaration() && !m.HasKnownDeclaration())
        {
            return m.Identifier == Identifier;
        }

        return false;
    }

    public override int GetHashCode()
    {
        if (HasKnownDeclaration()) return Declaration.GetHashCode();
        return Identifier.GetHashCode();
    }

    public override string ToString()
    {
        if (!HasKnownDeclaration()) return Identifier;

        string returnType = Declaration.ReturnType.ToString();
        string methodName = Declaration.Identifier.Text;
        var classAncestors = Declaration
            .Ancestors()
            .OfType<ClassDeclarationSyntax>();

        if (classAncestors.Any())
        {
            SyntaxToken classIdentifier = classAncestors.First().Identifier;
            string className = classIdentifier.Text;
            string pureAttribute = HasPureAttribute() ? "[Pure] " : "";
            return $"{pureAttribute}{returnType} {className}.{methodName}";
        }

        // If no ancestor is a class declaration, look for struct
        // declarations
        var structAncestors = Declaration
            .Ancestors()
            .OfType<StructDeclarationSyntax>();

        if (structAncestors.Any())
        {
            string structName = structAncestors.First().Identifier.Text;
            string pureAttribute = HasPureAttribute() ? "[Pure] " : "";
            return $"(struct) {pureAttribute}{returnType} {structName}.{methodName}";
        }

        return $"{returnType} *no class/identifier* {methodName}";
    }
}