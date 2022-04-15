namespace PurityCodeQualityMetrics.GitCrawler.Issue
{
    public interface IIssueTrackerService
    {
        List<FaultyVersion> GetFaultyVersions();
    } 
}
