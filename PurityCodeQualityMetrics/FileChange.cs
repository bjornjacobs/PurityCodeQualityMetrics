namespace PurityCodeQualityMetrics;

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