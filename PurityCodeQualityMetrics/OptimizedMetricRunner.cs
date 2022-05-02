using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using PurityCodeQualityMetrics.CodeMetrics;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Purity.Util;
using RestSharp;

namespace PurityCodeQualityMetrics;

public class OptimizedMetricRunner
{
    private PurityAnalyser _purityAnalyser;
    private PurityCalculator _purityCalculator;

    public OptimizedMetricRunner(PurityAnalyser purityAnalyser, PurityCalculator purityCalculator)
    {
        _purityAnalyser = purityAnalyser;
        _purityCalculator = purityCalculator;
    }

    public async Task<List<MethodWithMetrics>> Run(string solutionPath, List<LinesChange> changes)
    {
        var solution = await GetSolution(solutionPath);
        var projects = solution.NonTestProjects();
        var compilations = await Task.WhenAll(projects.Select(async x => await x.GetCompilationAsync()));

        var methods = compilations.SelectMany(com => com.SyntaxTrees.SelectMany(tree =>
        {
            var change = changes.Where(c => tree.FilePath.EndsWith(c.Path, StringComparison.CurrentCultureIgnoreCase));
            var methods = change.SelectMany(tree.RelevantMethods);
            return methods.Select(x => (Method: x, Model: com.GetSemanticModel(tree), Compilation: com));
        })).ToList();
        
        var metrics = methods.Select(x => CalculateMetricsForMethod(x.Method, x.Model, solution, (y) => x.Compilation.GetSemanticModel(y))).ToList();

        return metrics;
    }

    private MethodWithMetrics CalculateMetricsForMethod(SyntaxNode method, SemanticModel model, Solution solution, Func<SyntaxTree, SemanticModel> getModel)
    {
        var classNode = method.GetClass();
        var methodsSymbol = method.GetMethodSymbol(model)!;

        var report = _purityAnalyser.ExtractReportsFromMethodAndDependencies(method, model, solution, getModel);
        var scores = _purityCalculator.CalculateScores(report, (dependency, purityReport) => null);

        
        var result = new MethodWithMetrics(methodsSymbol!.Name, method.SyntaxTree.FilePath);
        var linesOfSourceCode = SourceLinesOfCode.GetCount(method);
        result.PurityScore = scores.FirstOrDefault(x => x.Report.Name == methodsSymbol.GetUniqueMethodName(method));

        if (classNode != null)
        {
            
            result.Metrics[Measure.DepthOfInheritanceTree] = DepthOfInheritanceTree.GetCount(classNode, model);
            result.Metrics[Measure.LackOfCohesionOfMethods] = LackOfCohesionOfMethods.GetCount(classNode, model);
            result.Metrics[Measure.WeightedMethodsPerClass] = WeightedMethodsPerClass.GetCount(classNode);
            result.Metrics[Measure.ResponseForAClass] = ResponseForAClass.GetCount(classNode);
        }
        
        result.Metrics[Measure.SourceLinesOfCode] = linesOfSourceCode;
        //result.Metrics[Measure.CouplingBetweenObjects] = CouplingBetweenObjects.GetCount(classNode, model, )
        result.Metrics[Measure.CommentDensity] = CommentDensity.GetCount(method, linesOfSourceCode);
        var lambda = LambdaMetrics.GetValueList(method, model);
        var linesOfLambda = SourceLinesOfLambda.GetCount(method);
        result.Metrics[Measure.LambdaCount] = lambda.LambdaCount;
        result.Metrics[Measure.SourceLinesOfLambda] = linesOfLambda;
        result.Metrics[Measure.LambdaScore] = (int) ((double) linesOfLambda / linesOfSourceCode * 100);
        result.Metrics[Measure.LambdaFieldVariableUsageCount] = lambda.FieldVariableUsageCount;
        result.Metrics[Measure.LambdaLocalVariableUsageCount] = lambda.LocalVariableUsageCount;
        result.Metrics[Measure.LambdaSideEffectCount] = lambda.SideEffects;

        //result.Metrics[Measure.NumberOfChildren] = NumberOfChildren.GetCount(classNode, model,);
        result.Metrics[Measure.UnterminatedCollections] = UnterminatedCollections.GetCount(method, model);
        result.Metrics[Measure.CyclomaticComplexity] = CyclomaticComplexity.GetCount(method);

        return result;
    }

    private static async Task<Solution> GetSolution(string solutionPath)
    {
        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();
        
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);
        var solution = await workspace.OpenSolutionAsync(solutionPath);
        return solution;
    }
}

public record MethodWithMetrics(string MethodName, string FilePath)
{
    public List<ViolationWithDistance> Violations => PurityScore?.Violations;
    public int DependencyCount => PurityScore?.DependencyCount ?? 0;
    public int TotalLinesOfSourceCode => PurityScore?.LinesOfSourceCode ?? 0;

    [JsonIgnore]
    public PurityScore PurityScore { get; set; }
    public readonly IDictionary<Measure, double> Metrics = new Dictionary<Measure, double>();
}

public static class SolutionHelper
{
    public static List<Project> NonTestProjects(this Solution solution)
    {
        return solution.Projects.Where(x => !x.Name.Contains("test", StringComparison.CurrentCultureIgnoreCase)).ToList();
    }

    public static List<SyntaxNode> RelevantMethods(this SyntaxTree tree, LinesChange change)
    {
        var methods = tree.GetAllMethods();
        return methods.Where(x =>
        {
            var start = x.GetLocation().GetLineSpan().StartLinePosition.Line;
            var end = x.GetLocation().GetLineSpan().EndLinePosition.Line;

            return start <= change.End && end >= change.Start;
        }).ToList();
    }

    public static ClassDeclarationSyntax GetClass(this SyntaxNode node)
    {
        if (node.Parent == null) return null;
        if (node.Parent is ClassDeclarationSyntax clas) return clas;
        return node.Parent.GetClass();
    }
}