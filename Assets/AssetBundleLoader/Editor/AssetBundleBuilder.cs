/**
 *  Author ZhanQI
 */

using System;
using System.Collections.Generic;
using System.IO;
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
                    if (Directory.Exists(path))
                    {
                        string[] filePaths = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, path), "*", SearchOption.AllDirectories);
                        foreach(string filePath in filePaths)
                        {
                            FileInfo fileInfo = new FileInfo(filePath);
                            if (!fileInfo.Name.EndsWith(".meta"))
                            {
                                string resoourceFilePath = fileInfo.FullName.Remove(0, Environment.CurrentDirectory.Length + 1);
                                AssetImporter assetImporter = AssetImporter.GetAtPath(resoourceFilePath);
                                assetImporter.assetBundleName = null;
                            }
                        }
                    }
                    else
                    {
                        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
                        assetImporter.assetBundleName = null;
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
            if (selectedGUIDs.Length > 0)
            {
                foreach (string guid in selectedGUIDs)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Directory.Exists(path))
                    {
                        string[] filePaths = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, path), "*", SearchOption.AllDirectories);
                        foreach (string filePath in filePaths)
                        {
                            FileInfo fileInfo = new FileInfo(filePath);
                            if (ExcludeExtend(fileInfo.Name))
                            {
                                string resoourceFilePath = fileInfo.FullName.Remove(0, Environment.CurrentDirectory.Length + 1);
                                AssetImporter assetImporter = AssetImporter.GetAtPath(resoourceFilePath);
                                string bundleName = AssetBundlePath.FileToBundleName(resoourceFilePath.Remove(0, "Assets/".Length));
                                if (!assetImporter.assetBundleName.Equals(bundleName))
                                {
                                    assetImporter.assetBundleName = bundleName;
                                }
                            }
                        }
                    }
                    else
                    {
                        string resoourceFilePath = path;
                        AssetImporter assetImporter = AssetImporter.GetAtPath(resoourceFilePath);
                        string bundleName = AssetBundlePath.FileToBundleName(resoourceFilePath.Remove(0, "Assets/".Length));
                        if (!assetImporter.assetBundleName.Equals(bundleName))
                        {
                            assetImporter.assetBundleName = bundleName;
                        }
                    }
                    Debug.Log("Reset asset bundle names " + path);
                }
            }
            AssetDatabase.RemoveUnusedAssetBundleNames();
            Debug.Log("Reset asset bundle names complete");
        }

        private static string PrePath
        {
            get
            {
                return AssetBundleSettings.Instance.GetTargetURI();
            }
        }

        private static bool ResetAllAssetBundleNames()
        {
            string[] folders = AssetBundleSettings.Instance.GetResourcesFolders();
            if(folders != null && folders.Length > 0)
            {
                //遍历已经包含的文件
                Dictionary<string, bool> includedName = new Dictionary<string, bool>();
                StripFilesName(includedName);
                foreach (string folder in folders)
                {
                    if (!string.IsNullOrWhiteSpace(folder))
                    {
                        string folder0 = Path.Combine(Application.dataPath, folder);
                        if (Directory.Exists(folder0))
                        {
                            //Reset assetBundleName
                            string[] paths = Directory.GetFiles(folder0, "*", SearchOption.AllDirectories);
                            foreach (string path in paths)
                            {
                                //TODO 改用 AssetDatabase
                                FileInfo fileInfo = new FileInfo(path);
                                if (ExcludeExtend(fileInfo.Name))
                                {
                                    string resoourceDirPath = fileInfo.DirectoryName.Remove(0, Application.dataPath.Length + 1);
                                    string resoourceFilePath0 = fileInfo.FullName.Remove(0, Application.dataPath.Length + 1);
                                    string resoourceFilePath = Path.Combine("Assets", resoourceFilePath0);
                                    string bundleName = AssetBundlePath.FormatPath(resoourceDirPath) + AssetBundlePath.Extension;
                                    //includedName[bundleName] = true;
                                    AssetImporter assetImporter = AssetImporter.GetAtPath(resoourceFilePath);
                                    if (!assetImporter.assetBundleName.Equals(bundleName))
                                    {
                                        assetImporter.assetBundleName = bundleName;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError("AssetBundleSettings: folder does not exist! " + folder0);
                            return false;
                        }
                    }
                    else
                    {
                        Debug.LogError("AssetBundleSettings: source folder setting cannot be empty");
                        return false;
                    }
                }
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

        private static readonly string[] excludeExtends = {
            ".meta", ".cs", "LightingData.asset"
        };

        private static bool ExcludeExtend(string name)
        {
            foreach(string exd in excludeExtends)
            {
                if (name.EndsWith(exd))
                {
                    return false;
                }
            }
            return true;
        }

        private static void StripFilesName(Dictionary<string, bool> includedName)
        {
            string oldPathPre = PrePath;
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
                    DeleteUseless0(name);
                }
            }
        }

        private static void DeleteUseless0(string name)
        {
            string pre = PrePath;
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

        private static void BuildAssetBundles()
        {
            string outputPathLocal = PrePath;
            if (!Directory.Exists(outputPathLocal))
            {
                Directory.CreateDirectory(outputPathLocal);
            }
            BuildPipeline.BuildAssetBundles(
            outputPathLocal,
            (BuildAssetBundleOptions)AssetBundleSettings.Instance.compressed | BuildAssetBundleOptions.DeterministicAssetBundle,
            AssetBundleSettings.Instance.buildTarget);
            Debug.Log("Asset bundle build complete");
        }
    }
#endif
}
