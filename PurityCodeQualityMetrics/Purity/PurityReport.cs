using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace PurityCodeQualityMetrics.Purity;

public enum PurityViolation
{
    //Minor
    ThrowsException = 1,
    ModifiesParameter = 2,
    ModifiesNonFreshObject = 3,
    
    //Local
    ModifiesLocalState = 10,
    ReadsLocalState = 11,

    //Global
    ReadsGlobalState = 20,
    ModifiesGlobalState = 21,
    
    UnknownMethod = 30,
}

public enum MethodType
{
    Lambda,
    Local,
    Method,
    Getter,
    Setter,
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
    
    public string FilePath { get; set; }
    public int LineStart { get; set; }
    public int LineEnd { get; set; }
    
    public int SourceLinesOfCode { get; set; }
    
    [NotMapped] public List<string> ParameterTypes { get; set; } = null!;
    
    [NotMapped] public List<PurityViolation> Violations { get; set; }
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
        set { ParameterTypes = JsonConvert.DeserializeObject<List<string>>(value)!; }
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
    
    public List<MethodDependency> Overrides { get; set; }


    //Dependency information
    public bool ReturnShouldBeFresh { get; set; }
    public bool FreshDependsOnMethodReturnIsFresh { get; set; }
    
    


    public MethodDependency()
    {
    }

    public MethodDependency(string name)
    {
        Name = name;
        Namespace = "UNKNOWN";
        FullName = Namespace + "." + name;
        ReturnType = "UNKnOWN";
    }

    public MethodDependency(string name, string @namespace, string returnType, List<string> parameterTypes, MethodType methodType, bool isInterface, bool returnShouldBeFresh)
    {
        Name = name;
        Namespace = @namespace;
        FullName = @namespace + "." + name;
        ReturnType = returnType;
        ParameterTypes = parameterTypes;
        MethodType = methodType;
        IsInterface = isInterface;
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