using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace GitMyPackage
{
    
    public static class EditorUtils
    {
        private static string _folderName;
        private static readonly string PackageSourceDir =
            Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "PackageSource";

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
        
        public static string GetEmbeddedPackagePath(PackageManifest packageJson)
        {
            _folderName = packageJson.repository.url.Split('/').Last().Replace(".git","");
            return PackageSourceDir + Path.DirectorySeparatorChar + _folderName;
        }
        
    }
}
