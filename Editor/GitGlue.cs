using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace GitMyPackage
{
    /// <summary>
    /// Class GitGlue provides the ability to interact with the Git Process
    /// </summary>
    public static class GitGlue
    {
        private static readonly string PackageSourceDir =
            Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "PackageSource";

        public static bool CheckoutRevision(PackageManifest packageJson, string branchName)
        {
            var gitProcess = GetGitProcess(EditorUtils.GetEmbeddedPackagePath(packageJson));
            gitProcess.Arguments = "checkout -b " + branchName + " " + packageJson.repository.revision;
            var result = RunGitProcess(gitProcess);
            
            return result != "";
        }

        public static bool ClonePackage(PackageManifest packageJson)
        {
            
            Directory.CreateDirectory(PackageSourceDir);
            var gitProcess = GetGitProcess(PackageSourceDir);

            var sshUrl = packageJson.repository.url.Replace("https://", "git@").Replace(".com/", ".com:");
            gitProcess.Arguments = "clone --recursive " + sshUrl;
            
            var result = RunGitProcess(gitProcess);
            if (Directory.EnumerateFileSystemEntries(EditorUtils.GetEmbeddedPackagePath(packageJson)).Any()) return true;
            Debug.LogError("Did not successfully clone package : " + result);
            return false;
        }

        private static ProcessStartInfo GetGitProcess(string workingDir)
        {
            var gitProcess = new ProcessStartInfo();
            gitProcess.EnvironmentVariables["PATH"] = "/usr/local/bin:" + Environment.GetEnvironmentVariable("PATH");
            gitProcess.FileName = @"git";
            gitProcess.UseShellExecute = false;
            gitProcess.RedirectStandardOutput = true;
            gitProcess.RedirectStandardError = true;
            gitProcess.WorkingDirectory = workingDir;

            return gitProcess;
        }
        
        private static string RunGitProcess(ProcessStartInfo gitProcess)
        {
            var result = "";
            using (var process = Process.Start(gitProcess))
            {
                if (process != null)
                {
                    //string output = process.StandardOutput.ReadToEnd();
                    result = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                }
                return result;
            }
        }

        public static bool CommitChange(string comment, string pathToPackage)
        {
            var gitProcess = GetGitProcess(pathToPackage);

            gitProcess.Arguments = "add . ";
            var addStatus = RunGitProcess(gitProcess);
            if (addStatus != "") return false;
            
            gitProcess.Arguments = $"commit -m \"{comment}\"";
            var commitStatus = RunGitProcess(gitProcess);

            return commitStatus != "";
        }

        public static bool PushBranch(PackageManifest packageJson, string branchName)
        {
            var gitProcess = GetGitProcess(EditorUtils.GetEmbeddedPackagePath(packageJson));
            gitProcess.Arguments = $"push -u origin {branchName}";
            var addStatus = RunGitProcess(gitProcess);
            
            return addStatus == "";
        }
        
        public static bool PushChange(string embeddedPackagePath)
        {
            var gitProcess = GetGitProcess(embeddedPackagePath);
            gitProcess.Arguments = $"push";
            var addStatus = RunGitProcess(gitProcess);
            
            return addStatus == "";
        }
    }
}