using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

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

public class PurityReport
{
    [NotMapped] public List<PurityViolation> Violations { get; set; }

    public string ViolationsJson
    {
        get => JsonConvert.SerializeObject(Violations);
        set => Violations = JsonConvert.DeserializeObject<List<PurityViolation>>(value)!;
    }

    public List<MethodDependency> Dependencies { get; set; }

    public bool ReturnValueIsFresh { get; set; }
    public bool IsMarkedByHand { get; set; }
    public bool IsLambda { get; set; }

    public string Name { get; init; }
    public string Namespace { get; init; }

    [Key] public string FullName { get; init; }

    public string Type { get; init; }

    [NotMapped] public List<string> ParameterTypes { get; private set; }
    
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

    public string Name { get; init; }
    public string Namespace { get; init; }

    public string FullName { get; init; }

    public string Type { get; init; }
    
    [NotMapped] public List<string> ParameterTypes { get; private set; }

    public string ParameterTypesJson
    {
        get => JsonConvert.SerializeObject(ParameterTypes);
        set => ParameterTypes = JsonConvert.DeserializeObject<List<string>>(value);
    }

    public bool ShouldBeFresh { get; init; }
    public bool DependsOnReturnToBeFresh { get; init; }

    public bool IsInterface { get; init; }
    public bool IsLambda { get; set; }

    public MethodDependency()
    {
    }

    public MethodDependency(string Name, string Namespace, string type, List<string> ParameterTypes, bool ShouldBeFresh,
        bool DependsOnReturnToBeFresh, bool isInterface)
    {
        this.Name = Name;
        this.Namespace = Namespace;
        this.ParameterTypes = ParameterTypes;
        this.Type = type;
        this.ShouldBeFresh = ShouldBeFresh;
        this.DependsOnReturnToBeFresh = DependsOnReturnToBeFresh;
        this.FullName = $"{Namespace}.{Name}";
        IsInterface = isInterface;
    }
}