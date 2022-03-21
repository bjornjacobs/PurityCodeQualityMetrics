using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics;

public class PurityScore
{
    public PurityScore(PurityReport report, List<PurityViolation> violations)
    {
        Report = report;
        Violations = violations;
        Puritylevel = CalculateLevel(Violations);
    }

    public PurityReport Report { get; set; }
    public Puritylevel Puritylevel { get; set; }
    public List<PurityViolation> Violations { get; set; }
    
    private static Puritylevel CalculateLevel(List<PurityViolation> violations)
    {
        if (violations.Contains(PurityViolation.ReadsGlobalState) ||
            violations.Contains(PurityViolation.ModifiesGlobalState))
            return Puritylevel.Impure;
        
        if (violations.Contains(PurityViolation.ModifiesLocalState) ||
            violations.Contains(PurityViolation.ReadsLocalState))
            return Puritylevel.LocallyImpure;
        
        if (violations.Contains(PurityViolation.ModifiesParameters))
            return Puritylevel.ParameteclyImpure;
        
        if (violations.Contains(PurityViolation.ThrowsException))
            return Puritylevel.ThrowsException;

        return Puritylevel.Pure;
    }
}

public enum Puritylevel
{
    Pure,
    ThrowsException,
    LocallyImpure,
    Impure,
    ParameteclyImpure
}