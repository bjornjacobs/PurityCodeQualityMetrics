using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class ModifyNonFreshObjectTestCases
{
    private Poco obj = new Poco();
    
    [ViolationsTest(PurityViolation.ModifiesNonFreshObject, PurityViolation.ReadsLocalState)]
    public void ModifyNonFresh()
    {
        var d = obj;
        d.Field = 5;
    }
    
    [ViolationsTest]
    public void ModifiesFresh()
    {
        var d = new Poco();
        d.Field = 5;
    }
    
    //This should 
    [ViolationsTest]
    public void ModifiesFreshFromOtherFunction()
    {
        var d = CreatePocoObject();
        d.Field = 5;
    }

    public Poco CreatePocoObject() => new Poco();
}