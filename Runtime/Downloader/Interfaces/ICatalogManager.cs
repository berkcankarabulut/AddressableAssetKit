using System.Threading;
using Cysharp.Threading.Tasks;

namespace AddressableAssetKit.Runtime.Downloader.Interfaces
{
    public interface ICatalogManager
    {
        UniTask<bool> UpdateCatalogsAsync(CancellationToken ct = default);
        void ClearCache();
    }
}