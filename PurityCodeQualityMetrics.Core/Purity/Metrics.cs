namespace PurityCodeQualityMetrics.Purity;

public static class Metrics
{
    public static double Metric1(this PurityScore score)
    {
        return(score.Violations.Where(x => x.Violation != PurityViolation.UnknownMethod).Select(v => 1d / (v.Distance + 1)).Sum() + 1) / (score.DependencyCount + 1);
    }

    private static readonly Dictionary<PurityViolation, double> Coefficients = new()
    {
        {PurityViolation.ModifiesParameter, 1d},
        {PurityViolation.ThrowsException, 1d},
        {PurityViolation.UnknownMethod, 1d},
        {PurityViolation.ModifiesGlobalState, 1d},
        {PurityViolation.ReadsGlobalState, 1d},
        {PurityViolation.ModifiesLocalState, 1d},
        {PurityViolation.ReadsLocalState, 1d}
    };

    public static double Metric2(this PurityScore score)
    {
        return score.Violations.GroupBy(x => x.Violation).Select(x => x.Count() * Coefficients[x.Key]).Sum() / (score.DependencyCount + 1);
    }
}