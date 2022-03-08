namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class TestClass
{
    private static int _global = 9;
    private int _member = 5;
    
    public int PureFunction()
    {
        return 1;
    }

    public int LocallyImpure()
    {
        _member = 1;
        return _member;
    }

    public int GloballyImpure()
    {
        _global = 16;
        return _global;
    }

    public int GloballyImpureOtherClass()
    {
        TestClass2.PublicState = 5;
        var value = 5;
        value = TestClass2.PublicState;
        return value;
    }
}

public class TestClass2
{
    public static int PublicState = 1;
}