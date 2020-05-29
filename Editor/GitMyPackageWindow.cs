using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GitMyPackage
{


    public class GitMyPackageWindow : EditorWindow
    {
        public bool cloned;
        private string _branchName;
        public PackageManifest packageJson;
        public bool commit;
        private string _comment;
        public string pathToPackage;

        private void OnGUI()
        {
            if (cloned)
            {
                _branchName = EditorGUILayout.TextField("Choose a branch name: ", _branchName);

                if (GUILayout.Button("Create new branch"))
                {
                    Debug.Log("Branch : " + _branchName);
                    GitGlue.CheckoutRevision(packageJson, _branchName);

                    GitGlue.PushBranch(packageJson, _branchName);

                    GitGlue.EmbedPackage(packageJson);
                    cloned = false;
                }

                if (!GUILayout.Button("Abort")) return;

                cloned = false;
            }

            if (commit)
            {
                _comment = EditorGUILayout.TextField("Comment for commit", _comment);
                if (GUILayout.Button("Commit"))
                {
                    GitGlue.CommitChange(_comment, pathToPackage);
                    //TODO: need to push
                }
                if (!GUILayout.Button("Abort")) return;

                commit = false;
            }
            
            this.Close();
            //TODO: close window not working for commit
        }
    }
}