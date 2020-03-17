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
    private static readonly string Path = Environment.GetEnvironmentVariable("PATH");
    
    private string _branchName = "";
    private static PackageManifest _packageJson;
    private static bool _cloned = false;
    private static ProjectContextualMenu _window;
    
    [MenuItem("Assets/Edit Package")]
    public static void EditPackage()
    {
        var selectedAsset = Selection.GetFiltered (typeof(Object), SelectionMode.TopLevel);
        var packageName = "";
        foreach(var obj in selectedAsset)
        {
            packageName = obj.name;
        }

        if (packageName == "")
        {
            Debug.Log("No Package is Selected") ;
            return;
        }

        _packageJson = ReadPackageManifest(packageName);

        ClonePackage(_packageJson);
        _window = (ProjectContextualMenu)EditorWindow.GetWindow(typeof(ProjectContextualMenu));
        _window.Show();
        

    }

    private void OnGUI()
    {
        if (!_cloned) return;
        _branchName = EditorGUILayout.TextField("Choose a branch name: ", _branchName);

        if (GUILayout.Button("Create new branch"))
        {
            Debug.Log("Branch : " + _branchName);
            CheckoutRevision();
        }

        if (!GUILayout.Button("Abort")) return;
        _cloned = false;
        _window.Close();
    }

    private static bool UseEmbeddedPackageVersion()
    {
        var packageSourceDir = Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar + "PackageSource";
        var addQuickSearchRequest = UnityEditor.PackageManager.Client.Add( "file:../PackageSource/" + _packageJson.name);
        while (!addQuickSearchRequest.IsCompleted)
            System.Threading.Thread.Sleep(10);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
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

    private bool CheckoutRevision()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var packageSourceDir =Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar + "PackageSource";
        Directory.SetCurrentDirectory(packageSourceDir.ToString() + System.IO.Path.DirectorySeparatorChar + _packageJson.name);
        var start = new ProcessStartInfo();
        start.EnvironmentVariables["PATH"] = "/usr/local/bin:" + Path;
        start.FileName = @"git";
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        var sshUrl = _packageJson.repository.url.Replace("https://", "git@").Replace(".com/", ".com:");
        
        start.Arguments = "checkout -b "+_branchName+ " "+_packageJson.repository.revision;

        using (var process = Process.Start(start))
        {
            if (process != null)
                using (var reader = process.StandardOutput)
                {
                    var result = reader.ReadToEnd();
                    Console.Write(result);
                }

            Debug.Log("Successfully clone package");
            Directory.SetCurrentDirectory(currentDir);
            _cloned = false;
            _window.Close();
        }
        UseEmbeddedPackageVersion();
        return true;
    }


    private static bool ClonePackage(PackageManifest packageJson)
    {

        var start = new ProcessStartInfo();
        start.EnvironmentVariables["PATH"] = "/usr/local/bin:" + Path;
        start.FileName = @"git";
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        var sshUrl = packageJson.repository.url.Replace("https://", "git@").Replace(".com/", ".com:");
        var packageSourceDir =
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar +
                                                "PackageSource");
        var cloneDir = packageSourceDir.ToString() + System.IO.Path.DirectorySeparatorChar + packageJson.name;
        start.Arguments = "clone --recursive " + sshUrl + " " + cloneDir;

        using (var process = Process.Start(start))
        {
            if (process != null)
                using (var reader = process.StandardError)
                {
                    var result = reader.ReadToEnd();
                    Console.Write(result);
                    if (!Directory.EnumerateFileSystemEntries(cloneDir).Any())
                    {
                        Debug.Log("Did not successfully clone package : " + result);
                        return false;
                    }
                }

            Debug.Log("Successfully clone package");
            _cloned = true;
        }
        return true;
    }


    private static PackageManifest ReadPackageManifest(string packageName)
    {
        var pathToPackage = Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar + "Packages" +
                            System.IO.Path.DirectorySeparatorChar + packageName;

        var path = pathToPackage + System.IO.Path.DirectorySeparatorChar + "package.json";
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

