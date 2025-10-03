using AddressableAssetKit.Runtime.Downloader.Models;

namespace AddressableAssetKit.Runtime.Downloader.Interfaces
{
    public interface IDownloadQueue
    {
        void Enqueue(DownloadTask task);
        bool TryDequeue(out DownloadTask task);
        int Count { get; }
        void Clear();
    }
}