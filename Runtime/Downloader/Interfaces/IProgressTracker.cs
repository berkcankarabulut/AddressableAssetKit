using AddressableAssetKit.Runtime.Downloader.Models;
using UniRx;

namespace AddressableAssetKit.Runtime.Downloader.Interfaces
{
    public interface IProgressTracker
    {
        IReadOnlyReactiveProperty<int> ActiveDownloadCount { get; }
        IReadOnlyReactiveProperty<int> QueuedDownloadCount { get; }
        IReadOnlyReactiveProperty<long> TotalDownloaded { get; }
        IReadOnlyReactiveProperty<long> TotalSize { get; }

        void OnDownloadStarted(DownloadTask task);
        void OnDownloadProgress(DownloadProgress progress);
        void OnDownloadCompleted(DownloadResult result);
        void Reset();
    }
}