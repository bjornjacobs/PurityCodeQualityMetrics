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

public class PurityTool
{
    private readonly ILogger _logger;

    private readonly PurityAnalyser _purityAnalyser;

    public PurityTool(ILogger logger)
    {
        _logger = logger;
        _purityAnalyser = new PurityAnalyser(logger);
    }

    public async Task<List<PurityReport>> GeneratePurityReports(string solution)
    {
        return await GeneratePurityReports(solution, new List<string>());
    }

    public List<PurityReport> AnalyseProject(Project project, Solution solution, List<string> files)
    {
        return _purityAnalyser.AnalyseProject(project, solution, files);
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
            .SelectMany(x => _purityAnalyser.AnalyseProject(x, currentSolution, files)).AsParallel().ToList();
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
        return _purityAnalyser.AnalyseProject(project, project.Solution, new List<string>());
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

            var result = _purityAnalyser.ExtractReportFromDeclaration(item, model, solution);
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
}