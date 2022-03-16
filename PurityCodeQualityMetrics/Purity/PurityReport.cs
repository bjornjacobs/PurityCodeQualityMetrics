using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PurityCodeQualityMetrics.Purity;

public enum PurityViolation
{
    //Minor
    ThrowsException = 1,
    ModifiesParameters = 2,
    
    //Local
    ModifiesLocalPrivateState = 10,
    ReadsLocalPrivateState = 11,
    ModifiesLocalPublicState = 12,
    ReadsLocalPublicState = 13,

    //Global
    ReadsGlobalState = 20,
    ModifiesGlobalState = 21,
}

public class PurityReport
{
    [NotMapped] public List<PurityViolation> Violations { get; set; } = null!;

    public string ViolationsJson
    {
        get => JsonConvert.SerializeObject(Violations);
        set => Violations = JsonConvert.DeserializeObject<List<PurityViolation>>(value)!;
    }

    public List<MethodDependency> Dependencies { get; set; } = null!;

    public bool ReturnValueIsFresh { get; set; }
    public bool IsMarkedByHand { get; set; }
    public bool IsLambda { get; set; }

    public string Name { get; init; } = null!;
    public string Namespace { get; init; } = null!;

    [Key] public string FullName { get; init; } = null!;

    public string Type { get; init; } = null!;

    [NotMapped] public List<string> ParameterTypes { get; private set; } = null!;

    public string ParameterTypesJson
    {
        get => JsonConvert.SerializeObject(ParameterTypes);
        set => ParameterTypes = JsonConvert.DeserializeObject<List<string>>(value)!;
    }

    public PurityReport()
    {
    }

    public PurityReport(string name, string Namespace, string Type, List<string> ParameterTypes)
    {
        this.Name = name;
        this.Namespace = Namespace;
        this.Type = Type;
        this.ParameterTypes = ParameterTypes;
        this.FullName = $"{Namespace}.{name}";
        Dependencies = new List<MethodDependency>();
        Violations = new List<PurityViolation>();
    }


    public override string ToString()
    {
        return
            $"Name: {Name} - [Violations: {string.Join(", ", Violations)}] - Dependencies: [{string.Join(", ", Dependencies.Select(x => x?.Name ?? "UNKNOWN"))}]";
    }
}

public class MethodDependency
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; init; } = null!;
    public string Namespace { get; init; } = null!;

    public string FullName { get; init; } = null!;

    public string Type { get; init; } = null!;

    [NotMapped] public List<string> ParameterTypes { get; private set; } = null!;

    public string ParameterTypesJson
    {
        get => JsonConvert.SerializeObject(ParameterTypes);
        set => ParameterTypes = JsonConvert.DeserializeObject<List<string>>(value)!;
    }

    public bool ShouldBeFresh { get; init; }
    public bool DependsOnReturnToBeFresh { get; init; }

    public bool IsInterface { get; init; }
    public bool IsLambda { get; set; }

    public MethodDependency()
    {
    }

    public MethodDependency(string name, string @namespace, string type, List<string> parameterTypes, bool shouldBeFresh,
        bool dependsOnReturnToBeFresh, bool isInterface)
    {
        Name = name;
        Namespace = @namespace;
        ParameterTypes = parameterTypes;
        Type = type;
        ShouldBeFresh = shouldBeFresh;
        DependsOnReturnToBeFresh = dependsOnReturnToBeFresh;
        FullName = $"{@namespace}.{name}";
        IsInterface = isInterface;
    }
}