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

                    EditorUtils.EmbedPackage(packageJson);
                    cloned = false;
                    Close();
                }

                if (GUILayout.Button("Abort"))
                {
                    ProjectContextualMenu.RemoveCheckout(packageJson);
                    cloned = false;
                    Close();
                };
            }

            if (commit)
            {
                _comment = EditorGUILayout.TextField("Comment for commit", _comment);
                if (GUILayout.Button("Commit"))
                {
                    GitGlue.CommitChange(_comment, pathToPackage);
                    GitGlue.PushChange(pathToPackage);
                    commit = false;
                    Close();
                }

                if (GUILayout.Button("Abort"))
                {
                    commit = false;
                    Close();
                };
            }
        }
    }
}