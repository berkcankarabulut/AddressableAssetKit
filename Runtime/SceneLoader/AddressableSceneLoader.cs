using System;
using System.Threading;
using AddressableAssetKit.Runtime.Manager;
using Cysharp.Threading.Tasks; 
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using AddressableAssetKit.Runtime.SceneLoader.Interfaces;

namespace AddressableAssetKit.Runtime.SceneLoader
{ 
    public class AddressableSceneLoader : ISceneLoader, IDisposable
    {
        private readonly AddressableAssetManager _assetManager;
        private AsyncOperationHandle<SceneInstance> _currentSceneHandle;

        public IObservable<float> OnLoadProgress => _assetManager.OnLoadProgress;

        public AddressableSceneLoader(AddressableAssetManager assetManager)
        { 
            _assetManager = assetManager;
        }

        public async UniTask LoadSceneAsync(AssetReference sceneReference, CancellationToken ct = default)
        {
            if (sceneReference == null || !sceneReference.RuntimeKeyIsValid())
            {
                throw new ArgumentException("Invalid scene reference");
            }

            try
            {
                // AddressableAssetManager handles download + load automatically
                _currentSceneHandle = await _assetManager.LoadSceneAsync(
                    sceneReference,
                    LoadSceneMode.Single,
                    ct
                );

                Debug.Log($"✓ Scene loaded successfully");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Scene loading cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Failed to load scene: {ex.Message}");
                throw;
            }
        }

        public async UniTask UnloadSceneAsync(CancellationToken ct = default)
        {
            if (!_currentSceneHandle.IsValid())
            {
                Debug.LogWarning("No scene to unload");
                return;
            }

            try
            {
                await _assetManager.UnloadSceneAsync(_currentSceneHandle, ct);
                Debug.Log("✓ Scene unloaded successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"✗ Failed to unload scene: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            // No disposables in this class
            // AddressableAssetManager handles its own disposal
        }
    }
}