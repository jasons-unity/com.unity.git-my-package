using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

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
            var packageName = GetPackageName();
            if (packageName == "") return;
            _packageJson = ReadPackageManifest(packageName);
            
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

            var relativePath = GetEmbeddedPackagePathFromManifest(packageName);
            if (relativePath == "") return;
            var pathToPackage = Directory.GetCurrentDirectory() + relativePath;
            _window = GetWindow<GitMyPackageWindow>();
            _window.commit = true;
            _window.pathToPackage = pathToPackage;
            _window.Show();
        }

        // Get the file ref from the project manifest.json
        private static string GetEmbeddedPackagePathFromManifest(string packageName)
        {
            var line = File.ReadLines(
                    Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + 
                    "Packages"+ Path.DirectorySeparatorChar +"manifest.json"
                    ).FirstOrDefault(l => l.Contains(packageName));

            if (line == null)
            {
                Debug.LogError("Dev Package is not yet file: referenced in manifest");
                return "";
            }
            
            var value =  line.Split(':')[2];
            return  value.Replace("\"", "").Replace(",","")
                .Remove(0,2);
            
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
                Debug.LogError(
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

        public static void RemoveCheckout(PackageManifest packageJson)
        {
            //TODO: Delete package automatically
            Debug.LogError("Could not cleanup clone.  Please manually delete from PackageSource folder in your project.");
        }
    }
}