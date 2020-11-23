/**
 *  Author ZhanQI
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace E
{
    public static class AssetBundleLoader
    {
        /// <summary>
        /// Load an asset from asset bundle asynchronously in published project 
        /// or just load this asset directly in editor without load any asset bundle
        /// </summary>
        /// <typeparam name="T">The type of this asset based on UnityEngine.Object</typeparam>
        /// <param name="path">Direct subpath of "Assets" folder like "Res/Prefabs/Player.prefab"</param>
        /// <param name="callback">Callback at the end, null parameter if loading faild</param>
        public static void LoadAsset<T>(string path, System.Action<T> callback) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (AssetBundleSettings.Instance.simulateInEditor)
            {
                LoadAssetInEditor(path, callback);
            }
            else
            {
                LoadAssetFromAssetBundle(path, callback);
            }
#else
            LoadAssetFromAssetBundle(path, callback);
#endif
        }

        /// <summary>
        /// Load an asset from asset bundle asynchronously in published project 
        /// or just load this asset directly in editor without load any asset bundle
        /// </summary>
        /// <param name="path">Direct subpath of "Assets" folder like "Res/Prefabs/Player.prefab"</param>
        /// <param name="type">The type of this asset based on UnityEngine.Object</param>
        /// <param name="callback">Callback at the end, null parameter if loading faild</param>
        public static void LoadAsset(string path, Type type, System.Action<UnityEngine.Object> callback)
        {
#if UNITY_EDITOR
            if (AssetBundleSettings.Instance.simulateInEditor)
            {
                LoadAssetInEditor(path, type, callback);
            }
            else
            {
                LoadAssetFromAssetBundle(path, type, callback);
            }
#else
            LoadAssetFromAssetBundle(path, type, callback);
#endif
        }

        /// <summary>
        /// Load all assets from asset bundle asynchronously in published project 
        /// or just load these asset directly in editor without load any asset bundle
        /// </summary>
        /// <param name="path">Direct subpath of "Assets" folder like "Res/Prefabs",
        /// each non empty folder is an asset bundle</param>
        /// <param name="callback">Callback at the end</param>
        public static void LoadAllAsset(string path, System.Action<UnityEngine.Object[]> callback)
        {
#if UNITY_EDITOR
            if (AssetBundleSettings.Instance.simulateInEditor)
            {
                LoadAllAssetInEditor(path, callback);
            }
            else
            {
                LoadAllAssetFromAssetBundle(path, callback);
            }
#else
            LoadAllAssetFromAssetBundle(path, callback);
#endif
        }

#if UNITY_EDITOR
        private static void LoadAssetInEditor<T>(string path, System.Action<T> callback) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(Path.Combine("Assets", path));
            if (asset == null)
            {
                string assetName = AssetBundlePath.FileToAssetName(path);
                AssetBundleLoaderDebug.LogError("Asset loading faild in editor" + Environment.NewLine +
                    "path: " + path + Environment.NewLine +
                    "asset name: " + assetName);
            }
            callback?.Invoke(asset);
        }

        private static void LoadAssetInEditor(string path, Type type, System.Action<UnityEngine.Object> callback)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(Path.Combine("Assets", path), type);
            if (asset == null)
            {
                string assetName = AssetBundlePath.FileToAssetName(path);
                AssetBundleLoaderDebug.LogError("Asset loading faild in editor" + Environment.NewLine +
                    "path: " + path + Environment.NewLine +
                    "asset name: " + assetName);
            }
            callback?.Invoke(asset);
        }

        private static void LoadAllAssetInEditor(string path, System.Action<UnityEngine.Object[]> callback)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(Path.Combine("Assets", path));
            callback?.Invoke(assets);
        }

#endif

        private static void LoadAssetFromAssetBundle<T>(string path, System.Action<T> callback) where T : UnityEngine.Object
        {
            string bundleName = AssetBundlePath.FileToBundleName(path);
            LoadAssetBundle(bundleName, (AssetBundle bundle) =>
            {
                string assetName = AssetBundlePath.FileToAssetName(path);
                AssetBundleRequest request = bundle.LoadAssetAsync<T>(assetName);
                request.completed += (AsyncOperation operation) => 
                {
                    if (request.asset == null)
                    {
                        AssetBundleLoaderDebug.LogError("Asset loading faild in bundle" + Environment.NewLine +
                            "bundle name: " + bundleName + Environment.NewLine +
                            "asset name: " + assetName);
                    }
                    callback?.Invoke(request.asset as T);
                };
            });
        }

        private static void LoadAssetFromAssetBundle(string path, Type type, System.Action<UnityEngine.Object> callback)
        {
            string bundleName = AssetBundlePath.FileToBundleName(path);
            LoadAssetBundle(bundleName, (AssetBundle bundle) =>
            {
                string assetName = AssetBundlePath.FileToAssetName(path);
                AssetBundleRequest request = bundle.LoadAssetAsync(assetName, type);
                request.completed += (AsyncOperation operation) =>
                {
                    if (request.asset == null)
                    {
                        AssetBundleLoaderDebug.LogError("Asset loading faild in bundle" + Environment.NewLine +
                            "bundle name: " + bundleName + Environment.NewLine +
                            "asset name: " + assetName);
                    }
                    callback?.Invoke(request.asset);
                };
            });
        }

        private static void LoadAllAssetFromAssetBundle(string path, System.Action<UnityEngine.Object[]> callback)
        {
            string bundleName = AssetBundlePath.FileToBundleName(path);
            LoadAssetBundle(bundleName, (AssetBundle bundle) =>
            {
                AssetBundleRequest request = bundle.LoadAllAssetsAsync();
                request.completed += (AsyncOperation operation) =>
                {
                    callback?.Invoke(request.allAssets);
                };
            });
        }

        /// <summary>
        /// Load a scene from asset bundle asynchronously in published project 
        /// or just load this scene directly in editor without load any asset bundle
        /// </summary>
        /// <param name="path">Direct subpath of "Assets" folder like "Res/Scenes/scene01"</param>
        /// <param name="callback">Callback at the end, default parameter if loading faild</param>
        /// <param name="loadSceneMode">Closes all current loaded Scenes and load this one or adds this scene to the current loaded scenes</param>
        public static void LoadScene(string path, System.Action<Scene> callback, LoadSceneMode loadSceneMode = LoadSceneMode.Additive)
        {
#if UNITY_EDITOR
            if (AssetBundleSettings.Instance.simulateInEditor)
            {
                LoadSceneInEditor(path, callback, loadSceneMode);
            }
            else
            {
                LoadSceneFromAssetBundle(path, callback, loadSceneMode);
            }
#else
            LoadSceneFromAssetBundle(path, callback, loadSceneMode);
#endif
        }

#if UNITY_EDITOR
        private static void LoadSceneInEditor(string path, System.Action<Scene> callback, LoadSceneMode loadSceneMode)
        {
            Scene scene = SceneManager.GetSceneByName(AssetBundlePath.FileToAssetName(path));
            if (!path.EndsWith(".unity"))
            {
                path += ".unity";
            }
            path = Path.Combine("Assets", path);
            if (scene == default)
            {
                if (Application.isPlaying)
                {
                    LoadSceneByPathInEditorPlayMode(path, callback, loadSceneMode);
                }
                else
                {
                    LoadSceneByPathInEditor(path, callback, loadSceneMode);
                }
            }
            else
            {
                SceneManager.SetActiveScene(scene);
                callback?.Invoke(scene);
            }
        }

        private static void LoadSceneByPathInEditorPlayMode(string path, System.Action<Scene> callback, LoadSceneMode loadSceneMode)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(loadSceneMode))
            .completed += (AsyncOperation request) =>
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                if (scene != default)
                {
                    AssetBundleLoaderDebug.Log("Scene loading succeeded " + scene.name);
                    SceneManager.SetActiveScene(scene);
                    callback?.Invoke(scene);
                }
                else
                {
                    AssetBundleLoaderDebug.LogError("Scene loading faild " + AssetBundlePath.FileToAssetName(path).Replace(".unity", ""));
                    callback?.Invoke(default);
                }
            };
        }

        private static void LoadSceneByPathInEditor(string path, System.Action<Scene> callback, LoadSceneMode loadSceneMode)
        {
            Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, (UnityEditor.SceneManagement.OpenSceneMode)loadSceneMode);
            if (scene != default)
            {
                AssetBundleLoaderDebug.Log("Scene loading succeeded " + scene.name);
                SceneManager.SetActiveScene(scene);
                callback?.Invoke(scene);
            }
            else
            {
                AssetBundleLoaderDebug.LogError("Scene loading faild " + AssetBundlePath.FileToAssetName(path).Replace(".unity", ""));
                callback?.Invoke(default);
            }
        }
#endif

        private static void LoadSceneFromAssetBundle(string path, System.Action<Scene> callback, LoadSceneMode loadSceneMode)
        {
            string sceneName = AssetBundlePath.FileToAssetName(path);
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene == default)
            {
                string bundleName = AssetBundlePath.FileToBundleName(path);
                LoadAssetBundle(bundleName, (AssetBundle bundle) =>
                {
                    if (bundle != null)
                    {
                        LoadSceneByName(sceneName, callback, loadSceneMode);
                    }
                    else
                    {
                        AssetBundleLoaderDebug.LogError("Scene loading faild " + sceneName);
                        callback?.Invoke(default);
                    }
                });
            }
            else
            {
                SceneManager.SetActiveScene(scene);
                callback?.Invoke(scene);
            }
        }

        private static void LoadSceneByName(string sceneName, System.Action<Scene> callback, LoadSceneMode loadSceneMode)
        {
            SceneManager.LoadSceneAsync(sceneName, new LoadSceneParameters(loadSceneMode))
            .completed += (AsyncOperation operation) =>
            {
                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (scene != default)
                {
                    AssetBundleLoaderDebug.Log("Scene loading succeeded " + sceneName);
                    SceneManager.SetActiveScene(scene);
                    callback?.Invoke(scene);
                }
                else
                {
                    AssetBundleLoaderDebug.LogError("Scene loading faild " + sceneName);
                    callback?.Invoke(default);
                }
            };
        }

        /// <summary>
        /// Load an asset bundle builded by E.Editor.AssetBundleBuilder
        /// </summary>
        /// <param name="path">Direct subpath of "Assets" folder like "Res/Prefabs", 
        /// each non empty folder is an asset bundle</param>
        /// <param name="callback">Callback at the end, null parameter if loading faild</param>
        public static void LoadAssetBundle(string path, System.Action<AssetBundle> callback)
        {
            string bundleName = AssetBundlePath.DirectoryToBundleName(path);
            if (LoadingBundle.TryGetLoaded(bundleName, out AssetBundle bundle))
            {
                callback?.Invoke(bundle);
                return;
            }
            LoadingManifest.GetManifest((AssetBundleManifest manifest) =>
            {
                string[] dependencies = manifest.GetAllDependencies(bundleName);
                int count = dependencies.Length;
                LoadingBundle.GetLoadingBundle(bundleName).InitProgress(count);
                if (dependencies != null && count > 0)
                {
                    List<LoadingBundle> deps = new List<LoadingBundle>();
                    foreach (string dependBundleName in dependencies)
                    {
                        deps.Add(LoadingBundle.GetLoadingBundle(dependBundleName));
                        LoadAssetBundle(dependBundleName, (AssetBundle dependBundle) =>
                        {
                            lock (dependencies)
                            {
                                --count;
                            }
                            if (count == 0)
                            {
                                LoadingBundle.EnterLoader(bundleName, callback, deps);
                            }
                        });
                    }
                }
                else
                {
                    LoadingBundle.EnterLoader(bundleName, callback);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">Direct subpath of "Assets" folder like "Res/Prefabs", 
        /// each non empty folder is an asset bundle</param>
        /// <param name="bundle"></param>
        /// <returns>true if this asset bundle loaded</returns>
        public static bool TryGetLoadedBundle(string path, out AssetBundle bundle)
        {
            string bundleName = AssetBundlePath.DirectoryToBundleName(path);
            return LoadingBundle.TryGetLoaded(bundleName, out bundle);
        }

        /// <summary>
        /// Unload an asset bundle
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="unloadAllLoadedObjects">Unload all Object from this asset bundle</param>
        /// <param name="unloadDependencies">Unload unused dependent asset bundles</param>
        public static void UnloadAssetBundle(this AssetBundle bundle, bool unloadAllLoadedObjects = true, bool unloadDependencies = true)
        {
            LoadingBundle.ReleaseLoader(bundle.name, unloadAllLoadedObjects, unloadDependencies);
        }

        /// <summary>
        /// Unload an asset bundle by path
        /// </summary>
        /// <param name="path">Direct subpath of "Assets" folder like "Res/Prefabs", 
        /// each non empty folder is an asset bundle</param>
        /// <param name="unloadAllLoadedObjects">Unload all Object from this asset bundle</param>
        /// <param name="unloadDependencies">Unload unused dependent asset bundles</param>
        public static void UnloadAssetBundle(string path, bool unloadAllLoadedObjects = true, bool unloadDependencies = true)
        {
            string bundleName = AssetBundlePath.DirectoryToBundleName(path);
            LoadingBundle.ReleaseLoader(bundleName, unloadAllLoadedObjects, unloadDependencies);
        }

        /// <summary>
        /// Unload all asset bundles
        /// </summary>
        /// <param name="unloadAllLoadedObjects">Unload all Object from this asset bundle</param>
        public static void UnloadAllAssetBundles(bool unloadAllLoadedObjects = true)
        {
            LoadingBundle.ReleaseAllLoaders(unloadAllLoadedObjects);
        }

        /// <summary>
        /// Get an asset bundle loading state
        /// </summary>
        /// <param name="path">Direct subpath of "Assets" folder like "Res/Prefabs", 
        /// each non empty folder is an asset bundle</param>
        /// <returns></returns>
        public static State GetBundleState(string path)
        {
            string bundleName = AssetBundlePath.DirectoryToBundleName(path);
            return LoadingBundle.GetLoadingBundle(bundleName).GetState();
        }

        public abstract class State
        {
            protected abstract float GetProgress();

            public float progress 
            {
                get
                {
                    return GetProgress();
                }
            }
        }

        private class LoadingBundle
        {
            private static readonly Dictionary<string, LoadingBundle> loadingBundleMap = new Dictionary<string, LoadingBundle>();

            public static LoadingBundle GetLoadingBundle(string bundleName)
            {
                lock (loadingBundleMap)
                {
                    if (loadingBundleMap.TryGetValue(bundleName, out LoadingBundle val))
                    {
                        return val;
                    }
                    return loadingBundleMap[bundleName] = new LoadingBundle() { name = bundleName };
                }
            }

            public static void SetDependenciesCount(string bundleName, int count)
            {
                LoadingBundle loadingBundle = GetLoadingBundle(bundleName);
                
            }

            public static void EnterLoader(string bundleName, System.Action<AssetBundle> callback, List<LoadingBundle> dependencies = null)
            {
                LoadingBundle loadingBundle = GetLoadingBundle(bundleName);
                if(dependencies != null)
                {
                    loadingBundle.SetDependencies(dependencies);
                }
                loadingBundle.AddCallback(callback);
            }

            public static void ReleaseLoader(string bundleName, bool unloadAllLoadedObjects, bool unloadDependencies)
            {
                lock (loadingBundleMap)
                {
                    if(loadingBundleMap.TryGetValue(bundleName, out LoadingBundle val))
                    {
                        val.Unload(unloadAllLoadedObjects, unloadDependencies, true);
                    }
                }
            }

            public static void ReleaseAllLoaders(bool unloadAllLoadedObjects)
            {
                lock (loadingBundleMap)
                {
                    string[] bundleNames = loadingBundleMap.Keys.ToArray();
                    foreach (string bundleName in bundleNames)
                    {
                        LoadingBundle loadingBundle = loadingBundleMap[bundleName];
                        loadingBundle.Unload(unloadAllLoadedObjects, false, false, true);
                    }
                }
            }

            public static bool TryGetLoaded(string bundleName, out AssetBundle bundle)
            {
                lock (loadingBundleMap)
                {
                    bundle = null;
                    if (loadingBundleMap.TryGetValue(bundleName, out LoadingBundle val))
                    {
                        if(val.assetBundle != null)
                        {
                            bundle = val.assetBundle;
                            return true;
                        }
                    }
                    return false;
                }
            }

            private static void RemoveFromMap(string bundleName)
            {
                lock (loadingBundleMap)
                {
                    loadingBundleMap.Remove(bundleName);
                }                    
            }

            private string name;

            private System.Action<AssetBundle> callbacks;

            private bool loading = false;

            #region progress
            private class EState : State
            {
                public LoadingBundle body;

                protected override float GetProgress()
                {
                    return body.GetProgress();
                }
            }

            private EState state;

            public State GetState()
            {
                if(state == null)
                {
                    state = new EState { body = this };
                }
                return state;
            }

            private int dependenciesCount = 0;

            private UnityWebRequest webRequest;

            private AssetBundleCreateRequest createRequest;

            private float GetProgress()
            {
                float value = GetSelfProgress() / (dependenciesCount + 1);
                if (dependencies != null)
                {
                    foreach (LoadingBundle lb in dependencies)
                    {
                        value += lb.GetProgress() / (dependenciesCount + 1);
                    }
                }
                return value;
            }

            public void InitProgress(int dependenciesCount)
            {
                this.dependenciesCount = dependenciesCount;
            }

            private float GetSelfProgress()
            {
                if(assetBundle != null)
                {
                    return 1;
                }
                else
                {
                    if (loading)
                    {
                        if(webRequest != null)
                        {
                            return webRequest.downloadProgress;
                        }
                        if(createRequest != null)
                        {
                            return createRequest.progress;
                        }
                    }
                }
                return 0;
            }
            #endregion

            private AssetBundle assetBundle;

            private LinkedList<LoadingBundle> beDependencies = new LinkedList<LoadingBundle>();

            private LoadingBundle[] dependencies;

            private int AddBeDependent(LoadingBundle beDependent)
            {
                lock (this)
                {
                    beDependencies.AddLast(beDependent);
                    return beDependencies.Count;
                }
            }

            private int RemoveBeDependent(LoadingBundle beDependent)
            {
                lock (this)
                {
                    beDependencies.Remove(beDependent);
                    return beDependencies.Count;
                }
            }

            private void SetDependencies(List<LoadingBundle> dependencies)
            {
                lock (this)
                {
                    this.dependencies = dependencies.ToArray();
                    foreach (LoadingBundle dep in dependencies)
                    {
                        dep.AddBeDependent(this);
                    }
                }
            }

            private void Unload(bool unloadAllLoadedObjects, bool unloadDependencies, bool warning = false, bool force = false)
            {
                lock (this)
                {
                    if(beDependencies.Count == 0 || force)
                    {
                        if(assetBundle != null)
                        {
                            assetBundle.Unload(unloadAllLoadedObjects);
                        }
                        if (dependencies != null)
                        {
                            foreach (LoadingBundle dep in dependencies)
                            {
                                dep.RemoveBeDependent(this);
                                if (unloadDependencies)
                                {
                                    dep.Unload(unloadAllLoadedObjects, unloadDependencies);
                                }
                            }
                        }
                        RemoveFromMap(name);
                    }
                    else
                    {
                        if (warning)
                        {
                            StringBuilder warningStr = new StringBuilder();
                            foreach (LoadingBundle lb in beDependencies)
                            {
                                warningStr.AppendLine(lb.name);
                            }
                            AssetBundleLoaderDebug.LogWarning("This asset bundle(" + name + ") is depended by other asset bundles, please unload these asset bundles first: " + Environment.NewLine + warningStr.ToString());
                        }
                    }
                }
            }

            private void AddCallback(System.Action<AssetBundle> callback)
            {
                lock (this)
                {
                    callbacks -= callback;
                    callbacks += callback;
                    Execute();
                }
            }

            private void Execute()
            {
                if (assetBundle != null)
                {
                    DoCallbacks(assetBundle);
                }
                else
                {
                    if (!loading)
                    {
                        loading = true;
                        Load();
                    }
                }
            }

            private void Load()
            {
                if (AssetBundleSettings.Instance.useWebRequest)
                {
                    LoadByWebRequest();
                }
                else
                {
                    LoadFromFile();
                }
            }

            private void DoCallbacks(AssetBundle bundle)
            {
                lock (this)
                {
                    if(bundle != null)
                    {
                        loading = false;
                        assetBundle = bundle;
                        System.Action<AssetBundle> backup = callbacks;
                        callbacks = null;
                        backup?.Invoke(bundle);
                    }
                    else
                    {
                        loading = false;
                        assetBundle = bundle;
                        System.Action<AssetBundle> backup = callbacks;
                        callbacks = null;
                        backup?.Invoke(null);
                    }
                }
            }

            private void LoadFromFile()
            {
                string bundlePath = AssetBundlePath.BundleNameToBundlePath(name);
                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
                createRequest = request;
                request.completed += (AsyncOperation asyncOperation) =>
                {
                    AssetBundle bundle = request.assetBundle;
                    if (bundle != null)
                    {
                        AssetBundleLoaderDebug.Log("AssetBundle loading succeeded: " + name);
                        DoCallbacks(bundle);
                    }
                    else
                    {
                        AssetBundleLoaderDebug.LogError("AssetBundle loading faild:" + name + Environment.NewLine +
                            "path: " + bundlePath + Environment.NewLine +
                            "request.assetBundle is null");
                        DoCallbacks(null);
                    }
                    createRequest = null;
                };
            }

            private void LoadByWebRequest()
            {
                LoadingManifest.GetManifest((AssetBundleManifest manifest) => 
                {
                    Hash128 version = manifest.GetAssetBundleHash(name);
                    string bundlePath = AssetBundlePath.BundleNameToBundlePath(name);
                    UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(bundlePath, version);
                    webRequest = request;
                    request.SendWebRequest().completed += (AsyncOperation operation) =>
                    {
                        if (request.isHttpError || request.isNetworkError)
                        {
                            AssetBundleLoaderDebug.LogError("AssetBundle loading faild:" + name + Environment.NewLine +
                                "url: " + request.url + Environment.NewLine +
                                "response code: " + request.responseCode +
                                request.error);
                            DoCallbacks(null);
                        }
                        else
                        {
                            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
                            if (bundle != null)
                            {
                                AssetBundleLoaderDebug.Log("AssetBundle loading succeeded: " + name);
                                DoCallbacks(bundle);
                            }
                            else
                            {
                                AssetBundleLoaderDebug.LogError("AssetBundle loading faild:" + name + Environment.NewLine +
                                    "url: " + bundlePath + Environment.NewLine +
                                    "DownloadHandlerAssetBundle.GetContent(request) return null");
                                DoCallbacks(null);
                            }
                        }
                        request.Dispose();
                        webRequest = null;
                    };
                });
            }
        }
    
        private class LoadingManifest
        {
            private static AssetBundleManifest manifest;

            private static System.Action<AssetBundleManifest> callbacks;

            private static readonly object lockObj = new object();

            private static bool loading = false;

            public static void GetManifest(System.Action<AssetBundleManifest> callback)
            {
                if(manifest != null)
                {
                    callback?.Invoke(manifest);
                }
                else
                {
                    lock (lockObj)
                    {
                        callbacks -= callback;
                        callbacks += callback;
                        if (!loading)
                        {
                            loading = true;
                            Load();
                        }
                    }
                }
            }

            private static void Load()
            {
                if (AssetBundleSettings.Instance.useWebRequest)
                {
                    LoadByWebRequest();
                }
                else
                {
                    LoadFromFile();
                }
            }

            private static void LoadFromFile()
            {
                string manifestPath = AssetBundlePath.GetManifestPath();
                AssetBundle bundle = AssetBundle.LoadFromFile(manifestPath);
                if (bundle != null)
                {
                    manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                    bundle.Unload(false);
                    bundle = null;
                    if (manifest == null)
                    {
                        AssetBundleLoaderDebug.LogError("AssetBundleManifest loading faild" + Environment.NewLine +
                            "url: " + manifestPath + Environment.NewLine +
                            "bundle.LoadAsset<AssetBundleManifest>(\"AssetBundleManifest\") return null");
                    }
                    callbacks?.Invoke(manifest);
                }
                else
                {
                    AssetBundleLoaderDebug.LogError("AssetBundleManifest loading faild" + Environment.NewLine +
                        "path: " + manifestPath + Environment.NewLine +
                        "AssetBundle.LoadFromFile(manifestPath) return null");
                    callbacks?.Invoke(null);
                }
            }

            private static void LoadByWebRequest()
            {
                string manifestPath = AssetBundlePath.GetManifestPath();
                UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(manifestPath);
                UnityWebRequestAsyncOperation asyncOperation = request.SendWebRequest();
                asyncOperation.completed += (AsyncOperation operation) =>
                {
                    if (request.isHttpError || request.isNetworkError)
                    {
                        AssetBundleLoaderDebug.LogError("AssetBundleManifest loading faild" + Environment.NewLine +
                            "url: " + request.url + Environment.NewLine +
                            "response code: " + request.responseCode +
                            request.error);
                        callbacks?.Invoke(null);
                    }
                    else
                    {
                        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
                        if (bundle != null)
                        {
                            manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                            bundle.Unload(false);
                            bundle = null;
                            if (manifest == null)
                            {
                                AssetBundleLoaderDebug.LogError("AssetBundleManifest loading faild" + Environment.NewLine +
                                    "url: " + manifestPath + Environment.NewLine +
                                    "bundle.LoadAsset<AssetBundleManifest>(\"AssetBundleManifest\") return null");
                            }
                            callbacks?.Invoke(manifest);
                        }
                        else
                        {
                            AssetBundleLoaderDebug.LogError("AssetBundleManifest loading faild" + Environment.NewLine + 
                                "url: " + manifestPath + Environment.NewLine +
                                "DownloadHandlerAssetBundle.GetContent(request) return null");
                            callbacks?.Invoke(null);
                        }
                    }
                    request.Dispose();
                };
            }
        }
    }
}