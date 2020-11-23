/**
 *  Author ZhanQI
 */

using System.IO;
using System.Text.RegularExpressions;

namespace E
{
    public sealed class AssetBundlePath
    {
        public const string Extension = ".ab";

        public static string FileToBundlePath(string filePath)
        {
            return Path.Combine(AssetBundleSettings.Instance.GetDownloadURI(), FileToBundleName(filePath));
        }

        public static string BundleNameToBundlePath(string bundleName)
        {
            return Path.Combine(AssetBundleSettings.Instance.GetDownloadURI(), bundleName);
        }

        public static string FileToBundleName(string filePath)
        {
            string result = FormatPath(GetDirectoryName(filePath));
            if (!result.EndsWith(Extension))
            {
                result += Extension;
            }
            return result;
        }

        public static string DirectoryToBundleName(string dir)
        {
            string result = FormatPath(dir);
            if (!result.EndsWith(Extension))
            {
                result += Extension;
            }
            return result;
        }

        public static string FileToAssetName(string filePath)
        {
            return GetFileName(filePath);
        }

        public static string GetManifestPath()
        {
            return Path.Combine(
                    AssetBundleSettings.Instance.GetDownloadURI(),
                    AssetBundleSettings.Instance.GetBuildTargetName());
        }

        private static readonly Regex FormatReg0 = new Regex(@"\s+");
        private static readonly Regex FormatReg1 = new Regex(@"[/\\]+");
        private static readonly Regex FormatReg2 = new Regex(@"^[/\\]");

        public static string FormatPath(string oriPath)
        {
            return FormatReg0.Replace(FormatReg1.Replace(FormatReg2.Replace(oriPath.Trim(), ""), "."), "_").ToLower();
        }

        public static string GetDirectoryName(string filePath)
        {
            return filePath.Substring(0, filePath.Length - GetFileName(filePath).Length - 1);
        }

        private static readonly Regex fileNameRegex = new Regex(@"(?:[/\\]{0,1}(?:[^/\\\:\?\*\<\>\|]+[/\\])+([^/\\\:\?\*\<\>\|]+(?:\.[^/\\\:\?\*\<\>\|]+){0,1}))");

        public static string GetFileName(string filePath)
        {
            return fileNameRegex.Match(filePath).Groups[1].Value;
        }
    }
}