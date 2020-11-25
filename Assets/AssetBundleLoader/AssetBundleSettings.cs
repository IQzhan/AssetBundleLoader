using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace E
{
    [Serializable]
    public sealed class AssetBundleSettings : ScriptableObject
    {
        private static readonly object objLock = new object();

        private static AssetBundleSettings instance;

        public static AssetBundleSettings Instance 
        {
            get
            {
                if(instance == null)
                {
                    lock (objLock)
                    {
                        if(instance == null)
                        {
                            instance = Resources.Load<AssetBundleSettings>("AssetBundleSettings");
                        }
                    }
                }
                return instance;
            }
        }
#if UNITY_EDITOR
        public BuildTarget buildTarget = BuildTarget.StandaloneWindows64;

        public Compressed compressed = Compressed.LZMA;

        [SerializeField]
        private string outputPath = "%APPLICATION_PATH%/Output/Windows/%PRODUCT_NAME%/Data";

        public List<ResourceFolder> resourceFolders = new List<ResourceFolder>();
#endif
        [SerializeField]
        private string buildTargetName = "StandaloneWindows64";

        [SerializeField]
        private PublishPath readPath;

        public bool useWebRequest = false;
#if UNITY_EDITOR
        public bool simulateInEditor = true;
#endif
        public bool logCommon = false;

        public bool logError = true;

        public bool logWarning = false;

        /// <summary>
        /// Get target platform name
        /// </summary>
        /// <returns></returns>
        public string GetBuildTargetName()
        {
#if UNITY_EDITOR
            return buildTargetName = buildTarget.ToString();
#else
            return buildTargetName;
#endif
        }

        /// <summary>
        /// Get download URI
        /// </summary>
        /// <returns></returns>
        public string GetDownloadURI()
        {
#if UNITY_EDITOR
            return GetTargetURI();
#else
            return readPath.GetPath(GetBuildTargetName());
#endif
        }
#if UNITY_EDITOR
        //private static readonly Regex PathRegex = new Regex(@"^(?:\s*[a-zA-Z]\s*:\s*(?:[\\/](?:\s*[\S-[\/|<>?*:""]]+)+)*(?:\s[\\/]\s*){0,1})&");

        /// <summary>
        /// Get build target URI
        /// </summary>
        /// <returns></returns>
        public string GetTargetURI()
        {
            return Path.Combine(AssetBundleBuildConfigHelper.ConvertString(outputPath), "AssetBundles", GetBuildTargetName());
        }

        /// <summary>
        /// Get source folders
        /// </summary>
        /// <returns></returns>
        public string[] GetResourcesFolders()
        {
            if(resourceFolders != null)
            {
                string[] result = new string[resourceFolders.Count];
                for(int i = 0; i < resourceFolders.Count; i++)
                {
                    result[i] = AssetBundleBuildConfigHelper.ConvertString(resourceFolders[i].path);
                }
                return result;
            }
            return new string[0];
        }

        public enum Compressed
        {
            None = 1,
            LZMA = 0,
            LZ4 = 256
        }
#endif
        [Serializable]
        public class PublishPath
        {
            public PublishPathType pathType = PublishPathType.Application;

            public string custom = "%APPLICATION_PATH%/Output/Windows/%PRODUCT_NAME%";

            public string subpath = "Data";

            public string GetPath(string folderName)
            {
                switch (pathType)
                {
                    default:
                    case PublishPathType.Application:
                        return Path.Combine(Environment.CurrentDirectory, AssetBundleBuildConfigHelper.ConvertString(subpath), "AssetBundles", folderName);
                    case PublishPathType.StreamingAssets:
                        return Path.Combine(Application.streamingAssetsPath, AssetBundleBuildConfigHelper.ConvertString(subpath), "AssetBundles", folderName);
                    case PublishPathType.PersistentDataPath:
                        return Path.Combine(Application.persistentDataPath, AssetBundleBuildConfigHelper.ConvertString(subpath), "AssetBundles", folderName);
                    case PublishPathType.Custom:
                        return Path.Combine(AssetBundleBuildConfigHelper.ConvertString(custom), AssetBundleBuildConfigHelper.ConvertString(subpath), "AssetBundles", folderName);
                }
            }
        }

        public enum PublishPathType
        {
            Application,
            StreamingAssets,
            PersistentDataPath,
            Custom
        }

        [Serializable]
        public class ResourceFolder
        {
            public string path = string.Empty;
        }

        private class AssetBundleBuildConfigHelper
        {
#if UNITY_EDITOR
            private const string ApplicationPath = "%APPLICATION_PATH%";
#endif
            private const string CompanyName = "%COMPANY_NAME%";

            private const string ProductName = "%PRODUCT_NAME%";

            private static readonly Regex reg0 = new Regex(@"[\\/]&");

            public static string ConvertString(string input)
            {
                if (input != null)
                {
                    return reg0.Replace(input.Trim()
#if UNITY_EDITOR
                        .Replace(ApplicationPath, Environment.CurrentDirectory)
#endif
                        .Replace(CompanyName, Application.companyName)
                        .Replace(ProductName, Application.productName), string.Empty);
                }
                return input;
            }
        }
    }
}