using System.Text.Json.Serialization;

namespace PurityCodeQualityMetrics.Purity;

public class ViolationWithDistance
{
    [JsonPropertyName("D")]
    public int Distance { get; set; }
    [JsonPropertyName("V")]
    public PurityViolation Violation { get; set; }


    public ViolationWithDistance(int distance, PurityViolation violation)
    {
        Distance = distance;
        Violation = violation;
    }
}

public class PurityScore
{
    public PurityScore(PurityReport report, List<ViolationWithDistance> violations, int linesOfSourceCode)
    {
        Report = report;
        Violations = violations;
        LinesOfSourceCode = linesOfSourceCode;
        CalculateLevel();
    }

    public PurityReport Report { get; set; }
    public Puritylevel Puritylevel { get; set; }
    
    public bool ReturnIsFresh { get; set; }
    
    public int DependencyCount { get; set; }
    public List<ViolationWithDistance> Violations { get; set; }

    public int LinesOfSourceCode { get; set; }
    
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
        
        if (Violations.Any(x => x.Violation is PurityViolation.ModifiesNonFreshObject))
        {
            Puritylevel = Puritylevel.NonFreshObjectImpure;
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
    NonFreshObjectImpure,
    Impure,
    ParameteclyImpure
}