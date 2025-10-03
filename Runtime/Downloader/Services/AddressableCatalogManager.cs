using System.Threading;
using AddressableAssetKit.Runtime.Downloader.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressableAssetKit.Runtime.Downloader.Services
{
    public class AddressableCatalogManager : ICatalogManager
    {
        public async UniTask<bool> UpdateCatalogsAsync(CancellationToken ct = default)
        {
            var checkHandle = Addressables.CheckForCatalogUpdates(false);

            try
            {
                await checkHandle.ToUniTask(cancellationToken: ct);
                var catalogs = checkHandle.Result;

                if (catalogs?.Count > 0)
                {
                    var updateHandle = Addressables.UpdateCatalogs(catalogs, false);

                    try
                    {
                        await updateHandle.ToUniTask(cancellationToken: ct);
                        return updateHandle.Status == AsyncOperationStatus.Succeeded;
                    }
                    finally
                    {
                        Addressables.Release(updateHandle);
                    }
                }

                return false;
            }
            finally
            {
                Addressables.Release(checkHandle);
            }
        }

        public void ClearCache()
        {
            Caching.ClearCache();
        }
    }
}