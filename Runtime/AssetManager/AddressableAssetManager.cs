using System;
using System.Collections.Generic;
using System.Threading;
using AddressableAssetKit.Runtime.Download.Services;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UniRx;

namespace AddressableAssetKit.Runtime.Manager
{
    public class AddressableAssetManager : IDisposable
    {
        private readonly DownloadService _downloadService;
        private readonly Dictionary<string, AsyncOperationHandle> _loadedAssets = new();
        private readonly Dictionary<string, int> _assetRefCounts = new();

        private readonly Subject<float> _loadProgressSubject = new();
        public IObservable<float> OnLoadProgress => _loadProgressSubject;

        public AddressableAssetManager(DownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        public void Dispose()
        {
            ReleaseAllAssets();
            _loadProgressSubject?.OnCompleted();
            _loadProgressSubject?.Dispose();
        }

        #region Asset Loading API

        public async UniTask<T> LoadAssetAsync<T>(string key, CancellationToken ct = default) where T : UnityEngine.Object
        {
            // Check if already loaded
            if (_loadedAssets.TryGetValue(key, out var existingHandle))
            {
                _assetRefCounts[key]++;
                return existingHandle.Result as T;
            }

            // 1. Check if needs download
            await EnsureDownloadedAsync(key, ct);

            // 2. Load asset
            var handle = Addressables.LoadAssetAsync<T>(key);

            try
            {
                await handle.ToUniTask(cancellationToken: ct);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _loadedAssets[key] = handle;
                    _assetRefCounts[key] = 1;
                    Debug.Log($"✓ Loaded: {key}");
                    return handle.Result;
                }

                throw new Exception($"Failed to load: {key}");
            }
            catch
            {
                Addressables.Release(handle);
                throw;
            }
        }

        public async UniTask<List<T>> LoadAssetsAsync<T>(List<string> keys, CancellationToken ct = default) where T : UnityEngine.Object
        {
            var results = new List<T>();

            foreach (var key in keys)
            {
                var asset = await LoadAssetAsync<T>(key, ct);
                results.Add(asset);
            }

            return results;
        }

        public async UniTask<List<T>> LoadAssetsByLabelAsync<T>(string label, CancellationToken ct = default) where T : UnityEngine.Object
        {
            // 1. Check if needs download
            await EnsureDownloadedAsync(label, ct);

            // 2. Load assets
            var handle = Addressables.LoadAssetsAsync<T>(label, null);

            try
            {
                await handle.ToUniTask(cancellationToken: ct);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _loadedAssets[label] = handle;
                    _assetRefCounts[label] = 1;
                    Debug.Log($"✓ Loaded label: {label} ({handle.Result.Count} assets)");
                    return new List<T>(handle.Result);
                }

                throw new Exception($"Failed to load label: {label}");
            }
            catch
            {
                Addressables.Release(handle);
                throw;
            }
        }

        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null, CancellationToken ct = default)
        {
            // 1. Check if needs download
            await EnsureDownloadedAsync(key, ct);

            // 2. Instantiate
            var handle = Addressables.InstantiateAsync(key, parent);

            try
            {
                await handle.ToUniTask(cancellationToken: ct);

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var instanceKey = $"{key}_instance_{handle.Result.GetInstanceID()}";
                    _loadedAssets[instanceKey] = handle;
                    Debug.Log($"✓ Instantiated: {key}");
                    return handle.Result;
                }

                throw new Exception($"Failed to instantiate: {key}");
            }
            catch
            {
                Addressables.Release(handle);
                throw;
            }
        }

        public async UniTask<AsyncOperationHandle<SceneInstance>> LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, CancellationToken ct = default)
        {
            // 1. Check if needs download
            await EnsureDownloadedAsync(sceneName, ct);

            // 2. Load scene
            Debug.Log($"Loading scene: {sceneName}");
            var handle = Addressables.LoadSceneAsync(sceneName, mode);

            // Track progress
            while (!handle.IsDone && !ct.IsCancellationRequested)
            {
                _loadProgressSubject.OnNext(handle.PercentComplete);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadProgressSubject.OnNext(1f);
                Debug.Log($"✓ Scene loaded: {sceneName}");
                return handle;
            }

            throw new Exception($"Failed to load scene: {sceneName}");
        }

        public async UniTask<AsyncOperationHandle<SceneInstance>> LoadSceneAsync(AssetReference sceneReference, LoadSceneMode mode = LoadSceneMode.Single, CancellationToken ct = default)
        {
            if (sceneReference == null || !sceneReference.RuntimeKeyIsValid())
            {
                throw new ArgumentException("Invalid scene reference");
            }

            // 1. Check if needs download
            await EnsureDownloadedAsync(sceneReference.AssetGUID, ct);

            // 2. Load scene
            Debug.Log($"Loading scene: {sceneReference.AssetGUID}");
            var handle = sceneReference.LoadSceneAsync(mode);

            // Track progress
            while (!handle.IsDone && !ct.IsCancellationRequested)
            {
                _loadProgressSubject.OnNext(handle.PercentComplete);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            await handle.ToUniTask(cancellationToken: ct);

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadProgressSubject.OnNext(1f);
                Debug.Log($"✓ Scene loaded: {sceneReference.AssetGUID}");
                return handle;
            }

            throw new Exception($"Failed to load scene: {sceneReference.AssetGUID}");
        }

        public async UniTask UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle, CancellationToken ct = default)
        {
            if (!sceneHandle.IsValid())
            {
                Debug.LogWarning("Invalid scene handle to unload");
                return;
            }

            var unloadHandle = Addressables.UnloadSceneAsync(sceneHandle);
            await unloadHandle.ToUniTask(cancellationToken: ct);

            if (unloadHandle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log("✓ Scene unloaded");
            }
            else
            {
                throw new Exception("Failed to unload scene");
            }
        }

        public T GetLoadedAsset<T>(string key) where T : UnityEngine.Object
        {
            if (_loadedAssets.TryGetValue(key, out var handle))
                return handle.Result as T;

            Debug.LogWarning($"Asset not loaded: {key}");
            return null;
        }

        public bool IsAssetLoaded(string key)
        {
            return _loadedAssets.ContainsKey(key);
        }

        public int GetRefCount(string key)
        {
            return _assetRefCounts.TryGetValue(key, out var count) ? count : 0;
        }

        #endregion

        #region Release API

        public void ReleaseAsset(string key)
        {
            if (!_loadedAssets.TryGetValue(key, out var handle))
                return;

            if (_assetRefCounts.TryGetValue(key, out var count))
            {
                count--;

                if (count <= 0)
                {
                    Addressables.Release(handle);
                    _loadedAssets.Remove(key);
                    _assetRefCounts.Remove(key);
                    Debug.Log($"✓ Released: {key}");
                }
                else
                {
                    _assetRefCounts[key] = count;
                }
            }
        }

        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null) return;

            foreach (var key in _loadedAssets.Keys)
            {
                if (key.Contains($"instance_{instance.GetInstanceID()}"))
                {
                    if (_loadedAssets.TryGetValue(key, out var handle))
                    {
                        Addressables.ReleaseInstance(instance);
                        _loadedAssets.Remove(key);
                        Debug.Log($"✓ Released instance: {key}");
                    }
                    break;
                }
            }
        }

        public void ReleaseAllAssets()
        {
            foreach (var handle in _loadedAssets.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _loadedAssets.Clear();
            _assetRefCounts.Clear();
            Debug.Log("✓ All assets released");
        }

        #endregion

        #region Private Methods

        private async UniTask EnsureDownloadedAsync(string key, CancellationToken ct)
        {
            var size = await _downloadService.CheckDownloadSizeAsync(key, ct);

            if (size > 0)
            {
                Debug.Log($"Downloading: {key} ({size} bytes)");
                await _downloadService.DownloadAsync(key, ct: ct);
            }
        }

        #endregion
    }
}