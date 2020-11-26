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
                DeleteUselessOutputFiles();
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
                ResetBundleNames(folders0);
                AssetDatabase.RemoveUnusedAssetBundleNames();
                return true;
            }
            else
            {
                return false;
            }
        }

        private static readonly Regex ResourcesRegex = new Regex(@"(?:.+[\\/]){0,1}Resources(?:[\\/].+){0,1}");
        //private static readonly Regex EditorRegex = new Regex(@"(?:.+[\\/]){0,1}Editor(?:[\\/].+){0,1}");
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
                //!EditorRegex.IsMatch(assetPath) &&
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

        private static void DeleteUselessOutputFiles()
        {
            Dictionary<string, bool> includedName = new Dictionary<string, bool>();
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
            string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
            foreach (string eachName in allBundleNames)
            {
                includedName[eachName] = true;
            }
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

        [MenuItem("Assets/Bundle/CollectDependencies")]
        public static void CollectDependencies()
        {
            ResetAllAssetBundleNames();
            string[] abNames = AssetDatabase.GetAllAssetBundleNames();
            Dictionary<UnityEngine.Object, int> usedCounts = new Dictionary<UnityEngine.Object, int>();
            foreach (string abName in abNames)
            {
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(abName);
                Queue<UnityEngine.Object> queue = new Queue<UnityEngine.Object>();
                foreach(string assetPath in assetPaths)
                {
                    Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                    queue.Enqueue(AssetDatabase.LoadAssetAtPath(assetPath, assetType));
                }
                UnityEngine.Object[] collects = CollectDependencies(queue.ToArray());
                foreach(UnityEngine.Object collect in collects)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(collect, out string guid, out long localid))
                    {
                        //Debug.Log(collect.name + ", "+ collect.GetType().Name + ", " + AssetDatabase.GUIDToAssetPath(guid) + ", " + localid);
                        if (usedCounts.TryGetValue(collect, out int count))
                        {
                            usedCounts[collect] = ++count;
                        }
                        else
                        {
                            usedCounts[collect] = 1;
                        }
                    }
                }
            }
            //剔除计数小于等于1的，剔除标记在包内的
            string[] folders = AssetBundleSettings.Instance.GetResourcesFolders();
            foreach (KeyValuePair<UnityEngine.Object, int> kv in usedCounts)
            {
                UnityEngine.Object asset = kv.Key;
                string assetPath = AssetDatabase.GetAssetPath(asset);
                bool inAssetbundle = false;
                for(int i = 0; i < folders.Length; i++)
                {
                    //比照路径
                    if (assetPath.Contains(folders[i]))
                    {
                        inAssetbundle = true;
                        break;
                    }
                }
                if(!inAssetbundle && kv.Value > 1)
                {
                    Debug.Log(kv.Key.name + " " + kv.Key.GetType().Name + " " + kv.Value);
                    if (assetPath.StartsWith("Assets"))
                    {
                        AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
                        assetImporter.assetBundleName = AssetBundlePath.FileToBundleName(assetPath.Remove(0, "Assets/".Length));
                    }
                    else
                    {
                        //TODO SaveAsset
                    }
                }
            }
            //剩下的项目资源标记assetbundleName，保存内部资源asset，修改对内部资源的引用然后标记assetbundleName
            //打包
        }

        private const string unity_builtin_extra_guid = "0000000000000000f000000000000000";
        private const string unity_default_resources_guid = "0000000000000000e000000000000000";

        private static UnityEngine.Object[] CollectDependencies(UnityEngine.Object[] oris)
        {
            Queue<UnityEngine.Object> queue = new Queue<UnityEngine.Object>();
            UnityEngine.Object[] selObjs = EditorUtility.CollectDependencies(oris);
            Dictionary<string, bool> guids = new Dictionary<string, bool>();
            foreach (UnityEngine.Object selObj in selObjs)
            {
                Type selObjType = selObj.GetType();
                if (!(selObj is Component) && !UnityEditorNameSpaceRegex.IsMatch(selObjType.Namespace))
                {
                    if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(selObj, out string guid, out long localid))
                    {
                        if(guid.Equals(unity_builtin_extra_guid) || guid.Equals(unity_default_resources_guid))
                        {
                            queue.Enqueue(selObj);
                        }
                        else
                        {
                            if (!guids.ContainsKey(guid))
                            {
                                UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                                queue.Enqueue(mainAsset);
                                guids.Add(guid, true);
                            }
                        }
                    }
                }
            }
            return queue.ToArray();
        }

        

    }
#endif
}
