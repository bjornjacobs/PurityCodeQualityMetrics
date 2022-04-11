namespace PurityCodeQualityMetrics.Purity;

public class PurityScore
{
    public PurityScore(PurityReport report, List<(int Distance,PurityViolation Violation)> violations)
    {
        Report = report;
        Violations = violations;
         CalculateLevel();
    }

    public PurityReport Report { get; set; }
    public Puritylevel Puritylevel { get; set; }
    
    public bool ReturnIsFresh { get; set; }
    
    public int DependencyCount { get; set; }
    public List<(int Distance,PurityViolation Violation)> Violations { get; set; }
    
    public void CalculateLevel()
    {
        if (Violations.Any(x => x.Violation is PurityViolation.ReadsGlobalState or PurityViolation.ModifiesGlobalState))
        {
            Puritylevel = Puritylevel.Impure;
            return;
        }

        if (Violations.Any(x => x.Violation is PurityViolation.ModifiesLocalState or PurityViolation.ReadsLocalState))
        {
            Puritylevel = Puritylevel.LocallyImpure;
            return;
        }

        if (Violations.Any(x => x.Violation is PurityViolation.ModifiesParameter))
        {
            Puritylevel = Puritylevel.ParameteclyImpure;
            return;
        }

        if (Violations.Any(x => x.Violation is PurityViolation.ThrowsException))
        {
            Puritylevel = Puritylevel.ThrowsException;
            return;
        }

        Puritylevel = Puritylevel.Pure;
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