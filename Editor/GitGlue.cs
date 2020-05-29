using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using Debug = UnityEngine.Debug;

namespace GitMyPackage
{

    public static class GitGlue
    {
        private static readonly string PackageSourceDir =
            Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "PackageSource";
        private static readonly string CurrentDir = Directory.GetCurrentDirectory();
        private static string _folderName;

        public static void EmbedPackage(PackageManifest packageJson)
        {
            var addPackageRequest = Client.Add("file:../PackageSource/" + _folderName);
            while (!addPackageRequest.IsCompleted)
                System.Threading.Thread.Sleep(10);

            if (addPackageRequest.Status == StatusCode.Failure)
            {
                Debug.LogError("Could not Embed the package : " + _folderName + " because : " +
                               addPackageRequest.Error.message);
                try
                {
                    Directory.Delete(GetEmbeddedPackagePath(packageJson), true);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);

                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        public static bool CheckoutRevision(PackageManifest packageJson, string branchName)
        {
            var gitProcess = GetGitProcess();
            Directory.SetCurrentDirectory(GetEmbeddedPackagePath(packageJson));
            gitProcess.Arguments = "checkout -b " + branchName + " " + packageJson.repository.revision;
            var result = RunGitProcess(gitProcess);
            
            return result != "";
        }


        public static bool ClonePackage(PackageManifest packageJson)
        {

            var gitProcess = GetGitProcess();
            Directory.CreateDirectory(PackageSourceDir);
            Directory.SetCurrentDirectory(PackageSourceDir);

            var sshUrl = packageJson.repository.url.Replace("https://", "git@").Replace(".com/", ".com:");
            gitProcess.Arguments = "clone --recursive " + sshUrl;
            
            var result = RunGitProcess(gitProcess);
            if (Directory.EnumerateFileSystemEntries(GetEmbeddedPackagePath(packageJson)).Any()) return true;
            Debug.LogError("Did not successfully clone package : " + result);
            return false;
        }

        private static ProcessStartInfo GetGitProcess()
        {
            var gitProcess = new ProcessStartInfo();
            gitProcess.EnvironmentVariables["PATH"] = "/usr/local/bin:" + Environment.GetEnvironmentVariable("PATH");
            gitProcess.FileName = @"git";
            gitProcess.UseShellExecute = false;
            gitProcess.RedirectStandardOutput = true;
            gitProcess.RedirectStandardError = true;

            return gitProcess;
        }

        private static string GetEmbeddedPackagePath(PackageManifest packageJson)
        {
            _folderName = packageJson.repository.url.Split('/').Last().Replace(".git","");
            return PackageSourceDir + Path.DirectorySeparatorChar + _folderName;
        }

        private static string RunGitProcess(ProcessStartInfo gitProcess)
        {
            var result = "";
            using (var process = Process.Start(gitProcess))
            {
                if (process != null)
                {
                    using (var reader = process.StandardError)
                    {
                        result = reader.ReadToEnd();

                    }
                    if (process.ExitCode != 0)
                    {
                        Debug.Log(process.StandardError.CurrentEncoding);

                    }
                }
                Directory.SetCurrentDirectory(CurrentDir);
                return result;
            }
        }

        public static bool CommitChange(string comment, string pathToPackage)
        {
            var gitProcess = GetGitProcess();

            Directory.SetCurrentDirectory(pathToPackage);

            gitProcess.Arguments = "add . ";
            var addStatus = RunGitProcess(gitProcess);
            if (addStatus == "") return false;
            
            gitProcess.Arguments = $"commit -m \"{comment}\"";
            var commitStatus = RunGitProcess(gitProcess);
            if (commitStatus == "") return false;
            
            gitProcess.Arguments = $"push";
            var pushStatus = RunGitProcess(gitProcess);

            return pushStatus != "";

        }

        public static bool PushBranch(PackageManifest packageJson, string branchName)
        {
            var gitProcess = GetGitProcess();
            Directory.SetCurrentDirectory(GetEmbeddedPackagePath(packageJson));
            
            gitProcess.Arguments = $"push -u origin {branchName}";

            var addStatus = RunGitProcess(gitProcess);

            return addStatus != "";
        }
    }
}