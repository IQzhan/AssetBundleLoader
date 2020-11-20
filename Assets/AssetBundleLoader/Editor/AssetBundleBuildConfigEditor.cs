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
    [CustomEditor(typeof(AssetBundleBuildConfig))]
    public class AssetBundleBuildConfigEditor : UnityEditor.Editor
    {
        private AssetBundleBuildConfig asset;
        private Type assetType;
        private ReorderableList resourceFoldersList;
        private readonly string title = "";
        private readonly string desc = @"";

        private void OnEnable()
        {
            asset = (AssetBundleBuildConfig)target;
            assetType = asset.GetType();
            List<AssetBundleBuildConfig.ResourceFolder> resourceFolders = asset.resourceFolders;
            resourceFoldersList = new ReorderableList(resourceFolders, typeof(AssetBundleBuildConfig.ResourceFolder), true, true, true, true)
            {
                drawHeaderCallback = (rect) =>
                EditorGUI.LabelField(rect, "Resource Folders"),
                drawElementCallback = (rect, index, isActive, isFocused) => 
                {
                    AssetBundleBuildConfig.ResourceFolder resourceFolder = resourceFolders[index];
                    if (isActive)
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

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            //Desc
            EditorGUILayout.LabelField(title);
            EditorGUILayout.LabelField(desc);
            //Build Target
            EditorGUI.BeginChangeCheck();
            asset.buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", asset.buildTarget);
            if (EditorGUI.EndChangeCheck())
            {
                FieldInfo buildTargetNameInfo = assetType.GetField("buildTargetName", BindingFlags.NonPublic | BindingFlags.Instance);
                buildTargetNameInfo.SetValue(asset, asset.buildTarget.ToString());
            }
            //Compressed
            asset.compressed = (AssetBundleBuildConfig.Compressed)EditorGUILayout.EnumPopup("Compressed", asset.compressed);
            EditorGUILayout.Separator();
            //Output Path
            FieldInfo outputPathInfo = assetType.GetField("outputPath", BindingFlags.NonPublic | BindingFlags.Instance);
            AssetBundleBuildConfig.PublishPath outputPath = (AssetBundleBuildConfig.PublishPath)outputPathInfo.GetValue(asset);
            outputPath.pathType = (AssetBundleBuildConfig.PublishPathType)EditorGUILayout.EnumPopup("Output Path Type", outputPath.pathType);
            if (outputPath.pathType == AssetBundleBuildConfig.PublishPathType.Custom)
            {
                outputPath.custom = EditorGUILayout.TextField("Output Path", outputPath.custom);
            }
            outputPath.subpath = EditorGUILayout.TextField("Output Subpath", outputPath.subpath);
            EditorGUILayout.Separator();
            //Read Path
            FieldInfo readPathInfo = assetType.GetField("readPath", BindingFlags.NonPublic | BindingFlags.Instance);
            AssetBundleBuildConfig.PublishPath readPath = (AssetBundleBuildConfig.PublishPath)readPathInfo.GetValue(asset);
            readPath.pathType = (AssetBundleBuildConfig.PublishPathType)EditorGUILayout.EnumPopup("Read Path Type", readPath.pathType);
            if (readPath.pathType == AssetBundleBuildConfig.PublishPathType.Custom)
            {
                readPath.custom = EditorGUILayout.TextField("Read Path", readPath.custom);
            }
            readPath.subpath = EditorGUILayout.TextField("Read Subpath", readPath.subpath);
            //Use Web Request
            asset.useWebRequest = EditorGUILayout.Toggle("Use Web Request", asset.useWebRequest);
            //Simulate In Editor
            asset.simulateInEditor = EditorGUILayout.Toggle("Simulate In Editor", asset.simulateInEditor);
            EditorGUILayout.Separator();
            //Resource Folders
            resourceFoldersList.DoLayoutList();
            //Log
            asset.logCommon = EditorGUILayout.Toggle("LogCommon", asset.logCommon);
            asset.logError = EditorGUILayout.Toggle("LogError", asset.logError);
            asset.logWarning = EditorGUILayout.Toggle("LogWarning", asset.logWarning);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
