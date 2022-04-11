using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.Tests.Purity.TestCode;

public class ExceptionTestCases
{
    [ViolationsTest(PurityViolation.ThrowsException)]
    public void ExceptionTest1()
    {
        throw new Exception();
    }
    
    [ViolationsTest(PurityViolation.ThrowsException, PurityViolation.ThrowsException)]
    public void ExceptionTest2()
    {
        throw new Exception();
        throw new NotImplementedException();
    }
    
    
    [ViolationsTest]
    public void ExceptionTestInLocalFunction()
    {
        local();
        
        int local()
        {
            throw new Exception();
            return 1;
        }
    }
}