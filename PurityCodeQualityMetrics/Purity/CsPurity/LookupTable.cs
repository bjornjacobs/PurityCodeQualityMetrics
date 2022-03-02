using System.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity.CsPurity;

public static class Ext
{
    public static CSharpMethod GetMethodByName(
        this LookupTable lookupTable,
        string name
    )
    {
        foreach (var tree in lookupTable.Trees)
        {
            var methodDeclarations = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text == name);
            if (methodDeclarations.Any())
            {
                return new CSharpMethod(methodDeclarations.Single());
            }
        }

        return null;
    }
}

public class LookupTable
{
    public readonly DataTable Table = new DataTable();
    public readonly WorkingSet WorkingSet;
    public readonly IEnumerable<SyntaxTree> Trees;
    public bool Verbose = false;

    public LookupTable()
    {
        Table.Columns.Add("identifier", typeof(CSharpMethod));
        Table.Columns.Add("dependencies", typeof(IEnumerable<CSharpMethod>));
        Table.Columns.Add("purity", typeof(PurityValue));
    }
    public LookupTable(SyntaxTree tree) : this(new List<SyntaxTree> { tree }) { }

    public LookupTable(IEnumerable<SyntaxTree> trees) : this(trees, false)
    {
    }

    public LookupTable(IEnumerable<SyntaxTree> trees, bool verbose) : this()
    {
        Trees = trees;

        BuildLookupTable(verbose);
        WorkingSet = new WorkingSet(this);
    }
    

    // Creates a LookupTable with the content of `table`
    public LookupTable(DataTable table, LookupTable lt)
    {
        this.Trees = lt.Trees;
        this.Table = table.Copy();
    }

    public LookupTable Copy()
    {
        return new LookupTable(Table, this);
    }

    /// <summary>
    /// Builds the lookup table and calculates each method's dependency
    /// set.
    /// <param name="verbose">
    /// Determines if lookup table should print out
    /// each method while being constructed.
    /// </param>
    /// </summary>
    public void BuildLookupTable(bool verbose)
    {
        foreach (var tree in Trees)
        {
            var methodDeclarations = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>();
            
            var lambdaExpressions = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<SimpleLambdaExpressionSyntax>();
            
            
            foreach (var methodDeclaration in methodDeclarations)
            {
                CSharpMethod cSharpMethod = new CSharpMethod(methodDeclaration);

                // Ignore interface methods which also show up as
                // MethodDeclarationSyntaxes
                if (!cSharpMethod.IsInterfaceMethod())
                {
                    if (verbose)
                    {
                        Console.WriteLine($"Calculating dependencies for {cSharpMethod}.");
                    }

                    var dependencies = CalculateDependencies(cSharpMethod);
                    AddMethod(cSharpMethod);
                    foreach (var dependency in dependencies)
                    {
                        AddDependency(cSharpMethod, dependency);
                    }
                }
            }
        }
    }

    // This method is private since dependencies get removed after
    // calculating purities. See method CalculateDependencies().
    private IEnumerable<CSharpMethod> GetDependencies(CSharpMethod cSharpMethod)
    {
        return GetMethodRow(cSharpMethod).Field<IEnumerable<CSharpMethod>>("dependencies");
    }

    /// <summary>
    /// Computes a list of all unique methods that a method depends on. If
    /// any method doesn't have a known declaration, its purity level is
    /// set to `Unknown`. If an interface method invocation was found, the
    /// invoker's purity is set to `Unknown` since the invoked method could
    /// have any implementation.
    /// </summary>
    /// <param name="cSharpMethod">The method</param>
    /// <returns>
    /// A list of all unique Methods that <paramref name="cSharpMethod"/>
    /// depends on.
    /// </returns>
    public IEnumerable<CSharpMethod> CalculateDependencies(CSharpMethod cSharpMethod)
    {
        // If the dependencies have already been computed, return them
        if (HasMethod(cSharpMethod) && GetDependencies(cSharpMethod).Any())
        {
            return GetDependencies(cSharpMethod);
        }

        Stack<CSharpMethod> result = new Stack<CSharpMethod>();
        SemanticModel model = PurityAnalyzer.GetSemanticModel(
            Trees,
            cSharpMethod.GetRoot().SyntaxTree
        );

        // If the method doesn't have a known declaration we cannot
        // calculate its dependencies, and so we ignore it
        if (!cSharpMethod.HasKnownDeclaration())
        {
            AddMethod(cSharpMethod);
            SetPurity(cSharpMethod, PurityValue.Unknown);
            return result;
        }

        var methodInvocations = cSharpMethod
            .Declaration
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>();
        if (!methodInvocations.Any()) return result;

        model = PurityAnalyzer.GetSemanticModel(
            Trees,
            cSharpMethod.GetRoot().SyntaxTree
        );

        foreach (var invocation in methodInvocations.Distinct())
        {
            CSharpMethod invoked = new CSharpMethod(invocation, model);

            if (invoked.IsLocalFunction || invoked.IsDelegateFunction)
            {
                // Excludes delegate and local functions
                continue;
            }
            else if (invoked.Equals(cSharpMethod))
            {
                // Handles recursive calls. Don't continue analyzing
                // invoked method if it is equal to the one being analyzed
                continue;
            }
            else result.Push(invoked);
        }

        return result.Distinct();
    }

    /// <summary>
    /// Adds a dependency for a method to the lookup table.
    /// </summary>
    /// <param name="cSharpMethod">The method to add a dependency to</param>
    /// <param name="dependsOnNode">The method that methodNode depends on</param>
    public void AddDependency(CSharpMethod cSharpMethod, CSharpMethod dependsOnNode)
    {
        AddMethod(cSharpMethod);
        AddMethod(dependsOnNode);
        DataRow row = Table
            .AsEnumerable()
            .Single(row => row["identifier"].Equals(cSharpMethod));
        List<CSharpMethod> dependencies = row
            .Field<List<CSharpMethod>>("dependencies");
        if (!dependencies.Contains(dependsOnNode))
        {
            dependencies.Add(dependsOnNode);
        }
    }

    public void RemoveDependency(CSharpMethod cSharpMethodNode, CSharpMethod dependsOnNode)
    {
        if (!HasMethod(cSharpMethodNode))
        {
            throw new Exception(
                $"Method '{cSharpMethodNode}' does not exist in lookup table"
            );
        }
        else if (!HasMethod(dependsOnNode))
        {
            throw new Exception(
                $"Method '{dependsOnNode}' does not exist in lookup table"
            );
        }
        else if (!HasDependency(cSharpMethodNode, dependsOnNode))
        {
            throw new Exception(
                $"Method '{cSharpMethodNode}' does not depend on '{dependsOnNode}'"
            );
        }

        DataRow row = Table
            .AsEnumerable()
            .Single(row => row["identifier"].Equals(cSharpMethodNode));
        row.Field<List<CSharpMethod>>("dependencies").Remove(dependsOnNode);
    }

    public bool HasDependency(CSharpMethod cSharpMethod, CSharpMethod dependsOn)
    {
        return Table
            .AsEnumerable()
            .Any(row =>
                row["identifier"].Equals(cSharpMethod) &&
                row.Field<IEnumerable<CSharpMethod>>("dependencies").Contains(dependsOn)
            );
    }

    /// <summary>
    /// Adds method to the lookup table if it is not already in the lookup
    /// table
    /// </summary>
    /// <param name="cSharpMethodNode">The method to add</param>
    public void AddMethod(CSharpMethod cSharpMethodNode)
    {
        if (!HasMethod(cSharpMethodNode))
        {
            Table.Rows.Add(cSharpMethodNode, new List<CSharpMethod>(), PurityValue.Pure);
        }
    }

    public void RemoveMethod(CSharpMethod cSharpMethodNode)
    {
        if (!HasMethod(cSharpMethodNode))
        {
            throw new Exception(
                $"Method '{cSharpMethodNode}' does not exist in lookup table"
            );
        }
        else
        {
            Table
                .AsEnumerable()
                .Single(row => row["identifier"].Equals(cSharpMethodNode))
                .Delete();
        }
    }

    public bool HasMethod(CSharpMethod cSharpMethodNode)
    {
        return Table
            .AsEnumerable()
            .Any(row => row["identifier"].Equals(cSharpMethodNode));
    }

    public PurityValue GetPurity(CSharpMethod cSharpMethod)
    {
        return (PurityValue) GetMethodRow(cSharpMethod)["purity"];
    }

    /// <summary>
    /// Sets the purity of <paramref name="cSharpMethod"/> to <paramref
    /// name="purityValue"/> if <paramref name="purityValue"/> is less pure than
    /// <paramref name="cSharpMethod"/>'s previous purity.
    /// </summary>
    /// <param name="cSharpMethod">The method</param>
    /// <param name="purityValue">The new purity</param>
    public void SetPurity(CSharpMethod cSharpMethod, PurityValue purityValue)
    {
        if (purityValue < GetPurity(cSharpMethod))
        {
            GetMethodRow(cSharpMethod)["purity"] = purityValue;
        }
    }

    public LookupTable GetMethodsWithKnownPurities()
    {
        DataTable result = Table
            .AsEnumerable()
            .Where(row => (PurityValue) row["purity"] != (PurityValue.Unknown))
            .CopyToDataTable();
        return new LookupTable(result, this);
    }

    public DataRow GetMethodRow(CSharpMethod cSharpMethod)
    {
        return Table
            .AsEnumerable()
            .Where(row => row["identifier"].Equals(cSharpMethod))
            .Single();
    }

    /// <summary>
    /// Gets all methods from a list that are marked `Impure` in the lookup
    /// table.
    /// </summary>
    /// <param name="methods">The list of methods</param>
    /// <returns>
    /// All methods in <paramref name="methods"/> are marked `Impure`
    /// </returns>
    public IEnumerable<CSharpMethod> GetAllImpureMethods(IEnumerable<CSharpMethod> methods)
    {
        return methods.Where(m => GetPurity(m).Equals(PurityValue.Impure));
    }

    /// <summary>
    /// Gets all callers to a given method, i.e. that depend on it.
    /// </summary>
    /// <param name="cSharpMethod">The method</param>
    /// <returns>
    /// All methods that depend on <paramref name="cSharpMethod"/>.
    /// </returns>
    public IEnumerable<CSharpMethod> GetCallers(CSharpMethod cSharpMethod)
    {
        return Table.AsEnumerable().Where(
            r => r.Field<IEnumerable<CSharpMethod>>("dependencies").Contains(cSharpMethod)
        ).Select(r => r.Field<CSharpMethod>("identifier"));
    }

    /// <summary>
    /// Removes all methods in the lookup table that were not declared in
    /// any of the analyzed files.
    /// </summary>
    /// <returns>
    /// A new lookup table stripped of all methods who's declaration is not
    /// in any of the the syntax trees.
    /// </returns>
    public LookupTable StripMethodsNotDeclaredInAnalyzedFiles()
    {
        LookupTable result = Copy();
        List<CSharpMethod> methods = new List<CSharpMethod>();
        foreach (var tree in Trees)
        {
            var methodDeclarations = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>();
            foreach (var methodDeclaration in methodDeclarations)
            {
                methods.Add(new CSharpMethod(methodDeclaration));
            }
        }

        foreach (var row in Table.AsEnumerable())
        {
            var method = row.Field<CSharpMethod>("identifier");
            if (!methods.Contains(method)) result.RemoveMethod(method);
        }

        return result;
    }

    public int CountMethods()
    {
        return Table.Rows.Count;
    }

    /// <summary>
    /// Counts the number of methods in the lookup table with the attribute
    /// [Pure], or without the [Pure] attribute.
    /// </summary>
    /// <param name="havePureAttribute">
    /// Determines if the methods should have the [Pure] attribute or not
    /// </param>
    /// <returns>
    /// The number of methods with the [Pure] attribute, if <paramref
    /// name="havePureAttribute"/> is true, otherwise the number of methods
    /// without the [Pure] attribute.
    /// </returns>
    public int CountMethods(bool havePureAttribute)
    {
        return Table.AsEnumerable().Where(row =>
        {
            CSharpMethod cSharpMethod = row.Field<CSharpMethod>("identifier");
            return cSharpMethod.HasPureAttribute() && havePureAttribute ||
                   !cSharpMethod.HasPureAttribute() && !havePureAttribute;
        }).Count();
    }

    /// <summary>
    /// Counts the number of methods with a given purity level.
    /// </summary>
    /// <param name="purityValue">The purity level</param>
    /// <returns>
    /// The number of methods with the purity level <paramref
    /// name="purityValue"/>.
    /// </returns>
    public int CountMethodsWithPurity(PurityValue purityValue)
    {
        return Table
            .AsEnumerable()
            .Count(row => DataRowExtensions.Field<PurityValue>(row, "purity") == (purityValue));
    }

    /// <summary>
    /// Counts the number of methods with a given purity level and only
    /// those either with, or without the [Pure] attribute.
    /// </summary>
    /// <param name="purityValue">The purity level</param>
    /// <param name="hasPureAttribute">
    /// Determines if the methods should have the [Pure] attribute or not
    /// </param>
    /// <returns>
    /// The number of methods with the purity level <paramref
    /// name="purityValue"/> and the [Pure] attribute if <paramref
    /// name="hasPureAttribute"/> is true, otherwise the number of methods
    /// with the purity level <paramref name="purityValue"/> but with no [Pure]
    /// attribute.
    /// </returns>
    public int CountMethodsWithPurity(PurityValue purityValue, bool hasPureAttribute)
    {
        return GetMethodsWithPurity(purityValue, hasPureAttribute).Count();
    }

    public int CountMethodsWithPurity(PurityValue[] purities, bool hasPureAttribute)
    {
        return GetMethodsWithPurity(purities, hasPureAttribute).Count();
    }

    public int CountFalsePositives()
    {
        return CountMethodsWithPurity(PurityValue.Pure, false);
    }

    public int CountFalseNegatives()
    {
        return CountMethodsWithPurity(
            new PurityValue[] {PurityValue.Impure, PurityValue.ThrowsException},
            true
        );
    }

    public IEnumerable<CSharpMethod> GetMethodsWithPurity(PurityValue purityValue, bool hasPureAttribute)
    {
        return GetMethodsWithPurity(new PurityValue[] {purityValue}, hasPureAttribute);
    }

    public IEnumerable<CSharpMethod> GetMethodsWithPurity(PurityValue[] purities, bool hasPureAttribute)
    {
        return Table.AsEnumerable().Where(row =>
        {
            bool hasPurity = purities.Contains(row.Field<PurityValue>("purity"));
            bool methodHasPureAttribute = row.Field<CSharpMethod>("identifier")
                .HasPureAttribute();

            return hasPurity && (
                methodHasPureAttribute && hasPureAttribute ||
                !methodHasPureAttribute && !hasPureAttribute
            );
        }).Select(r => r.Field<CSharpMethod>("identifier"));
    }

    /// <summary>
    /// Removes all interface methods from the lookup table, i.e. methods
    /// declared in interfaces which therefore lack implementation.
    /// </summary>
    /// <returns>A lookup table stripped of all interface methods.</returns>
    public LookupTable StripInterfaceMethods()
    {
        LookupTable result = Copy();
        List<CSharpMethod> interfaceMethods = result
            .Table
            .AsEnumerable()
            .Where(row => row.Field<CSharpMethod>("identifier").IsInterfaceMethod())
            .Select(row => row.Field<CSharpMethod>("identifier"))
            .ToList();
        foreach (CSharpMethod method in interfaceMethods)
        {
            result.RemoveMethod(method);
        }

        return result;
    }

    /// <summary>
    /// Formats purity ratios into a string.
    /// </summary>
    /// <returns>
    /// Purity ratios formatted into a string.
    /// </returns>
    public string GetPurityRatios()
    {
        int methodsCount = CountMethods();
        double impures = CountMethodsWithPurity(PurityValue.Impure)
                         + CountMethodsWithPurity(PurityValue.ThrowsException);
        double pures = CountMethodsWithPurity(PurityValue.Pure);
        double unknowns = CountMethodsWithPurity(PurityValue.Unknown);

        return $"Impure: {impures}/{methodsCount}, Pure: {pures}/" +
               $"{methodsCount}, Unknown: {unknowns}/{methodsCount}";
    }

    public string GetFalsePositivesAndNegatives()
    {
        int throwExceptionCount = CountMethodsWithPurity(PurityValue.ThrowsException, true);
        int otherImpuresCount = CountMethodsWithPurity(PurityValue.Impure, true);
        var falseNegatives = GetMethodsWithPurity(
            new PurityValue[] {PurityValue.Impure, PurityValue.ThrowsException}, true
        );
        var falsePositives = GetMethodsWithPurity(PurityValue.Pure, false);

        string falseNegativesText = falseNegatives.Any()
            ? $"These methods were classified as impure (false negatives):\n\n" +
              string.Join("\n", falseNegatives.Select(m => "  " + m)) + $"\n\n"
            : "";
        string falsePositivesText = falsePositives.Any()
            ? $"These methods were classified as pure (false positives):\n\n" +
              string.Join("\n", falsePositives.Select(m => "  " + m)) + $"\n\n"
            : "";

        return "\n" + falseNegativesText +
               falsePositivesText +
               $"  Amount: {CountFalsePositives()}";
    }

    public static string FormatListLinewise<T>(IEnumerable<T> items)
    {
        return string.Join("\n", items);
    }

    /// <summary>
    /// Formats purity ratios into a string, including only methods with
    /// the [Pure] attribute.
    /// </summary>
    /// <returns>
    /// Purity ratios formatted into a string, including only methods with
    /// the [Pure] attribute.
    /// </returns>
    public string GetPurityRatiosPureAttributesOnly()
    {
        int methodsCount = CountMethods(true);
        double impures = CountMethodsWithPurity(PurityValue.Impure, true)
                         + CountMethodsWithPurity(PurityValue.ThrowsException, true);
        double pures = CountMethodsWithPurity(PurityValue.Pure, true);
        double unknowns = CountMethodsWithPurity(PurityValue.Unknown, true);
        return $"Impure: {impures}/{methodsCount}, Pure: " +
               $"{pures}/{methodsCount}, Unknown: {unknowns}/{methodsCount}";
    }

    public override string ToString()
    {
        string result = "";
        foreach (var row in Table.AsEnumerable())
        {
            foreach (var item in row.ItemArray)
            {
                if (item is CSharpMethod method)
                {
                    result += method;
                }
                else if (item is IEnumerable<CSharpMethod> methods)
                {
                    List<string> resultList = new List<string>();
                    var dependencies = methods;
                    foreach (var dependency in dependencies)
                    {
                        if (dependency == null) resultList.Add("-");
                        else resultList.Add(dependency.ToString());
                    }

                    result += String.Join(", ", resultList);
                }
                else
                {
                    result += item;
                }

                result += " | ";
            }

            result += "; \n";
        }

        return result;
    }

    public string ToStringNoDependencySet()
    {
        return ToStringNoDependencySet(false);
    }

    /// <summary>
    /// Formats the lookup table as a string
    /// </summary>
    /// <param name="pureAttributeOnly">
    /// Determines if only [Pure]
    /// attributes should be included in the string
    /// </param>
    /// <returns>
    /// The lookup table formatted as a string. If <paramref
    /// name="pureAttributeOnly"/> is true, only methods with the [Pure]
    /// attribute are included in the string, otherwise all methods are
    /// included.
    /// </returns>
    public string ToStringNoDependencySet(bool pureAttributeOnly)
    {
        int printoutWidth = 80;
        string result = FormatTwoColumn("METHOD", "PURITY LEVEL")
                        + new string('-', printoutWidth + 13)
                        + "\n";
        foreach (var row in Table.AsEnumerable())
        {
            CSharpMethod identifierCSharpMethod = row.Field<CSharpMethod>("identifier");
            string identifier = identifierCSharpMethod.ToString();
            string purity = row.Field<PurityValue>("purity").ToString();

            if (!pureAttributeOnly || pureAttributeOnly && identifierCSharpMethod.HasPureAttribute())
            {
                result += FormatTwoColumn(identifier, purity);
            }
        }

        return result;

        string FormatTwoColumn(string identifier, string purity)
        {
            int spaceWidth;
            if (printoutWidth - identifier.Length <= 0) spaceWidth = 0;
            else spaceWidth = printoutWidth - identifier.Length;

            string spaces = new String(' ', spaceWidth);
            return $"{identifier} {spaces}{purity}\n";
        }
    }
}