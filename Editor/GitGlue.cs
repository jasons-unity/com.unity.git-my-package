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
            var gitProcess = GetGitProcess(GetEmbeddedPackagePath(packageJson));
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
            if (Directory.EnumerateFileSystemEntries(GetEmbeddedPackagePath(packageJson)).Any()) return true;
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
                    // using (var reader = process.StandardError)
                    // {
                    //     result = reader.ReadToEnd();
                    //
                    // }
                    string output = process.StandardOutput.ReadToEnd();
                    Console.WriteLine(output);
                    string err = process.StandardError.ReadToEnd();

                    process.WaitForExit();
                }
                

                
                //Directory.SetCurrentDirectory(CurrentDir);
                return result;
            }
        }

        public static bool CommitChange(string comment, string pathToPackage)
        {
            var gitProcess = GetGitProcess(pathToPackage);

            gitProcess.Arguments = "add . ";
            var addStatus = RunGitProcess(gitProcess);
            //if (addStatus == "") return false;
            
            gitProcess.Arguments = $"commit -m \"{comment}\"";
            var commitStatus = RunGitProcess(gitProcess);

            //return commitStatus != "";
            return true;

        }

        public static bool PushBranch(PackageManifest packageJson, string branchName)
        {
            var gitProcess = GetGitProcess(GetEmbeddedPackagePath(packageJson));

            gitProcess.Arguments = $"push -u origin {branchName}";

            var addStatus = RunGitProcess(gitProcess);

            return addStatus != "";
        }
        
        public static bool PushChange(string embeddedPackagePath)
        {
            var gitProcess = GetGitProcess(embeddedPackagePath);

            gitProcess.Arguments = $"push";

            var addStatus = RunGitProcess(gitProcess);

            return addStatus != "";
        }
    }
}