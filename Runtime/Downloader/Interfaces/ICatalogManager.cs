using System.Threading;
using Cysharp.Threading.Tasks;

namespace AddressableAssetKit.Runtime.Download.Interfaces
{
    public interface ICatalogManager
    {
        UniTask<bool> UpdateCatalogsAsync(CancellationToken ct = default);
        void ClearCache();
    }
}