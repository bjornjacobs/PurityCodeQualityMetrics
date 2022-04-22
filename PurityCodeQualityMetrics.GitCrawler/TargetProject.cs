namespace PurityCodeQualityMetrics.GitCrawler
{
    public class TargetProject
    {
        public static string RepoDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "dev", "repos");
        
        public string OrganizationName { get; set; }
        public string RepositoryName { get; set; }
        
        /// <summary>
        /// The possible names of a solution file.
        /// In some projects this file changes
        /// </summary>
        public List<string> SolutionFile { get; set; }

        public string RepoPath => Path.Combine(RepoDir, RepositoryName);
        
        public string BugLabel { get; set; }
        public string MainBranch { get; set; }

        public TargetProject(string organizationName, string repositoryName, string solutionFileLocation, string bugLabel = "bug", string mainBranch = "origin/master")
        : this(organizationName,  repositoryName,  new List<string>(){solutionFileLocation}, bugLabel, mainBranch)
        { }
        
        public TargetProject(string organizationName, string repositoryName, List<string> solutionFile, string bugLabel = "bug", string mainBranch = "origin/master")
        {
            OrganizationName = organizationName;
            RepositoryName = repositoryName;
            SolutionFile = solutionFile;
            BugLabel = bugLabel;
            MainBranch = mainBranch;
        }

        public static TargetProject? ByName(string name) => GetTargetProjects()
            .FirstOrDefault(x => x.RepositoryName.Equals(name, StringComparison.CurrentCultureIgnoreCase));

        public static TargetProject[] GetTargetProjects()
        {
            return new[]
            {
                new TargetProject("dotnet", "cli", "Microsoft.DotNet.Cli.sln"),
                new TargetProject("dotnet", "roslyn", "Roslyn.sln", "bug", mainBranch: "origin/main"),
                new TargetProject("dotnet", "machinelearning", "Microsoft.ML.sln"),
                new TargetProject("dotnet", "reactive", @"Rx.NET\Source\System.Reactive.sln"),
                new TargetProject("jellyfin", "jellyfin",  new List<string>{"Jellyfin.sln", "MediaBrowser.sln"}),
                new TargetProject("akkadotnet", "akka.net", @"src\Akka.sln", "confirmed bug"),
                new TargetProject("JetBrains", "resharper-unity", @"resharper\resharper-unity.sln"),
                new TargetProject("aspnet", "AspNetCore", @"src\Mvc\Mvc.sln"),
                new TargetProject("aspnet", "EntityFrameworkCore", "EFCore.sln", "type-bug"),
                new TargetProject("OpenRA", "OpenRA", "OpenRA.sln"),
                new TargetProject("Humanizr", "Humanizer", "src/Humanizer.All.sln"),
                new TargetProject("shadowsocks", "shadowsocks-windows", "shadowsocks-windows.sln"),
                new TargetProject("0xd4d", "dnSpy", "dnSpy.sln"),

                new TargetProject("IdentityServer", "IdentityServer4",  
                    new List<string>{@"src\IdentityServer4.Core.sln", @"src\IdentityServer4\IdentityServer4.sln",@"IdentityServer4.sln"},"bug", "origin/main"),
                new TargetProject("icsharpcode", "ILSpy", "ILSpy.sln"),
                new TargetProject("Info Support", "knowNow", @"10-Orchard\src\Orchard.sln"),
            };
        }
    }
}