using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RestSharp;


namespace CodeQualityAnalyzer
{
    internal class TfsService : IIssueTrackerService
    {
        private readonly RestClient _client;
        private readonly ShellService _shellService;

        public TfsService(string repositoryFolder)
        {
            _client = new RestClient(SecretService.GetTfsSecrets().BaseUrl);
            _shellService = new ShellService(repositoryFolder);
        }

        public List<FaultyVersion> GetFaultyVersions()
        {
            var tfsSecrets = SecretService.GetTfsSecrets();

            string credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{tfsSecrets.UserName}:{tfsSecrets.AccessToken}"));

            var authHeader = new AuthenticationHeaderValue("Basic", credentials);

            var teamId = tfsSecrets.TeamId;
            var queryId = "586718e7-d6ec-48ae-bcd0-01d8a9095198";
            var request = new RestRequest($"{teamId}/_apis/wit/wiql/{queryId}?api-version=4.1");
            request.AddHeader("Authorization", authHeader.ToString());

            var response = _client.ExecuteAsync(request).Result;
            var result = JsonConvert.DeserializeObject<QueryResult>(response.Content);

            var bugWorkItems = result.workItems.Select(x => x.id);

            List<string> bugCommitHashes = new List<string>();
            foreach (int bugWorkItem in bugWorkItems)
            {
                request = new RestRequest($"_apis/wit/workitems/{bugWorkItem}?api-version=5.0");
                request.AddHeader("Authorization", authHeader.ToString());
                request.AddParameter("$expand", "relations");
                response = _client.ExecuteAsync(request).Result;
                WorkItemDetails workItemDetails = JsonConvert.DeserializeObject<WorkItemDetails>(response.Content);
                
                // Skip if there is no link in the workitem, 
                if(workItemDetails.relations is null) continue;

                foreach (var relation in workItemDetails.relations)
                {
                    if (!relation.url.Contains("Commit")) continue;
                    var hash = relation.url.Substring(98, 40);
                    bugCommitHashes.Add(hash);
                }
            }

            var faultyVersions = new List<FaultyVersion>();
            foreach (string commitHash in bugCommitHashes)
            {
                var parentHash = _shellService.GetParentHash(commitHash);
                if (parentHash == "") continue;
                parentHash = parentHash.Remove(parentHash.Length - 1);
                var patch = _shellService.GetPatchData(commitHash);
                List<FaultyFile> faultyFiles = patch.Split(new[] {"diff --git a"}, StringSplitOptions.None)
                    .Skip(1)
                    .Select(x => x.Split(new[] { "\n" }, StringSplitOptions.None))
                    .Where(x => GetPatchData(x).Length > 0 && GetFileName(x).EndsWith(".cs") && !GetFileName(x).ToLower().Contains("test"))
                    .Select(x => new FaultyFile(GetFileName(x), HelperFunctions.GetAffectedLines(GetPatchData(x))))
                    .ToList()
                    ;

                faultyVersions.Add(new FaultyVersion(parentHash, faultyFiles, commitHash));
            }

            return faultyVersions;
        }

        private static string GetFileName(string[] x)
        {
            string firstLine = x[0];
            string filename = firstLine.Split(new[] { " b/"}, StringSplitOptions.None)[0].TrimStart('/');

            return filename;
        }

        private static string[] GetPatchData(string[] dirtyPatchData)
        {
            int i = 0;
            const string patchInfoPattern = @"@@.*@@";
            while (i < dirtyPatchData.Length && !Regex.IsMatch(dirtyPatchData[i], patchInfoPattern)) i++;

            return i == dirtyPatchData.Length ? new string[] {} : dirtyPatchData.Skip(i).ToArray();
        }
    }

    class QueryResult
    {
        public List<WorkItem> workItems;
        public class WorkItem { public int id; }
    }

    class WorkItemDetails
    {
        public List<Relation> relations;
    }

    class Relation
    {
        public string rel;
        public string url;
    }
}
