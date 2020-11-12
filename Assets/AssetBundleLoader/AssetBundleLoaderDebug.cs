using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace E
{
    public sealed class AssetBundleLoaderDebug
    {
        public static void Log(string message)
        {
            if (AssetBundleBuildConfig.Instance.logCommon)
            {
                Debug.Log(message);
            }
        }

        public static void LogError(string message)
        {
            if (AssetBundleBuildConfig.Instance.logError)
            {
                Debug.LogError(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (AssetBundleBuildConfig.Instance.logWarning)
            {
                Debug.LogWarning(message);
            }
        }
    }
}
