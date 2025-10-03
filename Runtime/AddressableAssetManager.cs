using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets; 
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressableAssetKit.Runtime
{
    public class AddressableAssetManager : IDisposable
    {
        private readonly Dictionary<string, AsyncOperationHandle> _loadedAssets = new();
        private readonly Dictionary<string, int> _assetRefCounts = new();

        public void Dispose()
        {
            ReleaseAllAssets();
        }

        #region Asset Loading API

        public async UniTask<T> LoadAssetAsync<T>(string key, CancellationToken ct = default) where T : UnityEngine.Object
        {
            if (_loadedAssets.TryGetValue(key, out var existingHandle))
            {
                _assetRefCounts[key]++;
                return existingHandle.Result as T;
            }

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
    }
}