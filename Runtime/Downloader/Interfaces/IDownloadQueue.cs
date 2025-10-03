using AddressableAssetKit.Runtime.Download.Models;

namespace AddressableAssetKit.Runtime.Download.Interfaces
{
    public interface IDownloadQueue
    {
        void Enqueue(DownloadTask task);
        bool TryDequeue(out DownloadTask task);
        int Count { get; }
        void Clear();
    }
}