using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;

namespace CodeQualityAnalyzer
{
    static class HelperFunctions
    {
        public static List<int> GetAffectedLines(string[] patchData)
        {
            //string[] patch = patchData.Split(new[] { "\n" }, StringSplitOptions.None);
            const string patchInfoPattern = @"@@.*@@";
            const string digitsPattern = @"\d+";
            var patchInfo = Regex.Match(patchData[0], patchInfoPattern);
            var lineInfo = Regex.Matches(patchInfo.Value, digitsPattern);

            int startLineBefore = int.Parse(lineInfo[0].Value);
            int lineNumberBefore = startLineBefore - 1;

            HashSet<int> affectedLines = new HashSet<int>();
            for (int i = 1; i < patchData.Length; i++)
            {
                if (patchData[i] == $"\\ No newline at end of file")
                {
                    continue;
                }
                var newPatchInfo = Regex.Match(patchData[i], patchInfoPattern);
                //Checks if the patch data contains another section indicated by a line with metadata
                if (newPatchInfo.Success)
                {
                    lineNumberBefore = int.Parse(Regex.Matches(newPatchInfo.Value, digitsPattern)[0].Value) - 1;
                    continue;
                }

                string type = "nothing";
                if (patchData[i] == "" ) continue;
                if (patchData[i][0] == '+') type = "addition";
                else if (patchData[i][0] == '-') type = "deletion";

                // Skip counting this iteration if the line is an addition
                if (type != "addition")
                {
                    lineNumberBefore++;
                }
                // Add the current pointer to our hashset, only if nothing happens in the line, we skip the line
                if (type != "nothing")
                {
                    affectedLines.Add(lineNumberBefore);
                }
            }

            return affectedLines.ToList();
        }

        public static List<FaultyFile> GetFaultyFiles(List<FaultyFileData> faultyFilesData)
        {
            var qualifiedFaultyFilesData = faultyFilesData
                    // Filter everything but .cs files
                    .Where(x => x.Filename.EndsWith(".cs"))
                    // Filter all the files that have test in their name or in a test folder
                    .Where(x => !x.Filename.ToLower().Contains("test"))
                    .ToList()
                ;

            if (qualifiedFaultyFilesData.Count == 0) return null;
            //var issueId = int.Parse(Regex.Match(CommitInfo.Message, @"#\d+").Value.Substring(1));

            List<FaultyFile> faultyFiles = qualifiedFaultyFilesData
                .Where(x => x.Patch != null && x.Status == "modified")
                .Select(x => new FaultyFile(x.Filename, HelperFunctions.GetAffectedLines(x.Patch.Split(new[] { "\n" }, StringSplitOptions.None))))
                .ToList();

            // If there are two parents, the commit is a merge commit. The first parent is the master branch, where the second parent is the actual fix.
            // We want to run our metrics on the master branch, not the version with the fix. Therefore, we take the first parent. 
            return faultyFiles;
        }
    }
}
