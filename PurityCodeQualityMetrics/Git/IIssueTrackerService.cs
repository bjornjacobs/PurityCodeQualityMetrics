namespace PurityCodeQualityMetrics.Git
{
    public interface IIssueTrackerService
    {
        List<FaultyVersion> GetFaultyVersions();
    } 
}
