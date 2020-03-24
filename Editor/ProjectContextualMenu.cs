using System.IO;
using UnityEditor;
using UnityEngine;

public class ProjectContextualMenu : EditorWindow
{

    private static PackageManifest _packageJson;
    private static bool _cloned;
    private static ProjectContextualMenu _window;
    private string _branchName;
        

    [MenuItem("Assets/Edit Package")]
    public static void EditPackage()
    {
        var selectedAsset = Selection.GetFiltered(typeof(Object), SelectionMode.TopLevel);
        var packageName = "";
        foreach (var obj in selectedAsset)
        {
            packageName = obj.name;
        }

        if (packageName == "")
        {
            Debug.Log("No Package is Selected");
            return;
        }

        _packageJson = ReadPackageManifest(packageName);

        _cloned = GitGlue.ClonePackage(_packageJson);
        _window = (ProjectContextualMenu) GetWindow(typeof(ProjectContextualMenu));
        _window.Show();


    }

    private void OnGUI()
    {
        if (!_cloned) return;

        _branchName = EditorGUILayout.TextField("Choose a branch name: ", _branchName);

        if (GUILayout.Button("Create new branch"))
        {
            Debug.Log("Branch : " + _branchName);
            if (GitGlue.CheckoutRevision(_packageJson, _branchName))
            {
                GitGlue.EmbedPackage(_packageJson);
                _cloned = false;
            }
                
        }

        if (!GUILayout.Button("Abort")) return;

        _cloned = false;
        _window.Close();
    }

    private static PackageManifest ReadPackageManifest(string packageName)
    {
        var pathToPackage = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Packages" +
                            Path.DirectorySeparatorChar + packageName;

        var path = pathToPackage + Path.DirectorySeparatorChar + "package.json";
        var jsonString = File.ReadAllText(path);
        var packageJson = JsonUtility.FromJson<PackageManifest>(jsonString);
        return packageJson;
    }
}