namespace AddressableAssetKit.Runtime.Downloader
{ 
    public class DownloadSettings
    {
        public int MaxConcurrentDownloads = 3;
        public bool AutoRetryOnFail = true;
        public int MaxRetryCount = 3;
    }
}