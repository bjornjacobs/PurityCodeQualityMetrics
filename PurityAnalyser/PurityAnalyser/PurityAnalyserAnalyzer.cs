using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using PurityCodeQualityMetrics.Purity.Violations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace PurityAnalyser
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PurityAnalyserAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PurityAnalyser";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Method);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
          
            var methodSymbol = (IMethodSymbol)context.Symbol;


            var isPureMarked = methodSymbol.GetAttributes().Any(x => x.ToString() == "System.Diagnostics.Contracts.PureAttribute");
            var policy = new IdentifierViolationPolicy();


            if (isPureMarked)
            {
                var tree = methodSymbol.Locations[0].SourceTree;
                var node = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First(x => x.Identifier.ValueText == methodSymbol.Name);
                var model = context.Compilation.GetSemanticModel(tree);


                var violations = policy.Check(node, tree, model);
                
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, methodSymbol.Locations[0], string.Join(",", violations));

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
