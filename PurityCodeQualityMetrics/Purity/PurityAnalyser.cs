using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics.CodeMetrics;
using PurityCodeQualityMetrics.Purity.Util;
using PurityCodeQualityMetrics.Purity.Violations;

namespace PurityCodeQualityMetrics.Purity;

public class PurityAnalyser
{
    private readonly ILogger _logger;

    private readonly List<IViolationPolicy> _violationsPolicies = new List<IViolationPolicy>
    {
        new ThrowsExceptionViolationPolicy(),
        new IdentifierViolationPolicy()
    };

    public PurityAnalyser(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<List<PurityReport>> GeneratePurityReports(string solution)
    {
        return await GeneratePurityReports(solution, new List<string>());
    }

    public async Task<List<PurityReport>> GeneratePurityReports(string solution, List<string> files,
        bool ignoreTestProject = true)
    {
        _logger.LogInformation("Starting compilation");
        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (o, e) => _logger.LogWarning("Workspace error: {}", e.Diagnostic.Message);
        var currentSolution = await workspace.OpenSolutionAsync(solution);

        if (currentSolution.Projects.All(x => !x.MetadataReferences.Any()))
            _logger.LogError("References are empty: this usually means that MsBuild didn't load correctly");

        _logger.LogInformation($"Loaded project: {solution}");
        return currentSolution.Projects
            .Where(x => !ignoreTestProject || !x.Name.Contains("test", StringComparison.CurrentCultureIgnoreCase))
            .SelectMany(x => AnalyseProject(x, currentSolution, files)).AsParallel().ToList();
    }

    public async Task<List<PurityReport>> GeneratePurityReportsProject(string projectFile)
    {
        _logger.LogInformation("Starting compilation");
        if (!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (o, e) => _logger.LogWarning("Workspace error: {}", e.Diagnostic.Message);
        var project = await workspace.OpenProjectAsync(projectFile);

        if (!project.MetadataReferences.Any())
            _logger.LogInformation("References are empty: this usually means that MsBuild didn't load correctly");

        _logger.LogInformation($"Loaded project: {project.Name}");
        return AnalyseProject(project, project.Solution, new List<string>());
    }

    public List<PurityReport> AnalyseProject(Project project, Solution solution, List<string> files)
    {
        var compilation = project.GetCompilationAsync().Result;

        if (compilation == null) throw new Exception("Could not compile project");
        var errors = compilation.GetDiagnostics().Where(n => n.Severity == DiagnosticSeverity.Error).ToList();
        _logger.LogInformation($"Project {project.Name}: compiled with {errors.Count} errors");
        errors.ForEach(x => _logger.LogDebug("[COMPILER_ERROR] " + x.Location + " " + x.GetMessage()));

        return ExtractMethodReports(compilation, solution, files);
    }


    private List<PurityReport> ExtractMethodReports(Compilation compilation, Solution solution, List<string> files)
    {
        //Each syntax tree represents a file. Filter on files if there are any

        return compilation.SyntaxTrees.Where(x =>
                !files.Any() || files.Any(y => x.FilePath.Contains(y, StringComparison.CurrentCultureIgnoreCase)))
            .SelectMany(tree =>
                tree.GetAllMethods()
                    .Select(m => ExtractReportFromDeclaration(m, compilation.GetSemanticModel(tree), solution))
            ).ToList();
    }

    public (PurityReport Main, List<PurityReport> Graph) ExtractReportsFromMethodAndDependencies(SyntaxNode method,
        SemanticModel model,
        Solution solution, Func<SyntaxTree, SemanticModel> getModel)
    {
        var queue = new QueueOnlyOnce<SyntaxNode>();
        var lst = new List<PurityReport>();
        PurityReport? mainreport = null;
        
        queue.Enqueue(method);
        
        while (queue.HasItems)
        {
            var item = queue.Dequeue();
            
            var result = ExtractReportFromDeclaration(item, model, solution);
            if (mainreport == null)
                mainreport = result;
            
            lst.Add(result);
            
            //Recursive for dependencies
            var deps = item.DescendantNodesInThisFunction()
                .OfType<InvocationExpressionSyntax>()
                .Select(x =>
                {
                    try
                    {
                        var model = getModel(x.SyntaxTree);
                        var s = model.GetSymbolInfo(x);
                        if (s.Symbol == null) return null;

                        var locations = s.Symbol.Locations.FirstOrDefault();
                        if (locations == null || !locations.IsInSource) return null;

                        var dec = locations.SourceTree
                            .GetRoot()
                            .DescendantNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .Where(x => x.Identifier.Text == s.Symbol.Name)
                            .Select(x => (model.GetDeclaredSymbol(x), x))
                            .Single(x => x.Item1 != null && SymbolEqualityComparer.Default.Equals(x.Item1, s.Symbol));
                        return dec.x;
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
                })
                .Where(x => x != null)
                .ToList();
            deps.ForEach(x => queue.Enqueue(x));
        }
        
        return (mainreport!, lst);
    }

    public PurityReport ExtractReportFromDeclaration(SyntaxNode method, SemanticModel model, Solution solution)
    {
        var workingSymbol = method.GetMethodSymbol(model);

        var report = new PurityReport(
            workingSymbol.GetUniqueMethodName(method),
            workingSymbol.ContainingNamespace.ToUniqueString(),
            workingSymbol.ReturnType.ToUniqueString(),
            workingSymbol.Parameters.Select(x => x.Type.ToUniqueString())
                .ToList());

        report.FilePath = method.SyntaxTree.FilePath;
        report.LineStart = method.GetLocation().GetLineSpan().Span.Start.Line + 1;
        report.LineEnd = method.GetLocation().GetLineSpan().Span.End.Line + 1;

        report.SourceLinesOfCode = SourceLinesOfCode.GetCount(method);


        report.MethodType = workingSymbol.MethodKind.ToMethodType();
        report.Violations.AddRange(ExtractPurityViolations(method, model));
        report.Dependencies.AddRange(ExtractMethodDependencies(method, model, solution));

        var freshResult = method.IsReturnFresh(model);
        report.ReturnValueIsFresh = freshResult.IsFresh;
        report.Dependencies.Where(x => freshResult.Dependencies.Any(y => x.FullName == y)).ToList()
            .ForEach(x => x.FreshDependsOnMethodReturnIsFresh = true);

        report.Dependencies.Where(x => freshResult.ShouldBeFresh.Any(y => x.FullName == y)).ToList()
            .ForEach(x => x.ReturnShouldBeFresh = true);

        return report;
    }

    private List<PurityViolation> ExtractPurityViolations(SyntaxNode m, SemanticModel model)
    {
        return _violationsPolicies
            .SelectMany(policy => policy.Check(m, model.SyntaxTree, model))
            .ToList();
    }

    private List<MethodDependency> ExtractMethodDependencies(SyntaxNode m, SemanticModel model, Solution solution)
    {
        var invocations = m.DescendantNodes().OfType<InvocationExpressionSyntax>().Cast<SyntaxNode>();
        var lambdas = m.DescendantNodes().OfType<LambdaExpressionSyntax>().Cast<SyntaxNode>();

        return lambdas.Concat(invocations).Select(c =>
        {
            if (c.IsLogging()) return null;

            var symbol = model.GetSymbolInfo(c).Symbol as IMethodSymbol;

            if (symbol == null)
            {
                if (!c.ToString().Contains("nameof", StringComparison.CurrentCultureIgnoreCase))
                {
                    _logger.LogDebug($"Could not find symbol for {c.ToString()}");
                }

                return new MethodDependency(c.ToString());
            }

            return new MethodDependency(symbol.GetUniqueMethodName(c),
                symbol.ContainingNamespace.ToUniqueString(),
                symbol.ReturnType.ToUniqueString(),
                symbol.Parameters.Select(x => x.Type.ToUniqueString()).ToList(),
                symbol.MethodKind.ToMethodType(),
                symbol.IsAbstract,
                false);
        }).Where(x => x != null).ToList();
    }
}