using System.Collections.Immutable;
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
        new ReadsStaticFieldViolationPolicy(),
        new ModifiesGlobalStateViolationPolicy(),
    };

    public async Task<IList<PurityReport>> GeneratePurityReports(string project)
    {
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (o, e) =>
        {
            
        };
        var currentProject = await workspace.OpenProjectAsync(project);

        if (!currentProject.MetadataReferences.Any()) throw new Exception("References are empty");
        var compilation = await currentProject.GetCompilationAsync();

        if (compilation == null) throw new Exception("Could not compile project");

        var errors = compilation.GetDiagnostics().Where(n => n.Severity == DiagnosticSeverity.Error).ToList();

        return ExtractMethodReports(compilation);
    }

    private IList<PurityReport> ExtractMethodReports(Compilation compilation)
    {
        return compilation.SyntaxTrees.SelectMany(tree =>
        {
            var model = compilation.GetSemanticModel(tree);
            var methodDeclarations = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            return methodDeclarations.Select(m => ExtractReportFromDeclaration(m, tree, model));
        }).ToList();
    }

    private PurityReport ExtractReportFromDeclaration(MethodDeclarationSyntax m, SyntaxTree tree,
        SemanticModel model)
    {
        var methodSymbol = model.GetDeclaredSymbol(m);
        if (methodSymbol == null) throw new Exception("Symbol could not be found in model"); //Should never happen

        var report = new PurityReport(
            methodSymbol.Name,
            methodSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            methodSymbol.TypeParameters.Select(x => x.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)).ToImmutableList());

        report.Violations.AddRange(ExtractPurityViolations(m, model));
        report.Dependencies.AddRange(ExtractMethodDependencies(m, model));
        return report;
    }

    private List<PurityViolation> ExtractPurityViolations(MethodDeclarationSyntax m, SemanticModel model)
    {
        return _violationsPolicies
            .SelectMany(policy => policy.Check(m, model.SyntaxTree, model))
            .ToList();
    }

    private List<MethodDependency> ExtractMethodDependencies(MethodDeclarationSyntax m, SemanticModel model)
    {
        return m.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(c =>
        {
            var ms = model.GetSymbolInfo(c).Symbol as IMethodSymbol;
            return ms == null ? new MethodDependency(c.ToString(), "UNKOWN", new ImmutableArray<string>()) : new MethodDependency(
                ms.Name,
                ms.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                ms.Parameters.Select(x =>
                        x.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))
                    .ToImmutableList());
        }).ToList();
    }
}