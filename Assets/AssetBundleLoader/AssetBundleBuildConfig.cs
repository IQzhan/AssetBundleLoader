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

        [Header("目标平台")]
        public BuildTarget buildTarget = BuildTarget.StandaloneWindows;

        [Header("压缩方式"), Tooltip("LZMA最小,LZ4稍大,None无压缩")]
        public Compressed compressed = Compressed.LZMA;

        [Header("输出路径")]
        public PublishFolder outputFolder;

        [Header("发布后的读取模式")]
        public PublishFolder readFunction;

        [Header("源文件相对路径"), Tooltip("Assets下的源文件夹相对路径")]
        public string[] rourceFolders;

        [Header("控制台信息")]
        public bool logCommon = false;

        [Header("控制台错误信息")]
        public bool logError = true;

        [Header("控制台警告信息")]
        public bool logWarning = false;

        /// <summary>
        /// 获取打包路径
        /// </summary>
        /// <returns></returns>
        public string GetTargetURI()
        {
            return outputFolder.GetPath(buildTarget.ToString());
        }

        /// <summary>
        /// 获取下载路径
        /// </summary>
        /// <returns></returns>
        public string GetDownloadURI()
        {
            return readFunction.GetPath(buildTarget.ToString());
        }

        /// <summary>
        /// 获取源文件夹
        /// </summary>
        /// <returns></returns>
        public string[] GetRourceFolders()
        {
            if(rourceFolders != null)
            {
                for(int i = 0; i < rourceFolders.Length; i++)
                {
                    rourceFolders[i] = AssetBundleBuildConfigHelper.ConvertString(rourceFolders[i]);
                }
            }
            return rourceFolders;
        }

        [Serializable]
        public class PublishFolder
        {
            [Tooltip("路径类型")]
            public PublishFolderType folderType;

            [Tooltip("自定义路径")]
            public string custom;

            public string GetPath(string folderName)
            {
                switch (folderType)
                {
                    default:
                    case PublishFolderType.Application:
                        return Path.Combine(Environment.CurrentDirectory, "AssetBundles", folderName);
                    case PublishFolderType.StreamingAssets:
                        return Path.Combine(Application.streamingAssetsPath, "AssetBundles", folderName);
                    case PublishFolderType.PersistentDataPath:
                        return Path.Combine(Application.persistentDataPath, "AssetBundles", folderName);
                    case PublishFolderType.Custom:
                        return Path.Combine(AssetBundleBuildConfigHelper.ConvertString(custom), "AssetBundles", folderName);
                }
            }
        }

        public enum PublishFolderType
        {
            Application,
            StreamingAssets,
            PersistentDataPath,
            Custom
        }

        public enum Compressed
        {
            None = 1,
            LZMA = 0,
            LZ4 = 256
        }

        public enum BuildTarget
        {
            //
            // 摘要:
            //     Build a macOS standalone (Intel 64-bit).
            StandaloneOSX = 2,
            //
            // 摘要:
            //     Build a Windows standalone.
            StandaloneWindows = 5,
            //
            // 摘要:
            //     Build an iOS player.
            iOS = 9,
            //
            // 摘要:
            //     Build an Android .apk standalone app.
            Android = 13,
            //
            // 摘要:
            //     Build a Linux standalone.
            StandaloneLinux = 17,
            //
            // 摘要:
            //     Build a Windows 64-bit standalone.
            StandaloneWindows64 = 19,
            //
            // 摘要:
            //     WebGL.
            WebGL = 20,
            //
            // 摘要:
            //     Build an Windows Store Apps player.
            WSAPlayer = 21,
            //
            // 摘要:
            //     Build a Linux 64-bit standalone.
            StandaloneLinux64 = 24,
            //
            // 摘要:
            //     Build a PS4 Standalone.
            PS4 = 31,
            //
            // 摘要:
            //     Build a Xbox One Standalone.
            XboxOne = 33,
            //
            // 摘要:
            //     Build to Nintendo 3DS platform.
            N3DS = 35,
            WiiU = 36,
            //
            // 摘要:
            //     Build to Apple's tvOS platform.
            tvOS = 37,
            //
            // 摘要:
            //     Build a Nintendo Switch player.
            Switch = 38,
            Lumin = 39,
            //
            // 摘要:
            //     Build a Stadia standalone.
            Stadia = 40
        }

        private class AssetBundleBuildConfigHelper
        {
            private const string CompanyName = "%COMPANY_NAME%";

            private const string ProductName = "%PRODUCT_NAME%";

            public static string ConvertString(string input)
            {
                if (input != null)
                {
                    return input
                    .Replace(CompanyName, Application.companyName)
                    .Replace(ProductName, Application.productName);
                }
                return input;
            }
        }
    }
}