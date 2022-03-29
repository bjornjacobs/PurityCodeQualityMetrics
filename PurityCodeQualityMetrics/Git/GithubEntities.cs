using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CodeQualityAnalyzer
{
    class Issue
    {
        [JsonProperty("number")]
        public int Number { get; set; }
    }

    class FaultyFileData
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("patch")]
        public string Patch { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
    class Commit
    {
        [JsonProperty("sha")]
        public string Hash { get; set; }

        [JsonProperty("commit")]
        public CommitInfo CommitInfo { get; set; }

        [JsonProperty("Parents")]
        public List<Parent> Parents { get; set; }
    }

    class CommitInfo
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    class Parent
    {
        [JsonProperty("sha")]
        public string Hash { get; set; }
    }

    class BugFixCommit
    {
        [JsonProperty("sha")]
        public string Hash { get; set; }

        [JsonProperty("parents")]
        public List<Parent> Parents { get; set; }

        [JsonProperty("commit")]
        public CommitInfo CommitInfo { get; set; }

        [JsonProperty("files")]
        public List<FaultyFileData> FaultyFilesData { get; set; }

        public FaultyVersion GetFaultyVersion()
        {

            List<FaultyFile> faultyFiles = HelperFunctions.GetFaultyFiles(FaultyFilesData);
            if (faultyFiles is null) return null;
            // If there are two parents, the commit is a merge commit. The first parent is the master branch, where the second parent is the actual fix.
            // We want to run our metrics on the master branch, not the version with the fix. Therefore, we take the first parent. 
            return new FaultyVersion(Parents.First().Hash, faultyFiles, Hash);
        }
    }

}
