using System.Collections.Generic;

namespace CodeQualityAnalyzer
{
    public interface IIssueTrackerService
    {
        List<FaultyVersion> GetFaultyVersions();
    } 
}
