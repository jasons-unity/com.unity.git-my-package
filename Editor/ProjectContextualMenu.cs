using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;

namespace GitMyPackage
{
    public class ProjectContextualMenu : EditorWindow
    {
        private static PackageManifest _packageJson;
        private static GitMyPackageWindow _window;
        private static bool _cloned;
        
        [MenuItem("Assets/GitMyPackage/Edit Package")]
        public static void EditPackage()
        {
            _packageJson = ReadPackageManifest(GetPackageName());
            
            _cloned = GitGlue.ClonePackage(_packageJson);
            _window = GetWindow<GitMyPackageWindow>();
            _window.cloned = _cloned;
            _window.packageJson = _packageJson;
            _window.Show();
        }

        [MenuItem("Assets/GitMyPackage/Commit Change")]
        public static void SetupCommit()
        {
            var packageName = GetPackageName();

            var relativePath = GetEmbeddedPackagePath(packageName);
            var pathToPackage = Directory.GetCurrentDirectory() + relativePath;
            _window = GetWindow<GitMyPackageWindow>();
            _window.commit = true;
            _window.pathToPackage = pathToPackage;
            _window.Show();
        }

        private static string GetEmbeddedPackagePath(string packageName)
        {
            var line = File.ReadLines(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Packages"+ Path.DirectorySeparatorChar +"manifest.json")
                .FirstOrDefault(l => l.Contains(packageName));
            return line.Split(char.Parse(":"))[2].Replace("\"","").Replace(",","").Remove(0,2);
        }

        private static string GetPackageName()
        {
            var selectedAsset = Selection.GetFiltered(typeof(Object), SelectionMode.TopLevel);
            var packageName = "";
            foreach (var obj in selectedAsset)
            {
                packageName = obj.name;
            }

            if (packageName == "")
            {
                Debug.Log(
                    "No Package is Selected.  Make sure to select the Packages folder in the Project Window and then select the package folder in right column of the Project Window (Two column view)");
            }
            return packageName;
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
}