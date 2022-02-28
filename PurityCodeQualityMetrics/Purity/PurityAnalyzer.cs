﻿using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity;

public enum PurityValue
{
    Pure = 6,
    ThrowsException = 5,
    Undeterministic = 4,
    ParametricallyImpure = 2,
    Impure = 2,
    Unknown = 1,
}

public class PurityRating
{
    public PurityValue GlobalPurityValue => PurityValue.Pure;
    public PurityValue LocalPurityValue => PurityValue.Pure;
    public double PurityPercent => 1.0;
}

public class PurityAnalyzer
{
    public IDictionary<string, PurityRating> AnalyzePurity(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        return new Dictionary<string, PurityRating>();
    }

    readonly public LookupTable LookupTable;

    // Set this to true if enums should be considered to be impure.
    readonly public static bool enumsAreImpure = false;

    // Determines if Analyzer should be verbose or not in its printouts.
    public readonly bool Verbose = false;
    
    public PurityAnalyzer(string file) : this(new List<string> {file}){}
    public PurityAnalyzer(IEnumerable<string> files, bool verbose = false)
    {
        var trees = files.Select(f => CSharpSyntaxTree.ParseText(f)).ToList();
        Verbose = verbose;
        LookupTable = new LookupTable(trees, verbose);
    }
    

    /// <summary>
    /// Analyzes the purity of the given text.
    /// </summary>
    /// <param name="file">The content of the file to analyze</param>
    /// <returns>
    /// A LookupTable containing each method in <paramref name="file"/>,
    /// its dependency set as well as its purity level.
    /// </returns>
    public LookupTable Analyze()
    {
        LookupTable table = LookupTable;
        if (Verbose)
        {
            Console.WriteLine("Lookup table constructed. Calculating purity levels...\n");
        }

        WorkingSet workingSet = table.workingSet;
        bool tableModified = true;

        while (tableModified == true)
        {
            tableModified = false;

            foreach (var method in workingSet)
            {
                // Perform purity checks:

                PurityValue currentPurityValue = table.GetPurity(method);

                if (
                        currentPurityValue == PurityValue.Impure ||
                        currentPurityValue == PurityValue.ThrowsException
                    )
                    // If the method's purity already is Impure we simply
                    // propagate it and move on. Checks for Unknown are done in
                    // a later check in this method.
                {
                    PropagatePurity(method);
                }
                else if (PurityIsKnownPrior(method))
                {
                    SetPurityAndPropagate(method, GetPriorKnownPurity(method));
                }
                else if (method.IsUnsafe())
                {
                    SetPurityAndPropagate(method, PurityValue.Impure);
                }
                else if (ReadsStaticFieldOrProperty(method))
                {
                    SetPurityAndPropagate(method, PurityValue.Impure);
                }
                else if (ThrowsException(method))
                {
                    SetPurityAndPropagate(method, PurityValue.ThrowsException);
                }
                else if (ModifiesNonFreshIdentifier(method) == null)
                {
                    SetPurityAndPropagate(method, PurityValue.Unknown);
                }
                else if (ModifiesNonFreshIdentifier(method) ?? false)
                {
                    SetPurityAndPropagate(method, PurityValue.Impure);
                }
                else if (table.GetPurity(method) == PurityValue.Unknown)
                {
                    PropagatePurity(method);
                }
                else if (method.IsInterfaceMethod())
                    // If `method` is an interface method its purity is set to
                    // `Unknown` since we cannot know its implementation. This
                    // could be handled in the future by looking at all
                    // implementations of `method` and setting its purity level
                    // to the level of the impurest implementation.
                {
                    SetPurityAndPropagate(method, PurityValue.Unknown);
                }
                else if (!method.HasBody())
                {
                    SetPurityAndPropagate(method, PurityValue.Unknown);
                }
                else if (ContainsUnknownIdentifier(method))
                {
                    SetPurityAndPropagate(method, PurityValue.Unknown);
                }
                else
                {
                    RemoveMethodFromCallers(method);
                }
            }

            workingSet.Calculate();
        }

        return table;

        void PropagatePurity(Method method)
        {
            PurityValue purityValue = table.GetPurity(method);
            foreach (var caller in table.GetCallers(method))
            {
                table.SetPurity(caller, purityValue);
                table.RemoveDependency(caller, method);
            }

            tableModified = true;
        }

        /// <summary>
        /// Sets <paramref name="method"/>'s purity level to <paramref name="purity"/>.
        ///
        /// Sets <paramref name="tableModified"/> to true.
        /// </summary>
        void SetPurityAndPropagate(Method method, PurityValue purity)
        {
            table.SetPurity(method, purity);
            PropagatePurity(method);
            tableModified = true;
        }

        // Removes method from callers of method
        void RemoveMethodFromCallers(Method method)
        {
            foreach (var caller in table.GetCallers(method))
            {
                table.RemoveDependency(caller, method);
            }

            tableModified = true;
        }
    }

    /// <summary>
    /// Builds a semantic model
    /// </summary>
    /// <param name="trees">
    /// All trees including <paramref name="tree"/> representing all files
    /// making up the program to analyze </param>
    /// <param name="tree"></param>
    /// <returns></returns>
    public static SemanticModel GetSemanticModel(
        IEnumerable<SyntaxTree> trees,
        SyntaxTree tree
    )
    {
        return CSharpCompilation
            .Create("AnalysisModel")
            .AddReferences(
                MetadataReference.CreateFromFile(
                    typeof(string).Assembly.Location
                )
            )
            .AddSyntaxTrees(trees)
            .GetSemanticModel(tree);
    }

    public static SemanticModel GetSemanticModel(SyntaxTree tree)
    {
        return GetSemanticModel(new List<SyntaxTree> {tree}, tree);
    }

    /// <summary>
    /// Returns the prior known purity level of <paramref name="method"/>.
    /// If the purity level of <paramref name="method"/> is not known
    /// prior, returns Purity.Unknown;
    /// </summary>
    public static PurityValue GetPriorKnownPurity(Method method)
    {
        if (!PurityIsKnownPrior(method)) return PurityValue.Unknown;
        else
            return PurityList.List
                .Single(m => m.Item1 == method.Identifier)
                .Item2;
    }

    /// <summary>
    /// Determines if the purity of <paramref name="method"/> is known in
    /// beforehand.
    ///
    /// Return true if it is, otherwise false.
    /// </summary>
    public static bool PurityIsKnownPrior(Method method)
    {
        return PurityIsKnownPrior(method.Identifier);
    }

    public static bool PurityIsKnownPrior(String methodIdentifier)
    {
        return PurityList.List.Exists(m => m.Item1 == methodIdentifier);
    }

    /// <summary>
    /// Gets a list of all identifiers in a method. Excludes any
    /// identifiers found in an [Attribute].
    /// </summary>
    /// <param name="method">The method</param>
    /// <returns>
    /// All IdentifierNameSyntax's inside <paramref name="method"/>
    /// </returns>
    IEnumerable<IdentifierNameSyntax> GetIdentifiers(Method method)
    {
        if (method.Declaration == null)
        {
            return Enumerable.Empty<IdentifierNameSyntax>();
        }

        IEnumerable<IdentifierNameSyntax> identifiers = method
            .Declaration
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>();

        // Ignore any identifiers found in an [Attribute]
        identifiers = identifiers.Where(
            i => !i.Ancestors().Where(
                a => a.GetType() == typeof(AttributeListSyntax)
            ).Any()
        );

        return identifiers;
    }

    /// <summary>
    /// Checks if identifier is fresh, i.e. declared inside the method.
    /// </summary>
    /// <param name="identifier">Identifier to check if it is fresh</param>
    /// <param name="method">Method that identifier is located in</param>
    /// <returns>
    /// True if <paramref name="identifier"/> is declared inside <paramref
    /// name="method"/>, and false if it is not (including if its
    /// declaration could not be found).
    /// </returns>
    public bool IdentifierIsFresh(ExpressionSyntax identifier, Method method)
    {
        SemanticModel model = PurityAnalyzer.GetSemanticModel(
            LookupTable.trees,
            method.GetRoot().SyntaxTree
        );
        ISymbol symbol = model.GetSymbolInfo(identifier).Symbol;

        // If declaration could not be found the identifier cannot be fresh
        if (symbol == null || symbol.DeclaringSyntaxReferences.Count() < 1)
        {
            return false;
        }

        // If symbol is a parameter it cannot be fresh
        if (symbol.Kind == SymbolKind.Parameter) return false;

        var symbolMethodAncestor = symbol
            .DeclaringSyntaxReferences
            .First() // TODO: check all trees
            .GetSyntax()
            .Ancestors()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        var identifierMethodAncestor = identifier
            .Ancestors()
            .OfType<MethodDeclarationSyntax>()
            .First();

        // Identifier is fresh if it lies inside the same method as its
        // declaration does
        return symbolMethodAncestor == identifierMethodAncestor;
    }

    /// <summary>
    /// Checks if the method modifies an identifier that is no fresh.
    /// </summary>
    /// <param name="method">The method to check</param>
    /// <returns>
    /// True if <paramref name="method"/> modifies an identifier that isn't
    /// fresh, otherwise false. If an identifier's freshness could not be
    /// determined, returns <c>null</c>.
    /// </returns>
    public bool? ModifiesNonFreshIdentifier(Method method)
    {
        IEnumerable<IdentifierNameSyntax> assignees = method.GetAssignees();

        if (assignees.Contains(null)) return null;
        else
            return assignees
                .Union(method.GetUnaryAssignees())
                .Where(i => !IdentifierIsFresh(i, method))
                .Any();
    }

    public bool ReadsStaticFieldOrProperty(Method method)
    {
        IEnumerable<IdentifierNameSyntax> identifiers = GetIdentifiers(method);

        foreach (var identifier in identifiers)
        {
            SemanticModel model = PurityAnalyzer.GetSemanticModel(
                LookupTable.trees,
                identifier.SyntaxTree.GetRoot().SyntaxTree
            );
            ISymbol symbol = model.GetSymbolInfo(identifier).Symbol;
            if (symbol == null) break;

            // If enums are considered to be impure we exclude them from
            // the check, as they will be covered by `isStatic`
            bool isEnum = enumsAreImpure ? false : IsEnum(symbol);

            bool isStatic = symbol.IsStatic;
            bool isField = symbol.Kind == SymbolKind.Field;
            bool isProperty = symbol.Kind == SymbolKind.Property;
            bool isMethod = symbol.Kind == SymbolKind.Method;

            if (isStatic && (isField || isProperty) && !isMethod && !isEnum)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the methods contains an identifier with an unknown
    /// implementation.
    /// </summary>
    /// <param name="method">The method to check</param>
    /// <returns>
    /// False if <paramref name="method"/> has a known implementation or if
    /// it is contained in the <see cref="knownPurities"/> list of known
    /// purities, otherwise true.
    /// </returns>
    public bool ContainsUnknownIdentifier(Method method)
    {
        IEnumerable<IdentifierNameSyntax> identifiers = GetIdentifiers(method);

        foreach (var identifier in identifiers)
        {
            // If the identifier is a parameter it cannot count as unknown
            if (identifier.Parent.Kind() == SyntaxKind.Parameter) continue;

            SemanticModel model = PurityAnalyzer.GetSemanticModel(
                LookupTable.trees,
                identifier.SyntaxTree.GetRoot().SyntaxTree
            );
            ISymbol symbol = model.GetSymbolInfo(identifier).Symbol;

            if (symbol == null)
            {
                // Check if the invocation that `symbol` is part of exists
                // in `knownPurities`, otherwise it's an unknown identifier
                var invocation = identifier
                    .Ancestors()
                    .OfType<InvocationExpressionSyntax>()
                    ?.FirstOrDefault()
                    ?.Expression
                    ?.ToString();

                Console.WriteLine(invocation);
                if (!PurityIsKnownPrior(invocation)) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a symbol is an enumeration.
    /// </summary>
    /// <param name="symbol">
    /// The symbol to check whether or not it is an enumeration.
    /// </param>
    /// <returns>
    /// True if <paramref name="symbol"/> is of the type Enum, otherwise
    /// false.
    /// </returns>
    bool IsEnum(ISymbol symbol)
    {
        if (symbol.ContainingType == null) return false;
        else return symbol.ContainingType.TypeKind == TypeKind.Enum;
    }

    /// <summary>
    /// Determines if method throws an exception.
    ///
    /// Return true if <paramref name="method"/> throws an exception,
    /// otherwise false.
    /// </summary>
    public bool ThrowsException(Method method)
    {
        if (method.Declaration == null) return false;

        IEnumerable<ThrowStatementSyntax> throws = method
            .Declaration
            .DescendantNodes()
            .OfType<ThrowStatementSyntax>();
        return throws.Any();
    }
}