using System;
using System.Diagnostics;

namespace CodeQualityAnalyzer
{
    public class ShellService
    {
        private readonly string _directory;

        public ShellService(string directory)
        {
            _directory = directory;
        }

        private string RunCommand(string processName, string arguments)
        {
            Process process = new Process();

            ProcessStartInfo startInfo = new ProcessStartInfo(processName)
            {
                WorkingDirectory = _directory,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
            };

            process.StartInfo = startInfo;
            //process.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
            //process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);

            process.Start();

            process.BeginErrorReadLine();
            //string err = process.StandardError.ReadToEnd();
            string err = "";
            process.ErrorDataReceived += (s, e) => err+= e.Data;


            if (err != "") throw new Exception(err);
            string output = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            return output;
        }

        public void CheckoutCommit(string hash = "HEAD")
        {
            RunCommand(
                "git",
                $"reset --hard {hash}"
            );  
        }

        public string CheckoutFileForCommit(string file, string hash = "HEAD")
        {
            return RunCommand(
                "git",
                $"checkout {hash} -- {file}"
            );
        }

        public string GetHeadHash()
        {
            return RunCommand(
                "git",
                " log --pretty=format:%h -n 1"
            );
        }

        public string GetPatchData(string hash)
        {
            return RunCommand(
                "git",
                $" show {hash}"
            );
        }


        public string GetParentHash(string hash)
        {
            return RunCommand(
                "git",
                $" log --pretty=%P -n 1 {hash}"
            );
        }
    }
}
