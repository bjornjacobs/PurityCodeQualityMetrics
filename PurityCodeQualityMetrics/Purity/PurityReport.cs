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
    ModifiesLocalState = 10,
    ReadsLocalState = 11,

    //Global
    ReadsGlobalState = 20,
    ModifiesGlobalState = 21,
}

public enum Scoping
{
    Local,
    Parameter,
    Field,
    Global
}

public enum MethodType
{
    Lambda,
    Local,
    Method,
    Global
}

public class PurityReport
{
    public string Name { get; init; } = null!;
    public string Namespace { get; init; } = null!;
    [Key] public string FullName { get; init; } = null!;
    public string ReturnType { get; init; } = null!;
    
    public bool ReturnValueIsFresh { get; set; }
    public bool IsMarkedByHand { get; set; }
    public MethodType MethodType { get; set; }
    
    [NotMapped] public List<string> ParameterTypes { get; private set; } = null!;
    
    [NotMapped] 
    public List<PurityViolation> Violations { get; set; }
    public List<MethodDependency> Dependencies { get; set; }


    public PurityReport()
    {
    }

    public PurityReport(string name, string @namespace, string returnType, List<string> parameterTypes)
    {
        Name = name;
        Namespace = @namespace;
        FullName = @namespace + "." + name;
        ReturnType = returnType;
        ParameterTypes = parameterTypes;
        FullName = $"{@namespace}.{name}";
        Dependencies = new List<MethodDependency>();
        Violations = new List<PurityViolation>();
    }


    public override string ToString()
    {
        return
            $"Name: {Name} - [Violations: {string.Join(", ", Violations)}] - Dependencies: [{string.Join(", ", Dependencies.Select(x => x?.Name ?? "UNKNOWN"))}]";
    }
    
    //For entity framework
    public string ViolationsJson
    {
        get => JsonConvert.SerializeObject(Violations);
        set => Violations = JsonConvert.DeserializeObject<List<PurityViolation>>(value)!;
    }

    public string ParameterTypesJson
    {
        get => JsonConvert.SerializeObject(ParameterTypes);
        set => ParameterTypes = JsonConvert.DeserializeObject<List<string>>(value)!;
    }
}

public class MethodDependency
{
    //Method information
    public string Name { get; init; } = null!;
    public string Namespace { get; init; } = null!;
    public string FullName { get; init; } = null!;
    public string ReturnType { get; init; } = null!;
    [NotMapped] public List<string> ParameterTypes { get; private set; } = null!;
    
    public MethodType MethodType { get; set; }
    public bool IsInterface { get; init; }


    //Dependency information
    public Scoping Scoping { get; private set; }
    public bool ReturnShouldBeFresh { get; init; }

    public MethodDependency()
    {
    }

    public MethodDependency(string name, Scoping scoping)
    {
        Name = name;
        Namespace = "UNKNOWN";
        FullName = Namespace + "." + name;
        Scoping = scoping;
        ReturnType = "UNKnOWN";
    }

    public MethodDependency(string name, string @namespace, string returnType, List<string> parameterTypes, MethodType methodType, bool isInterface, Scoping scoping, bool returnShouldBeFresh)
    {
        Name = name;
        Namespace = @namespace;
        FullName = @namespace + "." + name;
        ReturnType = returnType;
        ParameterTypes = parameterTypes;
        MethodType = methodType;
        IsInterface = isInterface;
        Scoping = scoping;
        ReturnShouldBeFresh = returnShouldBeFresh;
    }

    //For Entity Framework
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public string ParameterTypesJson
    {
        get => JsonConvert.SerializeObject(ParameterTypes);
        set => ParameterTypes = JsonConvert.DeserializeObject<List<string>>(value)!;
    }
}