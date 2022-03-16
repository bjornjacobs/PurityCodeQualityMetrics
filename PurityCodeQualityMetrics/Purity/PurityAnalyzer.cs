using System.Collections.Immutable;
using System.Linq.Expressions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics.Purity.Violations;

namespace PurityCodeQualityMetrics.Purity;

public class PurityAnalyzer
{
    private ILogger _logger;
    
    private readonly List<IViolationPolicy> _violationsPolicies = new List<IViolationPolicy>
    {
        new ThrowsExceptionViolationPolicy(),
        new StaticFieldViolationPolicy(),
        new LocalPurityPolicy(),
        new ParameterViolationPolicy()
    };

    public PurityAnalyzer(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<IList<PurityReport>> GeneratePurityReports(string project)
    {
        _logger.LogInformation("Starting compilation");
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (o, e) => throw new Exception(e.ToString());
        var currentProject = await workspace.OpenProjectAsync(project);

        if (!currentProject.MetadataReferences.Any())
            throw new Exception("References are empty: this usually means that MsBuild didn't load correctly");
        
        _logger.LogInformation($"Loaded project: {project}");
        var compilation = await currentProject.GetCompilationAsync();

        if (compilation == null) throw new Exception("Could not compile project");
        var errors = compilation.GetDiagnostics().Where(n => n.Severity == DiagnosticSeverity.Error).ToList();
        _logger.LogInformation($"Project compiled with {errors.Count} errors");
        errors.ForEach(x => _logger.LogDebug(x.Location + " " + x.GetMessage()));
        
        return ExtractMethodReports(compilation);
    }

    private IList<PurityReport> ExtractMethodReports(Compilation compilation)
    {
        //Each syntax tree represents a file
        return compilation.SyntaxTrees.SelectMany(tree =>
        {
            var model = compilation.GetSemanticModel(tree);
            List<CSharpSyntaxNode> methodDefinitions = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Cast<CSharpSyntaxNode>()
                .ToList();
            
            var localFunctions = tree.GetRoot()
                .DescendantNodes()
                .OfType<LocalFunctionStatementSyntax>()
                .Cast<CSharpSyntaxNode>()
                .ToList();

            var allFunctionAndMethods = methodDefinitions.Concat(localFunctions).ToList();

            var lambdaReports = allFunctionAndMethods
                .SelectMany(x =>
                {
                    int counter = 0; //Used for giving lambda's unique names
                    return x.DescendantNodes().OfType<LambdaExpressionSyntax>()
                        .Select(l => ExtractReportFromDeclaration(x, l, counter++, model));
                }).ToList();

            return allFunctionAndMethods
                .Select(m => ExtractReportFromDeclaration(m, null, 0, model))
                .Concat(lambdaReports);
        }).ToList();
    }


    private PurityReport ExtractReportFromDeclaration(SyntaxNode m, LambdaExpressionSyntax? l, int counter,
        SemanticModel model)
    {
        //Determine if this method is called in the context of a lambda or normal method
        var isLambda = l != null; 
        
        //Get symbols from the model. Some casting is needed because both local functions and methods definitions need to work 
        IMethodSymbol methodSymbol = (IMethodSymbol) model.GetDeclaredSymbol(m)!;
        ISymbol? lambdaSymbol = isLambda ? model.GetSymbolInfo(l!).Symbol : null;
        if (methodSymbol == null) throw new Exception("Symbol could not be found in model"); //Should never happen

        //Use lambda if that one is defined
        IMethodSymbol workingSymbol = isLambda ? (IMethodSymbol) lambdaSymbol! : methodSymbol;

        string name;
        if (workingSymbol.MethodKind == MethodKind.LocalFunction)
        {
            var parent = workingSymbol.ContainingSymbol;
            name = parent.GetNameWithClass() + ".<local>." + workingSymbol.Name;
        }
        else if (isLambda)
        {
            name =methodSymbol.GetNameWithClass() + ".<lambda>." + counter;
        }
        else
        {
            name = methodSymbol.GetNameWithClass();
        }
        
        var report = new PurityReport(
            name,
            workingSymbol.ContainingNamespace.ToUniqueString(),
            workingSymbol.ReturnType.ToUniqueString(),
            workingSymbol.Parameters.Select(x => x.Type.ToUniqueString())
                .ToList());

        report.IsLambda = isLambda;
        report.Violations.AddRange(ExtractPurityViolations(isLambda ? l! : m, model));
        report.Dependencies.AddRange(ExtractMethodDependencies(isLambda ? l! : m, model));
        return report;
    }

    private List<PurityViolation> ExtractPurityViolations(SyntaxNode m, SemanticModel model)
    {
        return _violationsPolicies
            .SelectMany(policy => policy.Check(m, model.SyntaxTree, model))
            .ToList();
    }

    private List<MethodDependency> ExtractMethodDependencies(SyntaxNode m, SemanticModel model)
    {
        var invocations = m.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(c =>
        {
            var symbol = model.GetSymbolInfo(c).Symbol as IMethodSymbol;
            if (symbol == null)
            {
                _logger.LogWarning($"Could not find symbol for {c.ToString()}");
                return new MethodDependency(c.ToString(), "UNKNOWN", string.Empty, new List<string>(), false, false, false);
            }

            
            string name;
            if (symbol.MethodKind == MethodKind.LocalFunction)
            {
                var parent = symbol.ContainingSymbol;
                name = parent.GetNameWithClass() + ".<local>." + symbol.Name;
            }
            else
            {
                name = symbol.GetNameWithClass();
            }
            
            return new MethodDependency(
                    name,
                    symbol.ContainingNamespace.ToUniqueString(),
                    symbol.ReturnType.ToUniqueString(),
                    symbol.Parameters.Select(x =>
                            x.Type.ToUniqueString())
                        .ToList(), false, false, symbol.IsPartialDefinition);
        }).ToList();

        int counter = 0;
        var lambdas = m.DescendantNodes().OfType<LambdaExpressionSyntax>().Select(l =>
        {
            var symbol = model.GetSymbolInfo(l).Symbol as IMethodSymbol;
            var parent = model.GetDeclaredSymbol(l.GetMethodThatBelongsTo()!) as IMethodSymbol;
            

            return  new MethodDependency(
                parent!.GetNameWithClass() + ".<lambda>." + counter++,
                symbol!.ContainingNamespace.ToUniqueString(),
                symbol.ReturnType.ToUniqueString(),
                symbol.Parameters.Select(x =>
                        x.Type.ToUniqueString())
                    .ToList(), false, false, symbol.IsPartialDefinition);
        });

        return lambdas.Concat(invocations).ToList();
    }
}