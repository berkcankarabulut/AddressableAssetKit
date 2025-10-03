using AddressableAssetKit.Runtime.Downloader.Interfaces; 
using AddressableAssetKit.Runtime.Downloader.Models;

namespace AddressableAssetKit.Runtime.Downloader.Services
{
    public class DefaultDownloadStrategy : IDownloadStrategy
    {
        private readonly DownloadSettings _settings;

        public DefaultDownloadStrategy(DownloadSettings settings = null)
        {
            _settings = settings;
        }

        public bool ShouldRetry(DownloadTask task, DownloadResult result)
        {
            if (result.Success)
                return false;

            var autoRetry = _settings?.AutoRetryOnFail ?? true;
            var maxRetry = _settings?.MaxRetryCount ?? 3;

            return autoRetry && task.RetryCount < maxRetry;
        }

        public int GetMaxConcurrentDownloads()
        {
            return _settings?.MaxConcurrentDownloads ?? 3;
        }
    }
}