using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics.CodeMetrics;
using PurityCodeQualityMetrics.Purity.Util;
using PurityCodeQualityMetrics.Purity.Violations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurityCodeQualityMetrics.Purity
{
    public class PurityAnalyser
    {

       
        private readonly ILogger _logger;

        public PurityAnalyser(ILogger logger)
        {
            _logger = logger;
        }

        private readonly List<IViolationPolicy> _violationsPolicies = new List<IViolationPolicy>
        {
            new ThrowsExceptionViolationPolicy(),
            new IdentifierViolationPolicy()
        };

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

        public List<MethodDependency> ExtractMethodDependencies(SyntaxNode m, SemanticModel model, Solution solution)
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
}
