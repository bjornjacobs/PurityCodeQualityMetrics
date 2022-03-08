using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PurityCodeQualityMetrics.Purity.CsPurity;

namespace PurityCodeQualityMetrics.Purity;

public enum PurityViolation
{
    ModifiesLocalState,
    ModifiesGlobalState,
    ReadsGlobalState,
    ReadsLocalState,
    ThrowsException,
    Unknown,
}


public record PurityReport(string Name, string Namespace, string Type, ImmutableList<string> ParameterTypes)
{
    public readonly List<PurityViolation> Violations = new List<PurityViolation>();
    public readonly List<MethodDependency> Dependencies = new List<MethodDependency>();

    public override string ToString()
    {
        return $"Name: {Name} - [Violations: {string.Join( ", ", Violations)}] - Dependencies: [{string.Join(", ", Dependencies.Select(x => x?.Name ?? "UNKOWN"))}]";
    }
}

public record MethodDependency(string Name, string Namespace, IImmutableList<string> ParameterTypes);