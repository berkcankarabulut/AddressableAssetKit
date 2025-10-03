using AddressableAssetKit.Runtime.Downloader.Models;

namespace AddressableAssetKit.Runtime.Downloader.Interfaces
{
    public interface IDownloadStrategy
    {
        bool ShouldRetry(DownloadTask task, DownloadResult result);
        int GetMaxConcurrentDownloads();
    }
}