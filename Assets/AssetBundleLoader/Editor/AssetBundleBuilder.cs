/**
 *  Author ZhanQI
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace E.Editor
{
#if UNITY_EDITOR
    public class AssetBundleBuilder
    {
        /// <summary>
        /// Build asset bundles
        /// </summary>
        [MenuItem("Assets/Bundle/Build")]
        public static void Build()
        {
            if (ResetAllAssetBundleNames())
            {
                BuildAssetBundles();
            }
        }

        /// <summary>
        /// Clears the asset bundle name of the file under the selected file or folder
        /// </summary>
        [MenuItem("Assets/Bundle/Clear Seleted BundleNames")]
        public static void ClearSeletedBundleNames()
        {
            string[] selectedGUIDs = Selection.assetGUIDs;
            if(selectedGUIDs.Length > 0)
            {
                foreach(string guid in selectedGUIDs)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AssetImporter.GetAtPath(path).assetBundleName = null;
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        string[] GUIDs = AssetDatabase.FindAssets("t:Object", new string[] { path });
                        foreach (string GUID in GUIDs)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(GUID);
                            AssetImporter.GetAtPath(assetPath).assetBundleName = null;
                        }
                    }
                    Debug.Log("Clear asset bundle names " + path);
                }
            }
            AssetDatabase.RemoveUnusedAssetBundleNames();
            Debug.Log("Clear asset bundle names complete");
        }

        /// <summary>
        /// Reset the asset bundle name of the file under the selected file or folder
        /// </summary>
        [MenuItem("Assets/Bundle/Reset Seleted BundleNames")]
        public static void ResetSeletedBundleNames()
        {
            string[] selectedGUIDs = Selection.assetGUIDs;
            foreach (string guid in selectedGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                {
                    AssetImporter.GetAtPath(path).assetBundleName = null;
                    ResetBundleNames(new string[] { path });
                }
                else
                {
                    ResetBundleName(path);
                }
                Debug.Log("Reset asset bundle names " + path);
            }
            AssetDatabase.RemoveUnusedAssetBundleNames();
            Debug.Log("Reset asset bundle names complete");
        }

        private static bool ResetAllAssetBundleNames()
        {
            string[] folders = AssetBundleSettings.Instance.GetResourcesFolders();
            if(folders.Length > 0)
            {
                string[] folders0 = new string[folders.Length];
                for(int i = 0; i < folders.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(folders[i]))
                    {
                        Debug.LogError("AssetBundleSettings: source folder setting cannot be empty");
                        return false;
                    }
                    folders0[i] = Path.Combine("Assets", folders[i]);
                }
                Dictionary<string, bool> includedName = new Dictionary<string, bool>();
                StripFilesName(includedName);
                ResetBundleNames(folders0);
                AssetDatabase.RemoveUnusedAssetBundleNames();
                string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
                foreach(string eachName in allBundleNames)
                {
                    includedName[eachName] = true;
                }
                DeleteUseless(includedName);
            }
            else
            {
                return false;
            }
            return true;
        }

        private static readonly Regex ResourcesRegex = new Regex(@"(?:.+[\\/]){0,1}Resources(?:[\\/].+){0,1}");
        private static readonly Regex EditorRegex = new Regex(@"(?:.+[\\/]){0,1}Editor(?:[\\/].+){0,1}");
        private static readonly Regex UnityEditorNameSpaceRegex = new Regex(@"^(?:UnityEditor)(?:\s*\..*){0,1}");

        private static void ResetBundleNames(string[] folders)
        {
            string[] GUIDs = AssetDatabase.FindAssets("t:Object", folders);
            foreach (string GUID in GUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(GUID);
                ResetBundleName(assetPath);
            }
        }

        private static void ResetBundleName(string assetPath)
        {
            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
            if (!ResourcesRegex.IsMatch(assetPath) &&
                !EditorRegex.IsMatch(assetPath) &&
                (!UnityEditorNameSpaceRegex.IsMatch(assetType.Namespace) ||
                assetType.FullName.Equals("UnityEditor.SceneAsset")))
            {
                string bundleName = AssetBundlePath.FileToBundleName(assetPath.Remove(0, "Assets/".Length));
                if (!assetImporter.assetBundleName.Equals(bundleName))
                {
                    assetImporter.assetBundleName = bundleName;
                }
            }
            else
            {
                assetImporter.assetBundleName = null;
            }
        }

        private static void StripFilesName(Dictionary<string, bool> includedName)
        {
            string oldPathPre = AssetBundleSettings.Instance.GetTargetURI();
            if (Directory.Exists(oldPathPre))
            {
                string[] oldPaths = Directory.GetFiles(oldPathPre, "*" + AssetBundlePath.Extension);
                foreach (string oldPath in oldPaths)
                {
                    string stripOldName = oldPath.Remove(0, oldPathPre.Length + 1);
                    includedName[stripOldName] = false;
                }
            }
        }

        private static void DeleteUseless(Dictionary<string, bool> includedName)
        {
            foreach (KeyValuePair<string, bool> kv in includedName)
            {
                string name = kv.Key;
                bool exists = kv.Value;
                if (!exists)
                {
                    DeleteFile(name);
                }
            }
            void DeleteFile(string name)
            {
                string pre = AssetBundleSettings.Instance.GetTargetURI();
                string[] paths = new string[]
                {
                Path.Combine(pre, name),
                Path.Combine(pre, name + ".manifest"),
                Path.Combine(pre, name + ".meta"),
                Path.Combine(pre, name, ".manifest.meta"),
                };
                foreach (string path in paths)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }
        }

        private static void BuildAssetBundles()
        {
            string outputPath = AssetBundleSettings.Instance.GetTargetURI();
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            BuildPipeline.BuildAssetBundles(
            outputPath,
            (BuildAssetBundleOptions)AssetBundleSettings.Instance.compressed | BuildAssetBundleOptions.DeterministicAssetBundle,
            AssetBundleSettings.Instance.buildTarget);
            Debug.Log("Asset bundle build complete");
        }
    }
#endif
}
