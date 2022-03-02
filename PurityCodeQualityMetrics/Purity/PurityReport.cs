using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.CsPurity;

namespace PurityCodeQualityMetrics.Purity;

public enum PurityViolation
{
    ModifiesLocalState,
    ModifiesGlobalState,
    NonDeterministic,
    ThrowsException,
    Unknown,
}

public record PurityReport(MethodDeclarationSyntax CSharpMethod)
{
    public MethodDeclarationSyntax CSharpMethod = CSharpMethod;

    public readonly IList<PurityViolation> Violations = new List<PurityViolation>();
    public readonly IList<MethodCall> Dependencies = new List<MethodCall>();
}

public record MethodCall(string Identifier, bool ModifiesReturnValue);