namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class TestClass
{
    private static int x = 9;
    
    private int _member = 5;
    public int PureFunction()
    {
        return 1;
    }

    public int LocallyImpure()
    {
        return _member;
    }

    public int GloballyImpure()
    {
        x = 16;
        return x;
    }
}