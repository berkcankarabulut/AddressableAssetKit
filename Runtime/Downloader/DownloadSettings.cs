namespace AddressableAssetKit.Runtime.Download
{ 
    public class DownloadSettings
    {
        public int MaxConcurrentDownloads = 3;
        public bool AutoRetryOnFail = true;
        public int MaxRetryCount = 3;
    }
}