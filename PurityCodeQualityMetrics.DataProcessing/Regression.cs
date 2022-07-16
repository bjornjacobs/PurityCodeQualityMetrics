using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.DataProcessing;

public class Regression
{
    public static string Path = @"C:\Users\BjornJ\dev\PurityCodeQualityMetrics\data\regression";

    public static void EnsureDirectoryExists() => Directory.CreateDirectory(Path);

    public static Measure[] BaseLineMetrics = new[]
    {
        Measure.CyclomaticComplexity,
        Measure.CommentDensity,
        Measure.DepthOfInheritanceTree,
        Measure.ResponseForAClass,
        Measure.WeightedMethodsPerClass,
        Measure.LackOfCohesionOfMethods
    };
    
    public static Measure[] BaseLineMetricsFunctional = new[]
    {
        Measure.LambdaCount,
        Measure.LambdaScore,
        Measure.UnterminatedCollections,
        Measure.SourceLinesOfLambda,
        Measure.LambdaFieldVariableUsageCount,
        Measure.LambdaLocalVariableUsageCount,
        Measure.LambdaSideEffectCount,
    };

    public static double[] Generate(MethodWithMetrics m, bool isFaulty, ModelType modelType)
    {
        return modelType switch
        {
            ModelType.Purity => GeneratePurity(m, isFaulty),
            ModelType.BaselineFP => GenerateBaselineFp(m, isFaulty),
            ModelType.BaselineOOP => GenerateBaselineOOp(m, isFaulty),
            ModelType.Baseline => GenerateBaseline(m, isFaulty)
        };
    }
    
    public static double[] Generate(MethodWithMetrics m, bool isFaulty)
    {
        var one = Generate(m, isFaulty, ModelType.Purity);
        var two = Generate(m, isFaulty, ModelType.BaselineFP);
        var three = Generate(m, isFaulty, ModelType.BaselineOOP);

        return one.Take(one.Length - 1).Concat(two.Take(two.Length - 1)).Concat(three).ToArray();
    }


    private static int c = 0;
    static Random rnd = new Random();
    public static double[] GeneratePurity(MethodWithMetrics method, bool isFaulty)
    {
        
        
        double[] data = new Double[7];
        data[0] = method.Purity(PurityViolation.ReadsLocalState);
        data[1] = method.Purity(PurityViolation.ModifiesLocalState);
        data[2] = method.Purity(PurityViolation.ReadsGlobalState);
        data[3] = method.Purity(PurityViolation.ModifiesGlobalState);
        data[4] = method.Purity(PurityViolation.ModifiesParameter);
        data[5] = method.Purity(PurityViolation.ModifiesNonFreshObject);
        data[6] = isFaulty ? 1 : 0;
        return data;
    }
    
    public static double[] GenerateBaseline(MethodWithMetrics method, bool isFaulty)
    {
        var data = method.Metrics.Values.Select(x => (double)x).ToList();
        data.Add(isFaulty ? 1 : 0);
        return data.ToArray();
    }
    
    public static double[] GenerateBaselineFp(MethodWithMetrics method, bool isFaulty)
    {
        var data = method.Metrics.Where(x => BaseLineMetricsFunctional.Contains(x.Key)).Select(x => (double)x.Value).ToList();
        data.Add(isFaulty ? 1 : 0);
        return data.ToArray();
    }
    
    public static double[] GenerateBaselineOOp(MethodWithMetrics method, bool isFaulty)
    {
        var metrics = method.Metrics.Where(x => BaseLineMetrics.Contains(x.Key)).ToList();
        foreach (var m in BaseLineMetrics)
        {
            metrics.EnsureExists(m);
        }

        metrics = metrics.OrderBy(x => x.Key.ToString()).ToList();
        
        
        var data = metrics.Select(x => (double)x.Value).ToList();
        data.Add(isFaulty ? 1 : 0);
        return data.ToArray();
    }
    
    public static double[] GenerateCombined(MethodWithMetrics method, bool isFaulty)
    {
        var data = method.Metrics.Where(x => BaseLineMetrics.Contains(x.Key)).Select(x => (double)x.Value).ToList();
        data.AddRange(GeneratePurity(method, isFaulty));
        return data.ToArray();
    }


    public static string[] ToLines(double[][] values)
    {
        return values.Select(x => string.Join(";", x.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)))).ToArray();
    }
}

public enum ModelType
{
    BaselineOOP,
    BaselineFP,
    Purity,
    PurityLite,
    Baseline,
}

public static class Score
{
    public static int Mode =2;

    public static void EnsureExists(this List<KeyValuePair<Measure, int>> lst, Measure measure)
    {
        if (lst.All(x => x.Key != measure))
        {
            lst.Add(new KeyValuePair<Measure, int>(measure, 0));
        }
    }
    
    public static string ToStr(this ModelType type)
    {
        return type switch
        {
            ModelType.Purity => "purity",
            ModelType.BaselineFP => "baseline-fp",
            ModelType.BaselineOOP => "baseline-oop",
            ModelType.Baseline => "baseline"
        };
    }
    
    public static double Purity(this MethodWithMetrics m, PurityViolation violation)
    {
        switch (Mode)
        {
            case 0: return m.Violations.Count(x => x.Violation == violation);
            case 1: return (m.Violations.Count(x => x.Violation == violation)) / (double)m.TotalLinesOfSourceCode ;
            case 2: return m.Violations.Where(x => x.Violation == violation).Select(x => 1 / (x.Distance + 1)).Sum();
            case 3: return m.Violations.Where(x => x.Violation == violation).Select(x => 1 / (x.Distance + 1)).Sum() / (double)m.TotalLinesOfSourceCode;
            case 4: return m.Violations.Count(x => x.Distance == 0); //LITE
        }

        return -1;
    }
}