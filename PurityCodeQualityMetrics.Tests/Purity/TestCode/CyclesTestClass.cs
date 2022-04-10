using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class CyclesTestClass
{
    private int member = 5;
    
    [ViolationsTest(PurityViolation.ReadsLocalState)]
    public void Func1()
    {
        var x  = member;
        Func2();
    }
    
    public void Func2()
    {
        Func3();
    }
    
    public void Func3()
    {
        Func1();

    }
    
    [ViolationsTest(PurityViolation.ModifiesLocalState)]
    public void Func4()
    {
        Func2();
        member = 5;
    }
}