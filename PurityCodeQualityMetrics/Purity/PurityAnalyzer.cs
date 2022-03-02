using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace PurityCodeQualityMetrics.Purity;

public class PurityAnalyzer
{
    public async Task<IList<PurityReport>> GeneratePurityReports(string project)
    {
        var purityReports = new List<PurityReport>();

        using var workspace = MSBuildWorkspace.Create();

        var currentProject = await workspace.OpenProjectAsync(@"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\PurityCodeQualityMetrics\PurityCodeQualityMetrics.csproj");

        var compilation = await currentProject.GetCompilationAsync();
        if (compilation == null) Environment.Exit(1);

        foreach (var tree in compilation.SyntaxTrees)
        {
            Console.WriteLine(tree.FilePath);
            var model = compilation.GetSemanticModel(tree);

            var methodDeclarations = tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>();

            foreach (var m in methodDeclarations)
            {
                var report = new PurityReport(m);

                Console.WriteLine(m.Identifier);
                var calls = m.DescendantNodes().OfType<InvocationExpressionSyntax>();
                foreach (var c in calls)
                {
                    var s = model.GetSymbolInfo(c);
                    if (s.Symbol != null)
                        Console.WriteLine(s.Symbol);
                }

            }
        }
        
        return new List<PurityReport>();
    }
}