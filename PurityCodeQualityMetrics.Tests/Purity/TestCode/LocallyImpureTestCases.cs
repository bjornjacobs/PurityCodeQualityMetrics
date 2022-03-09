namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class LocallyImpureTestClass
{
    private int _member;
    
    public int LocallyImpure()
    {
        _member = 1;
        return _member;
    }

}