/**
 *  Author ZhanQI
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        public class ObjectReferencesCollection
        {
            private Dictionary<UnityEngine.Object, ObjectReferences> references = new Dictionary<UnityEngine.Object, ObjectReferences>();
            
            public ObjectReferences[] GetReferences()
            {
                List<ObjectReferences> refs = new List<ObjectReferences>();
                foreach(ObjectReferences objectReferences in references.Values)
                {
                    Type objType = objectReferences.obj.GetType();
                    if (!objType.FullName.Equals("UnityEngine.Object"))
                    {
                        refs.Add(objectReferences);
                    }
                }
                Debug.Log("Total: " + refs.Count);
                return refs.ToArray();
            }

            public UnityEngine.Object[] GetReferencesObject()
            {
                List<UnityEngine.Object> refs = new List<UnityEngine.Object>();
                foreach (ObjectReferences objectReferences in references.Values)
                {
                    //Type objType = objectReferences.obj.GetType();
                    //if (!objType.FullName.Equals("UnityEngine.Object"))
                    //{
                    //    refs.Add(objectReferences.obj);
                    //}
                    refs.Add(objectReferences.obj);
                }
                Debug.Log("Total: " + refs.Count);
                return refs.ToArray();
            }

            public ObjectReferences[] GetAssetReferences()
            {
                List<ObjectReferences> refs = new List<ObjectReferences>();
                foreach (ObjectReferences objectReferences in references.Values)
                {
                    refs.Add(objectReferences);
                }
                Debug.Log("Total: " + refs.Count);
                return refs.ToArray();
            }
        }

        public class ObjectReferences
        {
            public UnityEngine.Object obj;

            public Dictionary<UnityEngine.Object, List<string>> from = new Dictionary<UnityEngine.Object, List<string>>();

            public List<int> inGrops = new List<int>();
        }

        private static ObjectReferencesCollection FindReferences(params UnityEngine.Object[][] rootGrops)
        {
            Dictionary<UnityEngine.Object, ObjectReferences> references = new Dictionary<UnityEngine.Object, ObjectReferences>();
            for(int i = 0; i < rootGrops.Length; i++)
            {
                Find(i, rootGrops[i]);
            }
            ObjectReferencesCollection objectReferences = new ObjectReferencesCollection();
            FieldInfo fieldInfo = objectReferences.GetType().GetField("references", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo.SetValue(objectReferences, references);
            return objectReferences;

            bool AddReference(int gropID, UnityEngine.Object obj, UnityEngine.Object parent, string propertyPath)
            {
                bool result = false;
                if(!references.TryGetValue(obj, out ObjectReferences refe))
                {
                    result = true;
                    refe = references[obj] = new ObjectReferences()
                    {
                        obj = obj
                    };
                }
                if (!refe.inGrops.Contains(gropID))
                {
                    refe.inGrops.Add(gropID);
                }
                if(!refe.from.TryGetValue(parent, out List<string> propertyPaths))
                {
                    propertyPaths = refe.from[parent] = new List<string>();
                }
                if (!propertyPaths.Contains(propertyPath))
                {
                    propertyPaths.Add(propertyPath);
                }
                return result;
            }
            
            void Find(int gropID, params UnityEngine.Object[] objs)
            {
                for(int i = 0; i < objs.Length; i++)
                {
                    UnityEngine.Object obj = objs[i];
                    SerializedObject serializedObject = new SerializedObject(obj);
                    serializedObject.Update();
                    SerializedProperty serializedProperty = serializedObject.GetIterator();
                    while (serializedProperty.Next(true))
                    {
                        UnityEngine.Object referenceObject = null;
                        switch (serializedProperty.propertyType)
                        {
                            default:
                                break;
                            case SerializedPropertyType.ObjectReference:
                                referenceObject = serializedProperty.objectReferenceValue;
                                break;
                            case SerializedPropertyType.ExposedReference:
                                referenceObject = serializedProperty.exposedReferenceValue;
                                break;
                        }
                        if (referenceObject != null &&
                            AddReference(gropID, referenceObject, obj, serializedProperty.propertyPath))
                        {
                            
                            Debug.LogError(serializedProperty.name + " " + serializedProperty.type);

                            Find(gropID, referenceObject);
                        }
                    }
                    serializedObject.Dispose();
                }
            }
        }

        //[MenuItem("Assets/Bundle/Test0")]
        private static void Test0()
        {
            ResetAllAssetBundleNames();
            string[] abNames = AssetDatabase.GetAllAssetBundleNames();
            List<UnityEngine.Object[]> objGrops = new List<UnityEngine.Object[]>();
            foreach (string abName in abNames)
            {
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(abName);
                List<UnityEngine.Object> objGrop = new List<UnityEngine.Object>();
                foreach (string assetPath in assetPaths)
                {
                    Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                    objGrop.Add(AssetDatabase.LoadAssetAtPath(assetPath, assetType));
                }
                objGrops.Add(objGrop.ToArray());
            }
            ObjectReferencesCollection collection = FindReferences(objGrops.ToArray());
            Selection.objects = collection.GetReferencesObject();

        }

        public static readonly Regex MatchRegex = new Regex(@"\{\s*fileID\s*:\s*([0-9]+)\s*,\s*guid\s*:\s*([0-9a-f]+)\s*,\s*type\s*:\s*([0-9])\s*\}");
        public static readonly Regex MatchFileIDRegex = new Regex(@"(?:fileID\s*:\s*)([0-9]+)");
        public static readonly Regex MatchGUIDRegex = new Regex(@"(?:guid\s*:\s*)([0-9a-f]+)");
        public static readonly Regex MatchTypeRegex = new Regex(@"(?:type\s*:\s*)([0-9])");

        private static MatchedLine[] MatchFileText(string filePath)
        {
            List<MatchedLine> matchedLines = new List<MatchedLine>();
            string[] lines = File.ReadAllLines(filePath);
            for(int i = 0; i < lines.Length; i++)
            {
                MatchCollection matches = MatchRegex.Matches(lines[i]);
                if(matches.Count > 0)
                {
                    GroupCollection groups = matches[0].Groups;
                    matchedLines.Add(new MatchedLine()
                    {
                        filePath = filePath,
                        lineIndex = i,
                        fileID = long.Parse(groups[1].Value),
                        guid = groups[2].Value,
                        type = int.Parse(groups[3].Value)
                    });
                }
            }
            return matchedLines.ToArray();
        }

        private static void ReplaceWithNew(MatchedLine[] oris, long newFileID, string newGUID, int newType)
        {
            Dictionary<string, List<MatchedLine>> reorder = new Dictionary<string, List<MatchedLine>>();
            for(int i = 0; i < oris.Length; i++)
            {
                MatchedLine matchedLine = oris[i];
                if (!reorder.TryGetValue(matchedLine.filePath, out List<MatchedLine> matchLineList))
                {
                    reorder[matchedLine.filePath] = matchLineList = new List<MatchedLine>();
                }
                matchLineList.Add(matchedLine);
            }
            foreach(KeyValuePair<string, List<MatchedLine>> kv in reorder)
            {
                string filePath = kv.Key;
                List<MatchedLine> list = kv.Value;
                string[] lines = File.ReadAllLines(filePath);
                for(int i = 0; i < list.Count; i++)
                {
                    MatchedLine matchedLine = list[i];
                    string oriLine = lines[matchedLine.lineIndex];
                    MatchFileIDRegex.Replace(oriLine, newFileID.ToString());
                    MatchGUIDRegex.Replace(oriLine, newGUID);
                    MatchTypeRegex.Replace(oriLine, newType.ToString());
                }
                //commit
                File.WriteAllLines(filePath, lines);
            }
        }

        public class MatchedLine
        {
            public string filePath;

            public int lineIndex = -1;

            public long fileID;

            public string guid;

            public int type;
        }

        //[MenuItem("Assets/Bundle/Test1")]
        private static void Test1()
        {
            
        }

        private static readonly Regex HasReferencesRegex = new Regex(@"(?:\.unity)|(?:\.prefab)|(?:\.material)|(?:\.asset)");

        //[MenuItem("Assets/Bundle/Test2")]
        private static void Test2()
        {
            MatchedLine[] matchedLines = MatchFileText(Path.Combine(Application.dataPath + "/Example/Res/Scenes/Scene01/Scene01.unity"));
            Debug.Log(matchedLines.Length);
            foreach(MatchedLine matchedLine in matchedLines)
            {
                string guid = AssetDatabase.GUIDToAssetPath(matchedLine.guid);
                Debug.Log(guid);
            }
        }

        [MenuItem("Assets/Bundle/Output Resources")]
        public static void ExtractDefaultResources()
        {
            string[] resourcesGUIDs = GetResourcesGUIDs();
            string ParentFolderName = "Assets";
            string ResourcesFolderName = "_extract_resources";
            string buildinResourcesIDsPath = "ProjectSettings/BuildinResourcesIDs.txt";
            Regex FolderPathSplit = new Regex(@"\s*[\\/]+\s*");
            Regex NameRegex = new Regex(@"[\s\\/]");

            Dictionary<string, Dictionary<long, string>> guid_fileID_extractPath_map = new Dictionary<string, Dictionary<long, string>>();

            foreach (string guid in resourcesGUIDs)
            {
                string resourcesPath = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object[] resources = AssetDatabase.LoadAllAssetsAtPath(resourcesPath);
                RecordPath(resources);
                CopyToFolder(resources);
            }
            SaveRecordedPaths();

            string[] GetResourcesGUIDs()
            {
                string[] filePaths = AssetDatabase.FindAssets("buildin_resources_guids");
                if(filePaths.Length == 0)
                {
                    return new string[0];
                }
                TextAsset resourcesGUIDsText = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(filePaths[0]));
                if (resourcesGUIDsText == null)
                {
                    Debug.LogError("buildin_resources_guids.txt not exist.");
                    return new string[0];
                }
                string[] splitStrs = new Regex(@"\s+").Split(resourcesGUIDsText.text);
                List<string> resultStrs = new List<string>();
                for(int i = 0; i < splitStrs.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(splitStrs[i]))
                    {
                        resultStrs.Add(splitStrs[i].Trim());
                    }
                }
                return resultStrs.ToArray();
            }

            void RecordPath(UnityEngine.Object[] resources)
            {
                for(int i = 0; i < resources.Length; i++)
                {
                    UnityEngine.Object asset = resources[i];
                    Type assetType = asset.GetType();
                    if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localID))
                    {
                        string extractPath = Path.Combine(ParentFolderName, ResourcesFolderName, assetType.Name, ConvertName(asset.name) + ".asset");
                        AddToPathMap(guid, localID, extractPath);
                    }
                }
            }

            void SaveRecordedPaths()
            {
                foreach(KeyValuePair<string, Dictionary<long, string>> kv in guid_fileID_extractPath_map)
                {
                    Debug.Log("========================================");
                    foreach(KeyValuePair<long, string> kvvkv in kv.Value)
                    {
                        Debug.Log(kvvkv.Key + "    " + kvvkv.Value);
                    }
                }
            }

            void LoadRecordedPaths()
            {

            }

            void AddToPathMap(string guid, long fileID, string extractPath)
            {
                if(!guid_fileID_extractPath_map.TryGetValue(guid, out Dictionary<long, string> fileID_extractPath_map))
                {
                    guid_fileID_extractPath_map[guid] = fileID_extractPath_map = new Dictionary<long, string>();
                }
                fileID_extractPath_map[fileID] = extractPath;
            }

            string GetFormPathMap(string guid, long fileID)
            {
                if (guid_fileID_extractPath_map.TryGetValue(guid, out Dictionary<long, string> fileID_extractPath_map))
                {
                    if (fileID_extractPath_map.TryGetValue(fileID, out string value))
                    {
                        return value;
                    }
                }
                return null;
            }

            void CopyToFolder(UnityEngine.Object[] resources)
            {
                for (int i = 0; i < resources.Length; i++)
                {
                    UnityEngine.Object asset = resources[i];
                    Type assetType = asset.GetType();
                    if(assetType != typeof(MonoScript))
                    {
                        string folderPath = Path.Combine(ParentFolderName, ResourcesFolderName, assetType.Name);
                        string filePath = Path.Combine(folderPath, ConvertName(asset.name) + ".asset");
                        if (File.Exists(Path.Combine(Application.dataPath, filePath.Remove(0, ParentFolderName.Length + 1))))
                        {
                            AssetDatabase.DeleteAsset(filePath);
                        }
                        CreateExtractFolder(folderPath);
                        UnityEngine.Object clone = CloneAsset(asset, assetType);
                        if (clone != null)
                        {
                            AssetDatabase.CreateAsset(clone, filePath);
                        }
                    }
                }
                AssetDatabase.SaveAssets();
                EditorUtility.UnloadUnusedAssetsImmediate();
            }

            void CreateExtractFolder(string folderPath)
            {
                string[] folders = FolderPathSplit.Split(folderPath);
                if(folders.Length > 0)
                {
                    string parentPath = null;
                    for (int i = 0; i < folders.Length; i++)
                    {
                        string folder = folders[i];
                        if (!string.IsNullOrWhiteSpace(folder))
                        {
                            if(parentPath != null)
                            {
                                string currFolderPath = Path.Combine(parentPath, folder);
                                if (!AssetDatabase.IsValidFolder(currFolderPath))
                                {
                                    AssetDatabase.CreateFolder(parentPath, folder);
                                }
                                parentPath = currFolderPath;
                            }
                            else
                            {
                                parentPath = folder;
                            }
                        }
                    }
                }
            }

            UnityEngine.Object CloneAsset(UnityEngine.Object asset , Type type)
            {
                if (type.BaseType == typeof(Texture))
                {
                    return CloneTexture(asset);
                }
                else
                {
                    UnityEngine.Object clone = UnityEngine.Object.Instantiate(asset);
                    clone.name = asset.name;
                    return clone;
                }

                UnityEngine.Object CloneTexture(UnityEngine.Object obj)
                {
                    SerializedObject serializedObject = new SerializedObject(obj);
                    serializedObject.Update();
                    SerializedProperty isReadableProperty = serializedObject.FindProperty("m_IsReadable");
                    bool isReadable = isReadableProperty.boolValue;
                    isReadableProperty.boolValue = true;
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Dispose();
                    UnityEngine.Object clone = UnityEngine.Object.Instantiate(obj);
                    clone.name = asset.name;
                    SerializedObject serializedClone = new SerializedObject(clone);
                    serializedClone.Update();
                    serializedClone.FindProperty("m_IsReadable").boolValue = isReadable;
                    serializedClone.ApplyModifiedProperties();
                    serializedClone.Dispose();
                    return clone;
                }
            }

            string ConvertName(string name)
            {
                return NameRegex.Replace(name, "_");
            }
        }
    }
#endif
}
