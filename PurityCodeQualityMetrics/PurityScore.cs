using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics;

public class PurityScore
{
    public static PurityScore Unknown()
    {
        var score = new PurityScore(null);
        score.Violations.Add(PurityViolation.UnknownMethod);
        return score;
    }

    public PurityScore(PurityReport report)
    {
        this.Report = report;
        Violations = new List<PurityViolation>();
    }

    public PurityReport Report { get; set; }
    public Puritylevel Puritylevel { get; set; }
    public List<PurityViolation> Violations { get; set; }
}

public enum Puritylevel
{
    Pure,
    ThrowsException,
    LocallyImpure,
    Impure,
    ParameteclyImpure
}