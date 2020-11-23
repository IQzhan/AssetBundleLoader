using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace E.Editor
{
    [CustomEditor(typeof(AssetBundleSettings))]
    public class AssetBundleSettingsEditor : UnityEditor.Editor
    {
        private static AssetBundleSettings asset;
        private static Type assetType;
        private static ReorderableList resourceFoldersList;
        private static readonly string help = 
            "Use %COMPANY_NAME% in path and it will be replaced with Company Name in player settings;" + Environment.NewLine +
            "Use %PRODUCT_NAME% in path and it will be replaced with Product Name in player settings.";

        public static void Init()
        {
            if (asset == null)
            {
                asset = AssetBundleSettings.Instance;
            }
            if (assetType == null)
            {
                assetType = asset.GetType();
            }
            if (resourceFoldersList == null)
            {
                List<AssetBundleSettings.ResourceFolder> resourceFolders = asset.resourceFolders;
                resourceFoldersList = new ReorderableList(resourceFolders, typeof(AssetBundleSettings.ResourceFolder), true, true, true, true)
                {
                    drawHeaderCallback = (rect) =>
                    EditorGUI.LabelField(rect, new GUIContent("Resource Folders", "资源文件")),
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        AssetBundleSettings.ResourceFolder resourceFolder = resourceFolders[index];
                        if (isFocused)
                        {
                            resourceFolder.path = EditorGUI.TextField(rect, resourceFolder.path);
                        }
                        else
                        {
                            EditorGUI.LabelField(rect, resourceFolder.path);
                        }
                    }
                };
            }
        }

        public static void Draw()
        {
            EditorGUILayout.BeginVertical("HelpBox");
            //Build Target
            EditorGUI.BeginChangeCheck();
            asset.buildTarget = (BuildTarget)EditorGUILayout.EnumPopup(new GUIContent("Build Target", "目标平台"), asset.buildTarget);
            if (EditorGUI.EndChangeCheck())
            {
                FieldInfo buildTargetNameInfo = assetType.GetField("buildTargetName", BindingFlags.NonPublic | BindingFlags.Instance);
                buildTargetNameInfo.SetValue(asset, asset.buildTarget.ToString());
            }
            //Compressed
            asset.compressed = (AssetBundleSettings.Compressed)EditorGUILayout.EnumPopup(new GUIContent("Compressed", "压缩方式，LZMA最小，LZ4稍大，None无压缩"), asset.compressed);
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("HelpBox");
            //Output Path
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            FieldInfo outputPathInfo = assetType.GetField("outputPath", BindingFlags.NonPublic | BindingFlags.Instance);
            AssetBundleSettings.PublishPath outputPath = (AssetBundleSettings.PublishPath)outputPathInfo.GetValue(asset);
            outputPath.pathType = (AssetBundleSettings.PublishPathType)EditorGUILayout.EnumPopup(new GUIContent("Path Type", "输出路径类型"), outputPath.pathType);
            if (outputPath.pathType == AssetBundleSettings.PublishPathType.Custom)
            {
                outputPath.custom = EditorGUILayout.TextField(new GUIContent("Path", "自定义输出路径"), outputPath.custom);
            }
            outputPath.subpath = EditorGUILayout.TextField(new GUIContent("Subpath", "叠加输出路径"), outputPath.subpath);
            EditorGUILayout.HelpBox(help, MessageType.None);
            //Resource Folders
            Rect rect = EditorGUILayout.GetControlRect(true, 50 + (resourceFoldersList.count) * 20);
            resourceFoldersList.DoList(EditorGUI.IndentedRect(rect));
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("HelpBox");
            //Read Path
            EditorGUILayout.LabelField("Read", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            FieldInfo readPathInfo = assetType.GetField("readPath", BindingFlags.NonPublic | BindingFlags.Instance);
            AssetBundleSettings.PublishPath readPath = (AssetBundleSettings.PublishPath)readPathInfo.GetValue(asset);
            readPath.pathType = (AssetBundleSettings.PublishPathType)EditorGUILayout.EnumPopup(new GUIContent("Path Type", "读取路径类型"), readPath.pathType);
            if (readPath.pathType == AssetBundleSettings.PublishPathType.Custom)
            {
                readPath.custom = EditorGUILayout.TextField(new GUIContent("Path", "自定义读取路径"), readPath.custom);
            }
            readPath.subpath = EditorGUILayout.TextField(new GUIContent("Subpath", "叠加读取路径"), readPath.subpath);
            EditorGUILayout.HelpBox(help, MessageType.None);
            //Use Web Request
            asset.useWebRequest = EditorGUILayout.Toggle(new GUIContent("Use Web Request", "使用WebRequest方式读取，在某些平台下必选（WebGL,Android,IOS等）"), asset.useWebRequest);
            //Simulate In Editor
            asset.simulateInEditor = EditorGUILayout.Toggle(new GUIContent("Simulate In Editor", "使用编辑器API模拟加载asset和scene而不是真正的加载assetbundle"), asset.simulateInEditor);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical("HelpBox");
            //Log
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            asset.logCommon = EditorGUILayout.Toggle(new GUIContent("LogCommon", "打印控制台信息"), asset.logCommon);
            asset.logError = EditorGUILayout.Toggle(new GUIContent("LogError", "打印控制台错误信息"), asset.logError);
            asset.logWarning = EditorGUILayout.Toggle(new GUIContent("LogWarning", "打印控制台警告信息"), asset.logWarning);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawBuildButton()
        {
            if (GUILayout.Button(new GUIContent("Build", "Build asset bundle"), EditorStyles.miniButton))
            {
                AssetBundleBuilder.Build();
            }
        }

        private void OnEnable()
        {
            Init();
        }

        public override void OnInspectorGUI()
        {
            Draw();
            DrawBuildButton();
        }
    }

    public class AssetBundleBuildConfigWindow : EditorWindow
    {
        [MenuItem("Assets/Bundle/Settings")]
        private static void CreateWindow()
        {
            EditorWindow window = GetWindow(typeof(AssetBundleBuildConfigWindow), false, "Asset Bundle Settings", true);
            window.minSize = new Vector2(300f, 400f);
        }

        private void OnEnable()
        {
            AssetBundleSettingsEditor.Init();
        }

        private Vector2 scrollPos;

        private void DrawBuildButton()
        {
            //GUILayout.BeginArea(new Rect(position.width-120,position.height-25,100,30));
            if (GUILayout.Button(new GUIContent("Build", "Build asset bundle"), EditorStyles.miniButton))
            {
                AssetBundleBuilder.Build();
            }
            //GUILayout.EndArea();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));
            AssetBundleSettingsEditor.Draw();
            DrawBuildButton();
            EditorGUILayout.EndScrollView();
        }
    }
}
