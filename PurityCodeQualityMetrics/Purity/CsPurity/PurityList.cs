namespace PurityCodeQualityMetrics.Purity.CsPurity;

public class PurityList
{
    public static readonly List<(string, PurityValue)> List = new()
    {
        ("Console.Read", PurityValue.Impure),
        ("Console.ReadLine", PurityValue.Impure),
        ("Console.ReadKey", PurityValue.Impure),
        ("DateTime.Now", PurityValue.Impure),
        ("DateTimeOffset", PurityValue.Impure),
        ("Random.Next", PurityValue.Impure),
        ("Guid.NewGuid", PurityValue.Impure),
        ("System.IO.Path.GetRandomFileName", PurityValue.Impure),
        ("System.Threading.Thread.Start", PurityValue.Impure),
        ("Thread.Abort", PurityValue.Impure),
        ("Console.Write", PurityValue.Impure),
        ("Console.WriteLine", PurityValue.Impure),
        ("System.IO.Directory.Create", PurityValue.Impure),
        ("Directory.Move", PurityValue.Impure),
        ("Directory.Delete", PurityValue.Impure),
        ("File.Create", PurityValue.Impure),
        ("File.Move", PurityValue.Impure),
        ("File.Delete", PurityValue.Impure),
        ("File.ReadAllBytes", PurityValue.Impure),
        ("File.WriteAllBytes", PurityValue.Impure),
        ("System.Net.Http.HttpClient.GetAsync", PurityValue.Impure),
        ("HttpClient.PostAsync", PurityValue.Impure),
        ("HttpClinet.PutAsync", PurityValue.Impure),
        ("HttpClient.DeleteAsync", PurityValue.Impure),
        ("IDisposable.Dispose", PurityValue.Impure),
        ("List.IsCompatibleObject()", PurityValue.Pure),
        ("List.Add()", PurityValue.Impure),
        ("List.EnsureCapacity()", PurityValue.Impure),
        ("List.GetEnumerator()", PurityValue.Pure),
        ("List.TrimExcess()", PurityValue.Pure),
        ("List.Synchronized()", PurityValue.Pure),
        ("SynchronizedList.Add()", PurityValue.Impure),
        ("SynchronizedList.GetEnumerator()", PurityValue.Pure),
        ("List.Dispose()", PurityValue.Pure),
        ("System.Linq.Where", PurityValue.Pure),
        ("System.Linq.FirstOrDefault", PurityValue.Pure),
        ("a.Where", PurityValue.Pure),
        ("a.Where(x=>x==5).Where", PurityValue.Pure),
        ("a.Where(x=>x==5).Where(y=>y>5).FirstOrDefault", PurityValue.Pure)
    };
}