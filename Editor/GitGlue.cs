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
                    Directory.Delete(
                        Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "PackageSource" +
                        Path.DirectorySeparatorChar + _folderName, true);
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

            Directory.SetCurrentDirectory(PackageSourceDir + Path.DirectorySeparatorChar + _folderName);

            gitProcess.Arguments = "checkout -b " + branchName + " " + packageJson.repository.revision;

            RunGitProcess(gitProcess, packageJson);
            EditorWindow.GetWindow(typeof(ProjectContextualMenu)).Close();

            return true;
        }


        public static bool ClonePackage(PackageManifest packageJson)
        {

            var gitProcess = GetGitProcess();
            Directory.CreateDirectory(PackageSourceDir);
            Directory.SetCurrentDirectory(PackageSourceDir);


            var sshUrl = packageJson.repository.url.Replace("https://", "git@").Replace(".com/", ".com:");
            gitProcess.Arguments = "clone --recursive " + sshUrl;

            RunGitProcess(gitProcess, packageJson);

            return true;
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

        private static void RunGitProcess(ProcessStartInfo gitProcess, PackageManifest packageJson)
        {
            using (var process = Process.Start(gitProcess))
            {
                if (process != null)
                    using (var reader = process.StandardError)
                    {
                        var result = reader.ReadToEnd();
                        Console.Write(result);

                        _folderName = packageJson.repository.url.Split('/').Last().Replace(".git","");
                        
                        if (!Directory
                            .EnumerateFileSystemEntries(PackageSourceDir + Path.DirectorySeparatorChar +
                                                        _folderName).Any())
                        {
                            Debug.LogError("Did not successfully clone package : " + result);
                        }
                    }

                Debug.Log("Successfully clone package");
                Directory.SetCurrentDirectory(CurrentDir);
            }
        }
    }
}