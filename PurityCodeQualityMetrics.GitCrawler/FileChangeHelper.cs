using System.Text.RegularExpressions;
using LibGit2Sharp;
using PurityCodeQualityMetrics.Purity;

namespace PurityCodeQualityMetrics.GitCrawler;

public static class FileChangeHelper
{
    public static List<FileChange> GetFileChangesFromPatch(this PatchEntryChanges patch)
    {
        var path = patch.Path.Replace("/", "\\");
        

        var matches = Regex.Matches(patch.Patch, @"@@ -((\d*),(\d*)) \+((\d*),(\d*)) @@");
        
        var changes = matches
            .Select(x =>
            {
                
                
                var start = patch.Patch.Substring(x.Index);
                string[] delimiterChars = { "\n+", "\n-"};
                
                var before = start.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries).First().Count(x => x == '\n');
                
                return new FileChange
                {
                    Added = new LinesChange
                    {
                        Path = path, Start = int.Parse(x.Groups[2].Value) + before, Count =  patch.LinesDeleted //int.Parse(x.Groups[3].Value)
                    },
                    Removed = new LinesChange
                    {
                        Path = path, Start = int.Parse(x.Groups[5].Value) + before, Count = patch.LinesAdded //int.Parse(x.Groups[6].Value)
                    },
                    Path = path
                };
            })
            .ToList();


        return changes;
    }
    
    public static List<PurityScore> FilterChangedScores(this List<LinesChange> changes, List<PurityScore> scores)
    {
        changes = changes.Where(x => x.Path.EndsWith(".cs", StringComparison.CurrentCultureIgnoreCase)).ToList();
        
        var changedMethods = scores
            .Where(s => s.Report.ReportInChanges(changes))
            .ToList();

        return changedMethods;
    }

    public static bool ReportInChanges(this PurityReport report, List<LinesChange> changes)
    {
        return changes.Any(c => report.FilePath.EndsWith(c.Path, StringComparison.CurrentCultureIgnoreCase) &&
                                ((c.Start >= report.LineStart && c.Start <= report.LineEnd ||
                                  c.End >= report.LineStart && c.End <= report.LineEnd) ||
                                 report.LineStart >= c.Start && report.LineStart <= c.End ||
                                 report.LineEnd >= c.Start && report.LineEnd <= c.End));
    }
}


public class FileChange
{
    public string Path { get; set; }
    public LinesChange Added { get; set; }
    public LinesChange Removed { get; set; }
}

public class LinesChange {

    public string Path { get; set; }
    public int Start { get; set; }
    public int Count { get; set; }
    public int End => Start + Count;

}