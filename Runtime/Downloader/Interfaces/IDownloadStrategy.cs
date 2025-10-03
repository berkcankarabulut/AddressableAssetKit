using AddressableAssetKit.Runtime.Download.Models;

namespace AddressableAssetKit.Runtime.Download.Interfaces
{
    public interface IDownloadStrategy
    {
        bool ShouldRetry(DownloadTask task, DownloadResult result);
        int GetMaxConcurrentDownloads();
    }
}