using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class ParameterImpure
{
    [ViolationsTest(PurityViolation.ModifiesParameter)]
    public void EditParameterField(Poco poco)
    {
        poco.Field = 5;
    }
    
    [ViolationsTest(PurityViolation.ModifiesParameter)] 
    public void EditParameterProperty(Poco poco)
    {
        poco.Property = 5;
    }
    
    [ViolationsTest(PurityViolation.ModifiesParameter)]
    public void EditParameterFieldOfObject(Poco poco)
    {
        poco.Object.Field = 5;
    }
    
    [ViolationsTest]
    public void OverwriteParameter(Poco poco)
    {
        poco = new Poco();
    }
}

public class Poco
{
    public int Property { get; set; }
    public int Field = 1;

    public Poco Object;
}