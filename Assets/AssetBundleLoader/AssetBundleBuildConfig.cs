using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace E
{
    [Serializable]
    public sealed class AssetBundleBuildConfig : ScriptableObject
    {
        private static readonly object objLock = new object();

        private static AssetBundleBuildConfig instance;

        public static AssetBundleBuildConfig Instance 
        {
            get
            {
                if(instance == null)
                {
                    lock (objLock)
                    {
                        if(instance == null)
                        {
                            instance = Resources.Load<AssetBundleBuildConfig>("AssetBundleBuildConfig");
                        }
                    }
                }
                return instance;
            }
        }
#if UNITY_EDITOR
        [Header("目标平台")]
        public BuildTarget buildTarget = BuildTarget.StandaloneWindows;

        [Header("压缩方式"), Tooltip("LZMA最小,LZ4稍大,None无压缩")]
        public Compressed compressed = Compressed.LZMA;

        [Header("输出路径"), SerializeField]
        private PublishPath outputPath;
#endif
        [SerializeField, HideInInspector]
        private string buildTargetName;

        [Header("读取路径"), SerializeField]
        private PublishPath readPath;

        [Tooltip("使用WebRequest方式读取")]
        public bool useWebRequest = false;
#if UNITY_EDITOR
        [Tooltip("在编辑器中模拟加载，但不是真正的加载assetbundle")]
        public bool simulateInEditor = true;

        [Header("源文件相对路径"), Tooltip("Assets下的源文件夹相对路径")]
        public List<ResourceFolder> resourceFolders = new List<ResourceFolder>();
#endif
        [Header("控制台信息")]
        public bool logCommon = false;

        [Header("控制台错误信息")]
        public bool logError = true;

        [Header("控制台警告信息")]
        public bool logWarning = false;

        /// <summary>
        /// 获取目标平台名称
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
        /// 获取下载路径
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
        /// <summary>
        /// 获取打包路径
        /// </summary>
        /// <returns></returns>
        public string GetTargetURI()
        {
            return outputPath.GetPath(GetBuildTargetName());
        }

        /// <summary>
        /// 获取源文件夹
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
            return null;
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
            [Tooltip("路径类型")]
            public PublishPathType pathType;

            [Tooltip("自定义路径")]
            public string custom;

            [Tooltip("叠加子路径")]
            public string subpath;

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
            public string path;
        }

        private class AssetBundleBuildConfigHelper
        {
            private const string CompanyName = "%COMPANY_NAME%";

            private const string ProductName = "%PRODUCT_NAME%";

            public static string ConvertString(string input)
            {
                if (input != null)
                {
                    return input.Trim()
                    .Replace(CompanyName, Application.companyName)
                    .Replace(ProductName, Application.productName);
                }
                return input;
            }
        }
    }
}