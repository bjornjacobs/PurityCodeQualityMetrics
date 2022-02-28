using System.Data;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PurityCodeQualityMetrics.Purity;

public static class Ext
{
    public static Method GetMethodByName(
        this LookupTable lookupTable,
        string name
    )
    {
        foreach (var tree in lookupTable.trees)
        {
            var methodDeclarations = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text == name);
            if (methodDeclarations.Any())
            {
                return new Method(methodDeclarations.Single());
            }
        }

        return null;
    }
}

public class LookupTable
{
    public DataTable table = new DataTable();
    public WorkingSet workingSet;
    public readonly IEnumerable<SyntaxTree> trees;
    public bool verbose = false;

    public LookupTable()
    {
        table.Columns.Add("identifier", typeof(Method));
        table.Columns.Add("dependencies", typeof(IEnumerable<Method>));
        table.Columns.Add("purity", typeof(PurityValue));
    }
    public LookupTable(SyntaxTree tree) : this(new List<SyntaxTree> { tree }) { }

    public LookupTable(IEnumerable<SyntaxTree> trees) : this(trees, false)
    {
    }

    public LookupTable(IEnumerable<SyntaxTree> trees, bool verbose) : this()
    {
        this.trees = trees;

        BuildLookupTable(verbose);
        workingSet = new WorkingSet(this);
    }
    

    // Creates a LookupTable with the content of `table`
    public LookupTable(DataTable table, LookupTable lt)
    {
        this.trees = lt.trees;
        this.table = table.Copy();
    }

    public LookupTable Copy()
    {
        return new LookupTable(table, this);
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
        foreach (var tree in trees)
        {
            var methodDeclarations = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>();
            foreach (var methodDeclaration in methodDeclarations)
            {
                Method method = new Method(methodDeclaration);

                // Ignore interface methods which also show up as
                // MethodDeclarationSyntaxes
                if (!method.IsInterfaceMethod())
                {
                    if (verbose)
                    {
                        Console.WriteLine($"Calculating dependencies for {method}.");
                    }

                    var dependencies = CalculateDependencies(method);
                    AddMethod(method);
                    foreach (var dependency in dependencies)
                    {
                        AddDependency(method, dependency);
                    }
                }
            }
        }
    }

    // This method is private since dependencies get removed after
    // calculating purities. See method CalculateDependencies().
    private IEnumerable<Method> GetDependencies(Method method)
    {
        return GetMethodRow(method).Field<IEnumerable<Method>>("dependencies");
    }

    /// <summary>
    /// Computes a list of all unique methods that a method depends on. If
    /// any method doesn't have a known declaration, its purity level is
    /// set to `Unknown`. If an interface method invocation was found, the
    /// invoker's purity is set to `Unknown` since the invoked method could
    /// have any implementation.
    /// </summary>
    /// <param name="method">The method</param>
    /// <returns>
    /// A list of all unique Methods that <paramref name="method"/>
    /// depends on.
    /// </returns>
    public IEnumerable<Method> CalculateDependencies(Method method)
    {
        // If the dependencies have already been computed, return them
        if (HasMethod(method) && GetDependencies(method).Any())
        {
            return GetDependencies(method);
        }

        Stack<Method> result = new Stack<Method>();
        SemanticModel model = PurityAnalyzer.GetSemanticModel(
            trees,
            method.GetRoot().SyntaxTree
        );

        // If the method doesn't have a known declaration we cannot
        // calculate its dependencies, and so we ignore it
        if (!method.HasKnownDeclaration())
        {
            AddMethod(method);
            SetPurity(method, PurityValue.Unknown);
            return result;
        }

        var methodInvocations = method
            .Declaration
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>();
        if (!methodInvocations.Any()) return result;

        model = PurityAnalyzer.GetSemanticModel(
            trees,
            method.GetRoot().SyntaxTree
        );

        foreach (var invocation in methodInvocations.Distinct())
        {
            Method invoked = new Method(invocation, model);

            if (invoked.IsLocalFunction || invoked.IsDelegateFunction)
            {
                // Excludes delegate and local functions
                continue;
            }
            else if (invoked.Equals(method))
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
    /// <param name="method">The method to add a dependency to</param>
    /// <param name="dependsOnNode">The method that methodNode depends on</param>
    public void AddDependency(Method method, Method dependsOnNode)
    {
        AddMethod(method);
        AddMethod(dependsOnNode);
        DataRow row = table
            .AsEnumerable()
            .Where(row => row["identifier"].Equals(method))
            .Single();
        List<Method> dependencies = row
            .Field<List<Method>>("dependencies");
        if (!dependencies.Contains(dependsOnNode))
        {
            dependencies.Add(dependsOnNode);
        }
    }

    public void RemoveDependency(Method methodNode, Method dependsOnNode)
    {
        if (!HasMethod(methodNode))
        {
            throw new Exception(
                $"Method '{methodNode}' does not exist in lookup table"
            );
        }
        else if (!HasMethod(dependsOnNode))
        {
            throw new Exception(
                $"Method '{dependsOnNode}' does not exist in lookup table"
            );
        }
        else if (!HasDependency(methodNode, dependsOnNode))
        {
            throw new Exception(
                $"Method '{methodNode}' does not depend on '{dependsOnNode}'"
            );
        }

        DataRow row = table
            .AsEnumerable()
            .Where(row => row["identifier"].Equals(methodNode))
            .Single();
        row.Field<List<Method>>("dependencies").Remove(dependsOnNode);
    }

    public bool HasDependency(Method method, Method dependsOn)
    {
        return table
            .AsEnumerable()
            .Any(row =>
                row["identifier"].Equals(method) &&
                row.Field<IEnumerable<Method>>("dependencies").Contains(dependsOn)
            );
    }

    /// <summary>
    /// Adds method to the lookup table if it is not already in the lookup
    /// table
    /// </summary>
    /// <param name="methodNode">The method to add</param>
    public void AddMethod(Method methodNode)
    {
        if (!HasMethod(methodNode))
        {
            table.Rows.Add(methodNode, new List<Method>(), PurityValue.Pure);
        }
    }

    public void RemoveMethod(Method methodNode)
    {
        if (!HasMethod(methodNode))
        {
            throw new Exception(
                $"Method '{methodNode}' does not exist in lookup table"
            );
        }
        else
        {
            table
                .AsEnumerable()
                .Where(row => row["identifier"].Equals(methodNode))
                .Single()
                .Delete();
        }
    }

    public bool HasMethod(Method methodNode)
    {
        return table
            .AsEnumerable()
            .Any(row => row["identifier"].Equals(methodNode));
    }

    public PurityValue GetPurity(Method method)
    {
        return (PurityValue) GetMethodRow(method)["purity"];
    }

    /// <summary>
    /// Sets the purity of <paramref name="method"/> to <paramref
    /// name="purityValue"/> if <paramref name="purityValue"/> is less pure than
    /// <paramref name="method"/>'s previous purity.
    /// </summary>
    /// <param name="method">The method</param>
    /// <param name="purityValue">The new purity</param>
    public void SetPurity(Method method, PurityValue purityValue)
    {
        if (purityValue < GetPurity(method))
        {
            GetMethodRow(method)["purity"] = purityValue;
        }
    }

    public LookupTable GetMethodsWithKnownPurities()
    {
        DataTable result = table
            .AsEnumerable()
            .Where(row => (PurityValue) row["purity"] != (PurityValue.Unknown))
            .CopyToDataTable();
        return new LookupTable(result, this);
    }

    public DataRow GetMethodRow(Method method)
    {
        return table
            .AsEnumerable()
            .Where(row => row["identifier"].Equals(method))
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
    public IEnumerable<Method> GetAllImpureMethods(IEnumerable<Method> methods)
    {
        return methods.Where(m => GetPurity(m).Equals(PurityValue.Impure));
    }

    /// <summary>
    /// Gets all callers to a given method, i.e. that depend on it.
    /// </summary>
    /// <param name="method">The method</param>
    /// <returns>
    /// All methods that depend on <paramref name="method"/>.
    /// </returns>
    public IEnumerable<Method> GetCallers(Method method)
    {
        return table.AsEnumerable().Where(
            r => r.Field<IEnumerable<Method>>("dependencies").Contains(method)
        ).Select(r => r.Field<Method>("identifier"));
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
        List<Method> methods = new List<Method>();
        foreach (var tree in trees)
        {
            var methodDeclarations = tree
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>();
            foreach (var methodDeclaration in methodDeclarations)
            {
                methods.Add(new Method(methodDeclaration));
            }
        }

        foreach (var row in table.AsEnumerable())
        {
            var method = row.Field<Method>("identifier");
            if (!methods.Contains(method)) result.RemoveMethod(method);
        }

        return result;
    }

    public int CountMethods()
    {
        return table.Rows.Count;
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
        return table.AsEnumerable().Where(row =>
        {
            Method method = row.Field<Method>("identifier");
            return method.HasPureAttribute() && havePureAttribute ||
                   !method.HasPureAttribute() && !havePureAttribute;
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
        return table
            .AsEnumerable()
            .Where(row => row.Field<PurityValue>("purity") == (purityValue))
            .Count();
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

    public IEnumerable<Method> GetMethodsWithPurity(PurityValue purityValue, bool hasPureAttribute)
    {
        return GetMethodsWithPurity(new PurityValue[] {purityValue}, hasPureAttribute);
    }

    public IEnumerable<Method> GetMethodsWithPurity(PurityValue[] purities, bool hasPureAttribute)
    {
        return table.AsEnumerable().Where(row =>
        {
            bool hasPurity = purities.Contains(row.Field<PurityValue>("purity"));
            bool methodHasPureAttribute = row.Field<Method>("identifier")
                .HasPureAttribute();

            return hasPurity && (
                methodHasPureAttribute && hasPureAttribute ||
                !methodHasPureAttribute && !hasPureAttribute
            );
        }).Select(r => r.Field<Method>("identifier"));
    }

    /// <summary>
    /// Removes all interface methods from the lookup table, i.e. methods
    /// declared in interfaces which therefore lack implementation.
    /// </summary>
    /// <returns>A lookup table stripped of all interface methods.</returns>
    public LookupTable StripInterfaceMethods()
    {
        LookupTable result = Copy();
        List<Method> interfaceMethods = result
            .table
            .AsEnumerable()
            .Where(row => row.Field<Method>("identifier").IsInterfaceMethod())
            .Select(row => row.Field<Method>("identifier"))
            .ToList();
        foreach (Method method in interfaceMethods)
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
        foreach (var row in table.AsEnumerable())
        {
            foreach (var item in row.ItemArray)
            {
                if (item is Method method)
                {
                    result += method;
                }
                else if (item is IEnumerable<Method> methods)
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
        foreach (var row in table.AsEnumerable())
        {
            Method identifierMethod = row.Field<Method>("identifier");
            string identifier = identifierMethod.ToString();
            string purity = row.Field<PurityValue>("purity").ToString();

            if (!pureAttributeOnly || pureAttributeOnly && identifierMethod.HasPureAttribute())
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