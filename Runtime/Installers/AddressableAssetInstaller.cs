using AddressableAssetKit.Runtime.Download.Services;
using AddressableAssetKit.Runtime.SceneLoader;
using AddressableAssetKit.Runtime.SceneLoader.Interfaces;
using Zenject;

namespace AddressableAssetKit.Runtime.Installers
{
    public class AddressableAssetInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Download Service
            Container.Bind<DownloadService>()
                .AsSingle()
                .NonLazy();

            Container.Bind<AddressableAssetManager>()
                .AsSingle()
                .NonLazy();

            Container.Bind<AddressableSceneLoader>()
                .AsSingle()
                .NonLazy();
        }
    }
}