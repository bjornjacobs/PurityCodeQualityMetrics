﻿using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics.Purity.Util;
using PurityCodeQualityMetrics.Purity.Violations;

namespace PurityCodeQualityMetrics.Purity;

public class PurityAnalyser
{
    private ILogger _logger;

    private readonly List<IViolationPolicy> _violationsPolicies = new List<IViolationPolicy>
    {
        new ThrowsExceptionViolationPolicy(),
        new StaticFieldViolationPolicy(),
        new LocalPurityPolicy(),
        new ParameterViolationPolicy()
    };

    public PurityAnalyser(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<List<PurityReport>> GeneratePurityReports(string solution)
    {
        _logger.LogInformation("Starting compilation");
        if(!MSBuildLocator.IsRegistered)
            MSBuildLocator.RegisterDefaults();
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (o, e) => throw new Exception(e.ToString());
        var currentProject = await workspace.OpenSolutionAsync(solution);

        if (!currentProject.Projects.First().MetadataReferences.Any())
            throw new Exception("References are empty: this usually means that MsBuild didn't load correctly");

        _logger.LogInformation($"Loaded project: {solution}");
        return currentProject.Projects.SelectMany( project =>
        {
            var compilation = project.GetCompilationAsync().Result;

            if (compilation == null) throw new Exception("Could not compile project");
            var errors = compilation.GetDiagnostics().Where(n => n.Severity == DiagnosticSeverity.Error).ToList();
            _logger.LogInformation($"Project compiled with {errors.Count} errors");
            errors.ForEach(x => _logger.LogDebug("[COMPILER_ERROR] " + x.Location + " " + x.GetMessage()));

            return ExtractMethodReports(compilation, currentProject);
        }).ToList();

    }

    private List<PurityReport> ExtractMethodReports(Compilation compilation, Solution solution)
    {
        //Each syntax tree represents a file
        return compilation.SyntaxTrees.SelectMany(tree =>
            tree.GetAllMethods().Select(m => ExtractReportFromDeclaration(m, compilation.GetSemanticModel(tree), solution))
        ).ToList();
    }


    private PurityReport ExtractReportFromDeclaration(SyntaxNode m, SemanticModel model, Solution solution)
    {
        var workingSymbol = m.GetMethodSymbol(model);

        var report = new PurityReport(
            workingSymbol.GetUniqueMethodName(m),
            workingSymbol.ContainingNamespace.ToUniqueString(),
            workingSymbol.ReturnType.ToUniqueString(),
            workingSymbol.Parameters.Select(x => x.Type.ToUniqueString())
                .ToList());

        var freshResult = m.IsReturnFresh(model, solution);
        report.ReturnValueIsFresh = freshResult.IsFresh;
        report.MethodType = workingSymbol.MethodKind.ToMethodType();
        report.Violations.AddRange(ExtractPurityViolations(m, model));
        report.Dependencies.AddRange(ExtractMethodDependencies(m, model, solution));
        
        
        report.Dependencies.Where(x => freshResult.Dependencies.Any(y => x.FullName == y.FullName)).ToList()
            .ForEach(x => x.FreshDependsOnMethodReturnIsFresh = true);
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

        return lambdas.Concat(invocations).Select( c =>
        {
            var symbol = model.GetSymbolInfo(c).Symbol as IMethodSymbol;
            if (symbol == null)
            {
                _logger.LogWarning($"Could not find symbol for {c.ToString()}");
                return new MethodDependency(c.ToString(), Scoping.Field);
            }


            if (symbol.IsAbstract)
            {
                var implementations = SymbolFinder.FindImplementationsAsync(symbol, solution).Result.ToList();
            }
            

            return new MethodDependency(symbol.GetUniqueMethodName(c),
                symbol.ContainingNamespace.ToUniqueString(),
                symbol.ReturnType.ToUniqueString(),
                symbol.Parameters.Select(x => x.Type.ToUniqueString()).ToList(),
                symbol.MethodKind.ToMethodType(),
                symbol.IsAbstract,
                Scoping.Field,
                false);
        }).ToList();
    }
}