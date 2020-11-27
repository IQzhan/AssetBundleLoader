/**
 *  Author ZhanQI
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace E.Editor
{
#if UNITY_EDITOR
    public class AssetBundleBuilder
    {
        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            
        }

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
                            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                            if (!assetType.FullName.Equals("UnityEditor.MonoScript"))
                            {
                                AssetImporter.GetAtPath(assetPath).assetBundleName = null;
                            }
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
        private static readonly Regex StartWithAssetsRegex = new Regex(@"^(?:Assets[\\/]).*");
        private static readonly Regex MatchInclinedRegex = new Regex(@"[\\/]");

        private const string unity_builtin_extra_guid = "0000000000000000f000000000000000";
        private const string unity_default_resources_guid = "0000000000000000e000000000000000";

        private const string AssetsFolderName = "Assets";
        private const string ExtractResourcesFolderName = "_extract_resources";

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
                if (!assetType.FullName.Equals("UnityEditor.MonoScript"))
                {
                    assetImporter.assetBundleName = null;
                }
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

        /// <summary>
        /// Not complete yet
        /// </summary>
        //[MenuItem("Assets/Bundle/CollectDependenciesBuild")]
        public static void CollectDependenciesBuild()
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
                foreach (UnityEngine.Object collect in collects)
                {
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(collect, out string guid, out long localid))
                    {
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
            string extractResourcesPath = Path.Combine(AssetsFolderName, ExtractResourcesFolderName);
            string[] folders = AssetBundleSettings.Instance.GetResourcesFolders();
            List<AssetImporter> modifiedAssetImporters = new List<AssetImporter>();
            try
            {
                foreach (KeyValuePair<UnityEngine.Object, int> kv in usedCounts)
                {
                    UnityEngine.Object asset = kv.Key;
                    int useCount = kv.Value;
                    string assetPath = AssetDatabase.GetAssetPath(asset);
                    bool inAssetbundle = false;
                    for (int i = 0; i < folders.Length; i++)
                    {
                        if (MatchInclinedRegex.Replace(assetPath, @"/")
                            .Contains(MatchInclinedRegex.Replace(folders[i], @"/")))
                        {
                            inAssetbundle = true;
                            break;
                        }
                    }
                    if (!inAssetbundle && useCount > 1)
                    {
                        Type assetType = asset.GetType();
                        //Debug.Log(asset.name + " " + assetType.Name + " " + useCount);
                        if (StartWithAssetsRegex.IsMatch(assetPath))
                        {
                            AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
                            assetImporter.assetBundleName = AssetBundlePath.FileToBundleName(assetPath.Remove(0, AssetsFolderName.Length + 1));
                            modifiedAssetImporters.Add(assetImporter);
                        }
                        else
                        {
                            //Create _extract_resources folder
                            if (!AssetDatabase.IsValidFolder(extractResourcesPath))
                            {
                                AssetDatabase.CreateFolder(AssetsFolderName, ExtractResourcesFolderName);
                            }
                            string subdir = Path.Combine(extractResourcesPath, assetType.Name);
                            if (!AssetDatabase.IsValidFolder(subdir))
                            {
                                AssetDatabase.CreateFolder(extractResourcesPath, assetType.Name);
                            }
                            //Clone Asset to _extract_resources
                            string savePath = Path.Combine(subdir, MatchInclinedRegex.Replace(asset.name, "_") + ".asset");
                            UnityEngine.Object clone = UnityEngine.Object.Instantiate(asset);
                            clone.name = asset.name;
                            AssetDatabase.CreateAsset(clone, savePath);
                            AssetDatabase.SaveAssets();
                            AssetImporter assetImporter = AssetImporter.GetAtPath(savePath);
                            assetImporter.assetBundleName = AssetBundlePath.FileToBundleName(savePath.Remove(0, AssetsFolderName.Length + 1));
                            modifiedAssetImporters.Add(assetImporter);
                            //Change reference
                            //TODO
                            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clone, out string guid, out long localid))
                            {
                                
                            }
                        }
                    }
                }
                //Build asset bundle
                //DeleteUselessOutputFiles();
                //BuildAssetBundles();
                Revok();
            }
            catch (Exception anyException)
            {
                Revok();
                throw anyException;
            }
            
            void Revok()
            {
                //Revok assetBundleName
                foreach (AssetImporter modifiedAssetImporter in modifiedAssetImporters)
                {
                    modifiedAssetImporter.assetBundleName = null;
                }
                modifiedAssetImporters.Clear();
                //Delete _extract_resources folder
                //if (AssetDatabase.IsValidFolder(extractResourcesPath))
                //{
                //    AssetDatabase.DeleteAsset(extractResourcesPath);
                //}
                AssetDatabase.RemoveUnusedAssetBundleNames();
                //Revok change reference
                //TODO
            }
        }

        private static UnityEngine.Object[] CollectDependencies(UnityEngine.Object[] roots)
        {
            Queue<UnityEngine.Object> queue = new Queue<UnityEngine.Object>();
            UnityEngine.Object[] selObjs = EditorUtility.CollectDependencies(roots);
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
                                if (AssetDatabase.IsMainAsset(selObj))
                                {
                                    queue.Enqueue(selObj);
                                }
                                else
                                {
                                    UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                                    queue.Enqueue(mainAsset);
                                }
                                guids.Add(guid, true);
                            }
                        }
                    }
                }
            }
            return queue.ToArray();
        }

        private static UnityEngine.Object[] CollectDependencies(UnityEngine.Object root)
        {
            return CollectDependencies(new UnityEngine.Object[] { root });
        }

        private static void MatchCollectDependencies(UnityEngine.Object root)
        {
            //Find reference
            if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(root, out string guid, out long localid))
            {
                SerializedObject so = new SerializedObject(root);
                StringBuilder sb = new StringBuilder();
                so.Update();
                SerializedProperty sp = so.GetIterator();
                while (sp.Next(true))
                {
                    sb.AppendLine(sp.name + " " + sp.propertyType);
                    if(sp.propertyType == SerializedPropertyType.ObjectReference)
                    {

                    }
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(root);
                AssetDatabase.SaveAssets();
                Debug.Log(sb);
            }
        }

        public class ObjectDependencies
        {
            //match a string line like  {fileID: 1828408971408620874, guid: dca30aeb80de1d24485341d3b41b91ca, type: 3}
            public static readonly Regex MatchRegex = new Regex(@"\{\s*fileID\s*:\s*([0-9]+)\s*,\s*guid\s*:\s*([0-9a-f]+)\s*,\s*type\s*:\s*([0-9])\s*\}");
            public static readonly Regex MatchFileIDRegex = new Regex(@"");
            public static readonly Regex MatchGUIDRegex = new Regex(@"");
            public static readonly Regex MatchTypeRegex = new Regex(@"");
            private ObjectDependencies() { }

            private UnityEngine.Object root;

            private string filePath = string.Empty;

            private string[] lines = new string[0];

            private Queue<DependenciesInfo> infos = new Queue<DependenciesInfo>();

            public class DependenciesInfo
            {
                public int[] atLines = new int[0];

                public long fileId = 0;

                public string guid = string.Empty;

                public int type = 0;

                private UnityEngine.Object source;

                public UnityEngine.Object Source
                {
                    get
                    {
                        if(source == null)
                        {
                            source = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                            //source = AssetDatabase.LoadAssetAtPath(Path.Combine("Assets", ))
                        }
                        return source;
                    }
                }
            }

            public static bool TryGetDependenciesInfo(UnityEngine.Object root, out ObjectDependencies dep)
            {
                dep = default;
                return false;
            }
        }

        //[MenuItem("Assets/Bundle/Test")]
        private static void Test()
        {
            //string str = "  m_LightingSettings: {fileID: 4890085278179872738, guid: c33e4649090c6b548bf176fb5b085ee2, type: 2}";
            //MatchCollection matchs = ObjectDependencies.MatchRegex.Matches(str);
            //if(matchs.Count > 0)
            //{
            //    GroupCollection groups = matchs[0].Groups;
            //    long fileID = long.Parse(groups[1].Value);
            //    string guid = groups[2].Value;
            //    int type = int.Parse(groups[3].Value);
            //    Debug.Log("match: "  + fileID + " " + guid + " " + type);
            //}
            //else
            //{
            //    Debug.Log("not match");
            //}

            MatchCollectDependencies(AssetDatabase.LoadAssetAtPath("Assets/Example/Res/Prefabs/Room.prefab", typeof(UnityEngine.GameObject)));
        }


    }
#endif
}
