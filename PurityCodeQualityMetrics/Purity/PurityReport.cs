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
    ModifiesParameters,
}


public record PurityReport(string Name, string Namespace, string Type, ImmutableList<string> ParameterTypes)
{
    public readonly List<PurityViolation> Violations = new List<PurityViolation>();
    public readonly List<MethodDependency> Dependencies = new List<MethodDependency>();
    
    public bool ReturnValueIsFresh = false;
    public bool IsMarkedByHand = false;

    public override string ToString()
    {
        return $"Name: {Name} - [Violations: {string.Join( ", ", Violations)}] - Dependencies: [{string.Join(", ", Dependencies.Select(x => x?.Name ?? "UNKNOWN"))}]";
    }
}

public record MethodDependency(string Name, string Namespace, IImmutableList<string> ParameterTypes, bool ShouldBeFresh, bool DependsOnReturnToBeFresh);