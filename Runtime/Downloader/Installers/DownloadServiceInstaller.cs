using AddressableAssetKit.Runtime.Download.Interfaces;
using AddressableAssetKit.Runtime.Download.Services;
using ModestTree;
using UnityEngine;
using Zenject;

namespace AddressableAssetKit.Runtime.Download.Installers
{
    public class DownloadServiceInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Debug.Log("DownloadServiceInstaller");
            // Core Services
            Container.Bind<IDownloadExecutor>()
                .To<AddressableDownloadExecutor>()
                .AsSingle();

            Container.Bind<IDownloadQueue>()
                .To<PriorityDownloadQueue>()
                .AsSingle();

            Container.Bind<IProgressTracker>()
                .To<ProgressTracker>()
                .AsSingle();

            Container.Bind<IDownloadStrategy>()
                .To<DefaultDownloadStrategy>()
                .AsSingle();

            Container.Bind<ICatalogManager>()
                .To<AddressableCatalogManager>()
                .AsSingle();

            // Main Service
            Container.Bind<DownloadService>()
                .AsSingle()
                .NonLazy();
        }
    }
}