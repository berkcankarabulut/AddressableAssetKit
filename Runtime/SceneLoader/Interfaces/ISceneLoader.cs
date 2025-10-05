using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace AddressableAssetKit.Runtime.SceneLoader.Interfaces
{ 
    public interface ISceneLoader
    { 
        IObservable<float> OnLoadProgress { get; }
 
        UniTask LoadSceneAsync(AssetReference sceneReference, CancellationToken ct = default);
 
        UniTask UnloadSceneAsync(CancellationToken ct = default);
    }
}