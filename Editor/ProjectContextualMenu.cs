using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public class ProjectContextualMenu : EditorWindow
{
    static string PATH = Environment.GetEnvironmentVariable("PATH");
    
    private string branchName = "";
    private static PackageManifest packageJson;
    private static bool cloned = false;
    private static ProjectContextualMenu window;
    
    [MenuItem("Assets/Edit Package")]
    public static void EditPackage()
    {
        Object[] selectedAsset = Selection.GetFiltered (typeof(Object), SelectionMode.TopLevel);
        var packageName = "";
        foreach(Object obj in selectedAsset)
        {
            packageName = obj.name;
        }

        if (packageName == "")
        {
            Debug.Log("No Package is Selected") ;
            return;
        }

        packageJson = ReadPackageManifest(packageName);

        ClonePackage(packageJson);
        window = (ProjectContextualMenu)EditorWindow.GetWindow(typeof(ProjectContextualMenu));
        window.Show();
        

    }
    
    void OnGUI()
    {
        if (cloned)
        {
            branchName = EditorGUILayout.TextField("Choose a branch name: ", branchName);

            if (GUILayout.Button("Create new branch"))
            {
                Debug.Log("Branch : " + branchName);
                CheckoutRevision();
            }

            if (GUILayout.Button("Abort"))
            {
                cloned = false;
                window.Close();
            }
        }
    }

    static bool UseEmbeddedPackageVersion()
    {
        var packageSourceDir =
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar +
                                      "PackageSource");
        var addQuickSearchRequest = UnityEditor.PackageManager.Client.Add( "file:../PackageSource/" + packageJson.name);
        if (!WaitForRequest(addQuickSearchRequest, $"Installing {packageJson.name}..."))
            Debug.LogError($"Failed to install {packageJson.name}");
        return true;
    }
    
    private static bool WaitForRequest<T>(UnityEditor.PackageManager.Requests.Request<T> request, string msg, int loopDelay = 20)
    {
        var progress = 0.0f;
        while (!request.IsCompleted)
        {
            Thread.Sleep(loopDelay);
            EditorUtility.DisplayProgressBar("Unity Package Manager", msg, Mathf.Min(1.0f, progress++ / 100f));
        }
        EditorUtility.ClearProgressBar();

        return request.Status == UnityEditor.PackageManager.StatusCode.Success && request.Result != null;
    }

    bool CheckoutRevision()
    {
        
        var packageSourceDir =
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar +
                                      "PackageSource");
        Directory.SetCurrentDirectory(packageSourceDir.ToString() + Path.DirectorySeparatorChar + packageJson.name);
        ProcessStartInfo start = new ProcessStartInfo();
        start.EnvironmentVariables["PATH"] = "/usr/local/bin:" + PATH;
        start.FileName = @"git";
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        var sshURL = packageJson.repository.url.Replace("https://", "git@").Replace(".com/", ".com:");
        
        start.Arguments = "checkout -b "+branchName+ " "+packageJson.repository.revision;

        using (Process process = Process.Start(start))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string result = reader.ReadToEnd();
                Console.Write(result);

            }
            Debug.Log("Successfully clone package");
            cloned = false;
            window.Close();
        }
        UseEmbeddedPackageVersion();
        return true;
    }
    

    static bool ClonePackage(PackageManifest packageJson)
    {

        ProcessStartInfo start = new ProcessStartInfo();
        start.EnvironmentVariables["PATH"] = "/usr/local/bin:" + PATH;
        start.FileName = @"git";
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        var sshURL = packageJson.repository.url.Replace("https://", "git@").Replace(".com/", ".com:");
        var packageSourceDir =
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar +
                                                "PackageSource");
        var cloneDir = packageSourceDir.ToString() + Path.DirectorySeparatorChar + packageJson.name;
        start.Arguments = "clone --recursive " + sshURL + " " + cloneDir;

        using (Process process = Process.Start(start))
        {
            using (StreamReader reader = process.StandardError)
            {
                string result = reader.ReadToEnd();
                Console.Write(result);
                if (!Directory.EnumerateFileSystemEntries(cloneDir).Any())
                {
                    Debug.Log("Did not successfully clone package : " + result);
                    return false;
                }
            }
            Debug.Log("Successfully clone package");
            cloned = true;
        }
        return true;
    }
    
   
 

    static PackageManifest ReadPackageManifest(string packageName)
    {
        var pathToPackage = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Packages" +
                            Path.DirectorySeparatorChar + packageName;

        var path = pathToPackage + Path.DirectorySeparatorChar + "package.json";
        var jsonString = File.ReadAllText(path);
        var packageJson = JsonUtility.FromJson<PackageManifest>(jsonString);
        return packageJson;
    }
}

[Serializable]
public class PackageManifest{
    public string name;
    public string displayName;
    public Repository repository;
}

[Serializable]
public class Repository
{
    public string footprint;
    public string type;
    public string url;
    public string revision;
    
}

