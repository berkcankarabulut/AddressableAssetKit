using AddressableAssetKit.Runtime.Downloader.Interfaces;
using AddressableAssetKit.Runtime.Downloader.Services;
using Zenject;

namespace AddressableAssetKit.Runtime.Downloader.Installers
{
    public class DownloadServiceInstaller : Installer<DownloadServiceInstaller>
    {
        public override void InstallBindings()
        {
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