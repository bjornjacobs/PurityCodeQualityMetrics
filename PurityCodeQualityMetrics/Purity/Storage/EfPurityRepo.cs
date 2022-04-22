using Microsoft.EntityFrameworkCore;

namespace PurityCodeQualityMetrics.Purity.Storage;

public class DatabaseContext : DbContext
{
    public DbSet<PurityReport> Reports { get; set; } = null!;
    public DbSet<MethodDependency> Dependencies { get; set; } = null!;

    public string DbPath { get; }
    
    public DatabaseContext(string dbName)
    {
        var folder = Environment.SpecialFolder.Desktop;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, $"/purity_data/{dbName}.db");
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    }
}

public class EfPurityRepo : IPurityReportRepo
{
    private string _dbname;

    public EfPurityRepo(string dbname)
    {
        _dbname = dbname;
    }

    public PurityReport? GetByName(string name)
    {
        using var db = new DatabaseContext(_dbname);
        db.Database.EnsureCreated();
        return db.Reports.Where(x => x.Name == name).Include(x => x.Dependencies).FirstOrDefault();
    }

    public PurityReport? GetByFullName(string fullname)
    {
        using var db = new DatabaseContext(_dbname);
        db.Database.EnsureCreated();
        return db.Reports.Where(x => x.FullName == fullname).Include(x => x.Dependencies).FirstOrDefault();
    }

    public List<PurityReport> GetAllReports(string start = "")
    {
        using var db = new DatabaseContext(_dbname);
        db.Database.EnsureCreated();
        return db.Reports.Where(x => x.FullName.StartsWith(start)).Include(x => x.Dependencies).ToList();
    }

    public void AddRange(IEnumerable<PurityReport> reports)
    {
        using var db = new DatabaseContext(_dbname);
        db.Database.EnsureCreated();

        foreach (var report in reports)
        {
            int i = reports.ToList().IndexOf(report);
            Console.WriteLine(i);
            var toRemove = db.Reports.Where(x => x.FullName == report.FullName).Include(x => x.Dependencies);
            foreach (var remove in toRemove)
            {
                db.Dependencies.RemoveRange(remove.Dependencies);
                db.Reports.Remove(remove);
            }
            
            db.Reports.Add(report);
            db.SaveChanges();
        }
    }

    public void RemoveClassesInFiles(List<string> path)
    {
        using var db = new DatabaseContext(_dbname);
        
        var toRemove = db.Reports
            .Where(x => path.Any(y => x.FilePath.StartsWith(y, StringComparison.CurrentCultureIgnoreCase)))
            .Include(x => x.Dependencies);
        
        foreach (var remove in toRemove)
        {
            db.Dependencies.RemoveRange(remove.Dependencies);
            db.Reports.Remove(remove);
        }
        db.SaveChanges();
    }
    
    public void Clear()
    {
        using var db = new DatabaseContext(_dbname);
        db.Database.EnsureDeleted();
    }
}