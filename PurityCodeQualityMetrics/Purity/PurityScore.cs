using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace PurityCodeQualityMetrics.Purity;

public class ViolationWithDistance
{
    public int Distance { get; set; }
    
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
    
    public void CalculateLevel(int level = Int32.MaxValue)
    {
        var v = Violations.Where(x => x.Distance <= level).ToList();
        
        if (v.Any(x => x.Violation is PurityViolation.ReadsGlobalState or PurityViolation.ModifiesGlobalState))
        {
            Puritylevel = Puritylevel.Impure;
            return;
        }

        if (v.Any(x => x.Violation is PurityViolation.ModifiesLocalState or PurityViolation.ReadsLocalState))
        {
            Puritylevel = Puritylevel.LocallyImpure;
            return;
        }

        if (v.Any(x => x.Violation is PurityViolation.ModifiesParameter))
        {
            Puritylevel = Puritylevel.ParameteclyImpure;
            return;
        }
        
        if (v.Any(x => x.Violation is PurityViolation.ModifiesNonFreshObject))
        {
            Puritylevel = Puritylevel.NonFreshObjectImpure;
            return;
        }

        if (v.Any(x => x.Violation is PurityViolation.ThrowsException))
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