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
        public class Factory
        {
            private Factory() { }

            private static Factory instance;

            public static Factory Instance
            {
                get
                {
                    if(instance == null)
                    {
                        instance = new Factory();
                    }
                    return instance;
                }
            }

            private AssetBundleSettings asset;
            private Type assetType;
            private ReorderableList resourceFoldersList;
            private readonly string help0 =
                "Use %APPLICATION_PATH% in path and it will be replaced with Environment.CurrentDirectory;" + Environment.NewLine +
                "Use %COMPANY_NAME% in path and it will be replaced with Company Name in player settings;" + Environment.NewLine +
                "Use %PRODUCT_NAME% in path and it will be replaced with Product Name in player settings." + Environment.NewLine + Environment.NewLine +
                "Assets under Resources folder will not be included in asset bundle.";
            private readonly string help1 =
                "Use %COMPANY_NAME% in path and it will be replaced with Company Name in player settings;" + Environment.NewLine +
                "Use %PRODUCT_NAME% in path and it will be replaced with Product Name in player settings.";

            private readonly Queue<Action> commands = new Queue<Action>();

            public void AddCommand(Action command)
            {
                commands.Enqueue(command);
            }

            public void ExecuteCommands()
            {
                if(commands.Count > 0)
                {
                    commands.Dequeue()?.Invoke();
                }
            }

            public void Init()
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
                    int selIndex = 0;
                    resourceFoldersList = new ReorderableList(resourceFolders, typeof(AssetBundleSettings.ResourceFolder), true, true, true, true)
                    {
                        drawHeaderCallback = (rect) =>
                        EditorGUI.LabelField(rect, new GUIContent("Resource Folders", "资源文件")),
                        drawElementCallback = (rect, index, isActive, isFocused) =>
                        {
                            AssetBundleSettings.ResourceFolder resourceFolder = resourceFolders[index];
                            if (selIndex == index)
                            {
                                resourceFolder.path = EditorGUI.TextField(rect, resourceFolder.path);
                            }
                            else
                            {
                                EditorGUI.LabelField(rect, resourceFolder.path);
                            }
                        },
                        onSelectCallback = (ReorderableList list) => 
                        {
                            if(selIndex == -1)
                            {
                                selIndex = list.index;
                            }
                            else if(selIndex != list.index)
                            {
                                selIndex = -1;
                            }                         
                        }
                    };
                }
            }

            public void Draw()
            {
                EditorGUILayout.BeginVertical("HelpBox");
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
                outputPathInfo.SetValue(asset, 
                    EditorGUILayout.TextField(new GUIContent("Path", "输出路径"), 
                    (string)outputPathInfo.GetValue(asset)));
                EditorGUILayout.HelpBox(help0, MessageType.None);
                //Resource Folders
                Rect rect = EditorGUILayout.GetControlRect(true, 50 + (resourceFoldersList.count > 0 ? resourceFoldersList.count : 1) * 20);
                resourceFoldersList.DoList(EditorGUI.IndentedRect(rect));
                if (GUILayout.Button(new GUIContent("Build", "Build asset bundle"), EditorStyles.miniButton))
                {
                    AddCommand(() =>
                    {
                        AssetBundleBuilder.Build();
                    });
                }
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
                EditorGUILayout.HelpBox(help1, MessageType.None);
                //Use Web Request
                asset.useWebRequest = EditorGUILayout.Toggle(new GUIContent("Use Web Request", "使用WebRequest方式读取，在某些平台下必选（WebGL,Android,IOS等）"), asset.useWebRequest);
                //Simulate In Editor
                asset.simulateInEditor = EditorGUILayout.Toggle(new GUIContent("Simulate In Editor", "使用编辑器API模拟加载asset和scene而不是真正的加载assetbundle"), asset.simulateInEditor);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical("HelpBox");
                //Console Log
                EditorGUILayout.LabelField("Console Log", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                asset.logCommon = EditorGUILayout.Toggle(new GUIContent("Common", "打印控制台普通信息"), asset.logCommon);
                asset.logError = EditorGUILayout.Toggle(new GUIContent("Error", "打印控制台错误信息"), asset.logError);
                asset.logWarning = EditorGUILayout.Toggle(new GUIContent("Warning", "打印控制台警告信息"), asset.logWarning);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }
        }
        
        private void OnEnable()
        {
            Factory.Instance.Init();
        }

        public override void OnInspectorGUI()
        {
            Factory.Instance.Draw();
            Factory.Instance.ExecuteCommands();
        }
    }

    public class AssetBundleSettingsWindow : EditorWindow
    {
        [MenuItem("Assets/Bundle/Settings")]
        private static void CreateWindow()
        {
            EditorWindow window = GetWindow(typeof(AssetBundleSettingsWindow), false, "Asset Bundle Settings", true);
            window.minSize = new Vector2(300f, 400f);
        }

        private void OnEnable()
        {
            AssetBundleSettingsEditor.Factory.Instance.Init();
        }

        private Vector2 scrollPos;

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height));
            AssetBundleSettingsEditor.Factory.Instance.Draw();
            EditorGUILayout.EndScrollView();
            AssetBundleSettingsEditor.Factory.Instance.ExecuteCommands();
        }
    }
}
