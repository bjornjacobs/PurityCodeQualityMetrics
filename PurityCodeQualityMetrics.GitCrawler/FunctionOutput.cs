using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.GitCrawler;

public class FunctionOutput
{
    public string CommitHash { get; set; }
    public string FullName { get; set; }
    
    public MethodWithMetrics Before { get; set; }
    public MethodWithMetrics After { get; set; }

    public FunctionOutput(string fullName, MethodWithMetrics before, MethodWithMetrics after)
    {
        FullName = fullName;
        Before = before;
        After = after;
    }
}

public class FunctionRating
{
    
    
    public int TotalLinesOfSourceCOde { get; set; }
    public int DependencyCount { get; set; }
    public List<ViolationWithDistance> Violations { get; set; }
    
    // Metrics
    public double CyclomaticComplexity { get; set; }
    public double SourceLinesOfCode { get; set; }
    public double CommentDensity { get; set; }

    public double WeightedMethodsPerClass { get; set; }
    public double DepthOfInheritance { get; set; }
    public double NumberOfChildren { get; set; }
    public double CouplingBetweenObjects { get; set; }
    public double ResponseForAClass { get; set; }
    public double LackOfCohesionOfMethods { get; set; }

    public double LambdaScore { get; set; }
    public double LambdaCount { get; set; }
    public double SourceLinesOfLambda { get; set; }
    public double LambdaFieldVariableUsageCount { get; set; }
    public double LambdaLocalVariableUsageCount { get; set; }
    public double UnterminatedCollections { get; set; }
    public double LambdaSideEffectCount { get; set; }


    public void SetMetrics(IDictionary<Measure, double> metrics)
    {
        CommentDensity = metrics[Measure.CommentDensity];
        CyclomaticComplexity = metrics[Measure.CyclomaticComplexity];
        SourceLinesOfCode = metrics[Measure.SourceLinesOfCode];
        WeightedMethodsPerClass = metrics[Measure.WeightedMethodsPerClass];
        DepthOfInheritance = metrics[Measure.DepthOfInheritanceTree];
        NumberOfChildren = metrics[Measure.NumberOfChildren];
        CouplingBetweenObjects = metrics[Measure.CouplingBetweenObjects];
        ResponseForAClass = metrics[Measure.ResponseForAClass];
        LackOfCohesionOfMethods = metrics[Measure.LackOfCohesionOfMethods];
        LambdaScore = metrics[Measure.LambdaScore];
        LambdaCount = metrics[Measure.LambdaCount];
        SourceLinesOfLambda = metrics[Measure.SourceLinesOfLambda];
        LambdaFieldVariableUsageCount = metrics[Measure.LambdaFieldVariableUsageCount];
        LambdaLocalVariableUsageCount = metrics[Measure.LambdaLocalVariableUsageCount];
        UnterminatedCollections = metrics[Measure.UnterminatedCollections];
        LambdaSideEffectCount = metrics[Measure.LambdaSideEffectCount];
    }
}

