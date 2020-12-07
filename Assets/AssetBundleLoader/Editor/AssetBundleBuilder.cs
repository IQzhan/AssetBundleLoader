/**
 *  Author ZhanQI
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        //private struct MatchedLine
        //{
        //    public string filePath;

        //    public int lineIndex;

        //    public string guid;

        //    public long fileID;

        //    public int type;
        //}

        //private struct FileIDInfo
        //{
        //    public string filePath;

        //    public int mainClassID;

        //    public long fileID;

        //    public string guid;

        //    public int type;
        //}

        //private class FileYMALInfo
        //{
        //    public struct Menber
        //    {
        //        public int classID;

        //        public long fileID;

        //        public Reference[] references;
        //    }

        //    public struct Reference
        //    {
        //        public FileYMALInfo target;

        //        public string line;
        //    }

        //    //file path
        //    public string path;

        //    //file guid
        //    public string guid;

        //    //file type
        //    public int type;

        //    //the main asset in this file, null if this asset is scene 
        //    public Menber main;

        //    //all assets in this file
        //    public Menber[] menbers;
        //}

        //[MenuItem("Assets/Bundle/Test1")]
        //private static void Test1()
        //{
        //    //ExtractDefaultResources();

        //}

        private const string ParentFolderName = "Assets";
        private const string ResourcesFolderName = "_extract_resources";
        private const string CopyResourcesFolderName = "Library";
        private const string BuildinResourcesIDsPath = "Library/BuildinResourcesIDs.txt";

        ////[MenuItem("Assets/Bundle/Reference Build")]
        //public static void ReferenceBuild()
        //{
        //    Regex MatchRegex = new Regex(@"\{\s*fileID\s*:\s*([0-9]+)\s*,\s*guid\s*:\s*([0-9a-f]+)\s*,\s*type\s*:\s*([0-9])\s*\}");
        //    Regex MatchFileIDRegex = new Regex(@"(?:fileID\s*:\s*)([0-9]+)");
        //    Regex MatchGUIDRegex = new Regex(@"(?:guid\s*:\s*)([0-9a-f]+)");
        //    Regex MatchTypeRegex = new Regex(@"(?:type\s*:\s*)([0-9])");
        //    Regex MatchClassIDAndFileIDInAssetRegex = new Regex(@"^(?:\s*---\s*!u!([0-9]+)\s*&([0-9]+)\s*)$");
        //    Regex MatchGUIDInMetaRegex = new Regex(@"^(?:\s*guid\s*:\s*([0-9a-f]+))$");
        //    Regex MatchAssetBundleName = new Regex(@"^(?:\s*assetBundleName\s*:\s*(\S*)\s*)$");

        //    Dictionary<string, Dictionary<long, string>> guid_fileID_extractPath_map = new Dictionary<string, Dictionary<long, string>>();
        //    Action command = null;

        //    ExtractDefaultResources();
        //    Execute();

        //    void Execute()
        //    {
        //        try
        //        {
        //            UnityEngine.Object[][] objectGrops = FindObjectGrops();
        //            FindReference(objectGrops);
        //            Task.Factory.StartNew(() =>
        //            {
        //                LoadRecordedPaths();

        //                command?.Invoke();
        //                Debug.Log("Asset bundle build complete.");
        //            });
        //        }
        //        catch(Exception e)
        //        {
        //            Revert();
        //            Debug.Log("Asset bundle build faild.");
        //            throw e;
        //        }
        //    }

        //    void Revert()
        //    {

        //    }

        //    UnityEngine.Object[][] FindObjectGrops()
        //    {
        //        ResetAllAssetBundleNames();
        //        string[] abNames = AssetDatabase.GetAllAssetBundleNames();
        //        List<UnityEngine.Object[]> objGrops = new List<UnityEngine.Object[]>();
        //        foreach (string abName in abNames)
        //        {
        //            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(abName);
        //            List<UnityEngine.Object> objGrop = new List<UnityEngine.Object>();
        //            foreach (string assetPath in assetPaths)
        //            {
        //                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
        //                objGrop.Add(AssetDatabase.LoadAssetAtPath(assetPath, assetType));
        //            }
        //            objGrops.Add(objGrop.ToArray());
        //        }
        //        return objGrops.ToArray();
        //    }

        //    void FindReference(UnityEngine.Object[][] grops)
        //    {
        //        for(int i = 0; i < grops.Length; i++)
        //        {
        //            int gropId = i;
        //            for(int j = 0; j < grops[i].Length; i++)
        //            {
        //                UnityEngine.Object obj = grops[i][j];
        //                Find(obj, gropId);
        //            }
        //        }

        //        void Find(UnityEngine.Object obj, int gropId)
        //        {
        //            string assetPath = AssetDatabase.GetAssetPath(obj);


        //        }

        //        void GetObjectInfos(string assetPath)
        //        {
        //            string absAssetPath = Path.Combine(Environment.CurrentDirectory, assetPath);
        //            //guid from .meta
        //            string guid = GetGUID(assetPath);
        //            //fileID from asset

        //        }
        //    }

        //    bool IsYMALFile(string assetPath)
        //    {

        //        return true;
        //    }

        //    string GetGUID(string assetPath)
        //    {
        //        string absAssetPath = Path.Combine(Environment.CurrentDirectory, assetPath);
        //        string metaPath = absAssetPath + ".meta";
        //        string[] lines = File.ReadAllLines(metaPath);
        //        for (int i = 0; i < lines.Length; i++)
        //        {
        //            MatchCollection matches = MatchGUIDInMetaRegex.Matches(lines[i]);
        //            if (matches.Count > 0)
        //            {
        //                GroupCollection groups = matches[0].Groups;
        //                string value = groups[1].Value;
        //                if (!string.IsNullOrWhiteSpace(value))
        //                {
        //                    return value;
        //                }
        //            }
        //        }
        //        return null;
        //    }

        //    string[] GetFileIDs(string assetPath)
        //    {
        //        string absAssetPath = Path.Combine(Environment.CurrentDirectory, assetPath);
        //        string[] lines = File.ReadAllLines(absAssetPath);
        //        for (int i = 0; i < lines.Length; i++)
        //        {
        //            MatchCollection matches = MatchClassIDAndFileIDInAssetRegex.Matches(lines[i]);
        //            if (matches.Count > 0)
        //            {
        //                GroupCollection groups = matches[0].Groups;
        //                //TODO 
        //            }
        //        }
        //        return null;
        //    }

        //    void SetAssetBunldeName(string assetPath, string assetBundleName)
        //    {
        //        string absAssetPath = Path.Combine(Environment.CurrentDirectory, assetPath);
        //        string metaPath = absAssetPath + ".meta";
        //        string[] lines = File.ReadAllLines(metaPath);
        //        for(int i = 0; i < lines.Length; i++)
        //        {
        //            if (MatchAssetBundleName.IsMatch(lines[i]))
        //            {
        //                lines[i] = "  assetBundleName: " + assetBundleName;
        //            }
        //        }
        //    }

        //    string GetAssetBundleName(string assetPath)
        //    {
        //        string absAssetPath = Path.Combine(Environment.CurrentDirectory, assetPath);
        //        string metaPath = absAssetPath + ".meta";
        //        string[] lines = File.ReadAllLines(metaPath);
        //        for (int i = 0; i < lines.Length; i++)
        //        {
        //            MatchCollection matches = MatchAssetBundleName.Matches(lines[i]);
        //            if (matches.Count > 0)
        //            {
        //                GroupCollection groups = matches[0].Groups;
        //                string value = groups[1].Value;
        //                if (!string.IsNullOrWhiteSpace(value))
        //                {
        //                    return value;
        //                }
        //            }
        //        }
        //        return null;
        //    }

        //    MatchedLine[] MatchFileText(string filePath)
        //    {
        //        List<MatchedLine> matchedLines = new List<MatchedLine>();
        //        string[] lines = File.ReadAllLines(filePath);
        //        for (int i = 0; i < lines.Length; i++)
        //        {
        //            MatchCollection matches = MatchRegex.Matches(lines[i]);
        //            if (matches.Count > 0)
        //            {
        //                GroupCollection groups = matches[0].Groups;
        //                matchedLines.Add(new MatchedLine()
        //                {
        //                    filePath = filePath,
        //                    lineIndex = i,
        //                    fileID = long.Parse(groups[1].Value),
        //                    guid = groups[2].Value,
        //                    type = int.Parse(groups[3].Value)
        //                });
        //            }
        //        }
        //        return matchedLines.ToArray();
        //    }

        //    void ReplaceWithNew(MatchedLine[] oris, long newFileID, string newGUID, int newType)
        //    {
        //        Dictionary<string, List<MatchedLine>> reorder = new Dictionary<string, List<MatchedLine>>();
        //        for (int i = 0; i < oris.Length; i++)
        //        {
        //            MatchedLine matchedLine = oris[i];
        //            if (!reorder.TryGetValue(matchedLine.filePath, out List<MatchedLine> matchLineList))
        //            {
        //                reorder[matchedLine.filePath] = matchLineList = new List<MatchedLine>();
        //            }
        //            matchLineList.Add(matchedLine);
        //        }
        //        foreach (KeyValuePair<string, List<MatchedLine>> kv in reorder)
        //        {
        //            string filePath = kv.Key;
        //            List<MatchedLine> list = kv.Value;
        //            string[] lines = File.ReadAllLines(filePath);
        //            for (int i = 0; i < list.Count; i++)
        //            {
        //                MatchedLine matchedLine = list[i];
        //                string oriLine = lines[matchedLine.lineIndex];
        //                MatchFileIDRegex.Replace(oriLine, "fileID: " + newFileID.ToString());
        //                MatchGUIDRegex.Replace(oriLine, "guid: " + newGUID);
        //                MatchTypeRegex.Replace(oriLine, "type: " + newType.ToString());
        //            }
        //            command += () =>
        //            {
        //                File.WriteAllLines(filePath, lines);
        //            };
        //        }
        //    }

        //    void LoadRecordedPaths()
        //    {
        //        Regex KeyValueMatch = new Regex(@"^(?:\s*([\w]+(?:\s+\w+)*)\s*:\s*([\w]+(?:\s+\w+)*)\s*)$");
        //        string filePath = Path.Combine(Environment.CurrentDirectory, BuildinResourcesIDsPath);
        //        if (File.Exists(filePath))
        //        {
        //            string[] lines = File.ReadAllLines(filePath);
        //            Dictionary<long, string> currGrop = null;
        //            foreach (string line in lines)
        //            {
        //                MatchCollection matchCollection = KeyValueMatch.Matches(line);
        //                if (matchCollection.Count > 0)
        //                {
        //                    Match match = matchCollection[0];
        //                    GroupCollection groups = match.Groups;
        //                    string key = groups[1].Value;
        //                    string value = groups[2].Value;
        //                    if (!string.IsNullOrWhiteSpace(key))
        //                    {
        //                        if (string.IsNullOrWhiteSpace(value))
        //                        {
        //                            currGrop = guid_fileID_extractPath_map[key] = new Dictionary<long, string>();
        //                        }
        //                        else
        //                        {
        //                            currGrop[long.Parse(key)] = value;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            throw new FileNotFoundException(BuildinResourcesIDsPath + " dosen't exist.");
        //        }
        //    }

        //    string GetFormPathMap(string guid, long fileID)
        //    {
        //        if (guid_fileID_extractPath_map.TryGetValue(guid, out Dictionary<long, string> fileID_extractPath_map))
        //        {
        //            if (fileID_extractPath_map.TryGetValue(fileID, out string value))
        //            {
        //                return value;
        //            }
        //        }
        //        return null;
        //    }

        //}

        public static void ExtractDefaultResources()
        {
            Regex AssetsFolderMatch = new Regex(@"^(?:Asset[\\/]).*");
            Regex FolderPathSplit = new Regex(@"\s*[\\/]+\s*");
            Regex NameRegex = new Regex(@"[\s\\/]");
            Dictionary<string, Dictionary<long, string>> guid_fileID_extractPath_map = new Dictionary<string, Dictionary<long, string>>();
            
            if (Check())
            {
                Execute();
            }
            
            bool Check()
            {
                string targetPath = Path.Combine(Environment.CurrentDirectory, CopyResourcesFolderName, ResourcesFolderName);
                string buildinResourcesIDsPath = Path.Combine(Environment.CurrentDirectory, BuildinResourcesIDsPath);
                if (!CheckVersion() ||
                    !Directory.Exists(targetPath) || 
                    !File.Exists(buildinResourcesIDsPath))
                {
                    return true;
                }
                return false;
            }

            bool CheckVersion()
            {
                string key = "_unity_buildin_resources_version_";
                if (PlayerPrefs.HasKey(key))
                {
                    string unityVersion = Application.unityVersion;
                    if (PlayerPrefs.GetString(key).Equals(unityVersion))
                    {
                        return true;
                    }
                }
                return false;
            }

            void UpdateVersion()
            {
                string key = "_unity_buildin_resources_version_";
                PlayerPrefs.SetString(key, Application.unityVersion);
            }

            void Execute()
            {
                try
                {
                    Revert();
                    string[] resourcesGUIDs = GetResourcesGUIDs();
                    foreach (string guid in resourcesGUIDs)
                    {
                        string resourcesPath = AssetDatabase.GUIDToAssetPath(guid);
                        UnityEngine.Object[] resources = AssetDatabase.LoadAllAssetsAtPath(resourcesPath);
                        if(resources != null && resources.Length > 0)
                        {
                            RecordPath(resources);
                            CopyToFolder(resources);
                        }
                    }
                    CopyResourcesFolder();
                    SaveRecordedPaths();
                    UpdateVersion();
                    Debug.Log("Extract default resources complete.");
                }
                catch(Exception e)
                {
                    Revert();
                    Debug.Log("Extract default resources faild.");
                    throw e;
                }
            }

            void Revert()
            {
                string resourcesPath = Path.Combine(Environment.CurrentDirectory, ParentFolderName, ResourcesFolderName);
                string targetPath = Path.Combine(Environment.CurrentDirectory, CopyResourcesFolderName, ResourcesFolderName);
                string buildinResourcesIDsPath = Path.Combine(Environment.CurrentDirectory, BuildinResourcesIDsPath);
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
                if (Directory.Exists(resourcesPath))
                {
                    AssetDatabase.DeleteAsset(Path.Combine(ParentFolderName, ResourcesFolderName));
                }
                if(File.Exists(buildinResourcesIDsPath))
                {
                    File.Delete(buildinResourcesIDsPath);
                }
            }

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
                    if(assetType != typeof(MonoScript))
                    {
                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localID))
                        {
                            string extractPath = Path.Combine(assetType.Name, ConvertName(asset.name));
                            AddToPathMap(guid, localID, extractPath);
                        }
                    }
                }
            }

            void CopyResourcesFolder()
            {
                string resourcesPath = Path.Combine(Environment.CurrentDirectory, ParentFolderName, ResourcesFolderName);
                string targetPath = Path.Combine(Environment.CurrentDirectory, CopyResourcesFolderName, ResourcesFolderName);
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
                if (Directory.Exists(resourcesPath))
                {
                    CopyDirectory(resourcesPath, targetPath, true);
                    AssetDatabase.DeleteAsset(Path.Combine(ParentFolderName, ResourcesFolderName));
                }
                void CopyDirectory(string SourcePath, string DestinationPath, bool overwriteexisting)
                {
                    try
                    {
                        SourcePath = SourcePath.EndsWith(@"\") ? SourcePath : SourcePath + @"\";
                        DestinationPath = DestinationPath.EndsWith(@"\") ? DestinationPath : DestinationPath + @"\";
                        if (Directory.Exists(SourcePath))
                        {
                            if (!Directory.Exists(DestinationPath))
                            {
                                Directory.CreateDirectory(DestinationPath);
                            }
                            foreach (string fls in Directory.GetFiles(SourcePath))
                            {
                                FileInfo flinfo = new FileInfo(fls);
                                flinfo.CopyTo(DestinationPath + flinfo.Name, overwriteexisting);
                            }
                            foreach (string drs in Directory.GetDirectories(SourcePath))
                            {
                                DirectoryInfo drinfo = new DirectoryInfo(drs);
                                CopyDirectory(drs, DestinationPath + drinfo.Name, overwriteexisting);
                            }
                        }
                    }
                    catch (IOException e)
                    {
                        throw e;
                    }
                }
            }

            

            void SaveRecordedPaths()
            {
                if(guid_fileID_extractPath_map.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (KeyValuePair<string, Dictionary<long, string>> kv in guid_fileID_extractPath_map)
                    {
                        sb.AppendLine(kv.Key + ":");
                        foreach (KeyValuePair<long, string> kvvkv in kv.Value)
                        {
                            sb.AppendLine("  " + kvvkv.Key + ": " + kvvkv.Value);
                        }
                    }
                    string text = sb.ToString();
                    File.WriteAllText(Path.Combine(Environment.CurrentDirectory, BuildinResourcesIDsPath), text);
                }
            }

            void AddToPathMap(string guid, long fileID, string extractPath)
            {
                if(!guid_fileID_extractPath_map.TryGetValue(guid, out Dictionary<long, string> fileID_extractPath_map))
                {
                    guid_fileID_extractPath_map[guid] = fileID_extractPath_map = new Dictionary<long, string>();
                }
                fileID_extractPath_map[fileID] = extractPath;
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
                        string sysFilePath = Path.Combine(Environment.CurrentDirectory, filePath);
                        if (File.Exists(sysFilePath))
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
                if (AssetsFolderMatch.IsMatch(folderPath))
                {
                    string[] folders = FolderPathSplit.Split(folderPath);
                    if (folders.Length > 0)
                    {
                        string parentPath = null;
                        for (int i = 0; i < folders.Length; i++)
                        {
                            string folder = folders[i];
                            if (!string.IsNullOrWhiteSpace(folder))
                            {
                                if (parentPath != null)
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
                else
                {
                    folderPath = Path.Combine(Environment.CurrentDirectory, folderPath);
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }
                }
            }

            UnityEngine.Object CloneAsset(UnityEngine.Object asset , Type type)
            {
                UnityEngine.Object clone;
                if (type.BaseType == typeof(Texture))
                {
                    clone = CloneTexture(asset);
                }
                else
                {
                    clone = UnityEngine.Object.Instantiate(asset);
                }
                clone.name = asset.name;
                return clone;

                UnityEngine.Object CloneTexture(UnityEngine.Object obj)
                {
                    SerializedObject serializedObject = new SerializedObject(obj);
                    serializedObject.Update();
                    SerializedProperty isReadableProperty = serializedObject.FindProperty("m_IsReadable");
                    bool isReadable = isReadableProperty.boolValue;
                    isReadableProperty.boolValue = true;
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Dispose();
                    UnityEngine.Object obj0 = UnityEngine.Object.Instantiate(obj);
                    SerializedObject serializedClone = new SerializedObject(obj0);
                    serializedClone.Update();
                    serializedClone.FindProperty("m_IsReadable").boolValue = isReadable;
                    serializedClone.ApplyModifiedProperties();
                    serializedClone.Dispose();
                    return obj0;
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
