using System.Collections.Immutable;
using System.Linq.Expressions;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using PurityCodeQualityMetrics.Purity.Violations;

namespace PurityCodeQualityMetrics.Purity;

public class PurityAnalyzer
{
    private readonly List<IViolationPolicy> _violationsPolicies = new List<IViolationPolicy>
    {
        new ThrowsExceptionViolationPolicy(),
        new StaticFieldViolationPolicy(),
    };

    public async Task<IList<PurityReport>> GeneratePurityReports(string project)
    {
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (o, e) => throw new Exception(e.ToString());
        var currentProject = await workspace.OpenProjectAsync(project);

        if (!currentProject.MetadataReferences.Any())
            throw new Exception("References are empty: this usually means that MsBuild didn't load correctly");
        var compilation = await currentProject.GetCompilationAsync();

        if (compilation == null) throw new Exception("Could not compile project");

        var errors = compilation.GetDiagnostics().Where(n => n.Severity == DiagnosticSeverity.Error).ToList();

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
        ISymbol? lambdaSymbol = isLambda ? model.GetSymbolInfo(l).Symbol : null;
        if (methodSymbol == null) throw new Exception("Symbol could not be found in model"); //Should never happen

        //Use lambda if that one is defined
        IMethodSymbol workingSymbol = isLambda ? (IMethodSymbol) lambdaSymbol! : methodSymbol;

        string name;
        if (workingSymbol.MethodKind == MethodKind.LocalFunction)
        {
            var parent = workingSymbol.ContainingSymbol;
            name = parent.Name + ".<local>." + workingSymbol.Name;
        }
        else if (isLambda)
        {
            name = methodSymbol.Name + ".<lambda>." + counter;
        }
        else
        {
            name = methodSymbol.Name;
        }
        
        var report = new PurityReport(
            name,
            workingSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            workingSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            workingSymbol.Parameters.Select(x => x.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
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
            
            return symbol == null
                ? new MethodDependency(c.ToString(), "UNKNOWN", string.Empty, new List<string>(), false, false, false)
                : new MethodDependency(
                    symbol.Name,
                    symbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    symbol.Parameters.Select(x =>
                            x.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
                        .ToList(), false, false, symbol.IsPartialDefinition);
        }).ToList();

        int counter = 0;
        var lambdas = m.DescendantNodes().OfType<LambdaExpressionSyntax>().Select(l =>
        {
            var symbol = model.GetSymbolInfo(l).Symbol as IMethodSymbol;
            if (m == null)
            {
                
            }
            
            var parent = model.GetDeclaredSymbol(l.GetMethodThatBelongsTo()) as IMethodSymbol;
            
            
            return  new MethodDependency(
                parent!.Name + ".<lambda>." + counter++,
                symbol!.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                symbol.Parameters.Select(x =>
                        x.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
                    .ToList(), false, false, symbol.IsPartialDefinition);
        });

        return lambdas.Concat(invocations).ToList();
    }
}