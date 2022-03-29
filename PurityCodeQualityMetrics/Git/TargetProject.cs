namespace CodeQualityAnalyzer
{
    class TargetProject
    {
        public string OrganizationName;
        public string RepositoryName;
        public string SolutionFileLocation;
        public string BugLabel;

        public TargetProject(string organizationName, string repositoryName, string solutionFileLocation, string bugLabel = "bug")
        {
            OrganizationName = organizationName;
            RepositoryName = repositoryName;
            SolutionFileLocation = solutionFileLocation;
            BugLabel = bugLabel;
        }

        public static TargetProject[] GetTargetProjects()
        {
            return new[]
            {
                new TargetProject("dotnet", "cli", "Microsoft.DotNet.Cli.sln"),
                new TargetProject("dotnet", "roslyn", "Roslyn.sln"),
                new TargetProject("dotnet", "machinelearning", "Microsoft.ML.sln"),
                new TargetProject("dotnet", "reactive", @"Rx.NET\Source\System.Reactive.sln"),
                new TargetProject("jellyfin", "jellyfin", "Jellyfin.sln"),
                new TargetProject("akkadotnet", "akka.net", @"src\Akka.sln", "confirmed bug"),
                new TargetProject("JetBrains", "resharper-unity", @"resharper\resharper-unity.sln"),
                new TargetProject("aspnet", "AspNetCore", @"src\Mvc\Mvc.sln"),
                new TargetProject("aspnet", "EntityFrameworkCore", "EFCore.sln", "type-bug"),
                new TargetProject("OpenRA", "OpenRA", "OpenRA.sln"),
                new TargetProject("Humanizr", "Humanizer", "src/Humanizer.All.sln"),
                new TargetProject("shadowsocks", "shadowsocks-windows", "shadowsocks-windows.sln"),
                new TargetProject("0xd4d", "dnSpy", "dnSpy.sln"),

                new TargetProject("IdentityServer", "IdentityServer4", @"src\IdentityServer4.Core.sln"),
                new TargetProject("icsharpcode", "ILSpy", "ILSpy.sln"),
                new TargetProject("Info Support", "knowNow", @"10-Orchard\src\Orchard.sln"),
            };
        }
    }
}