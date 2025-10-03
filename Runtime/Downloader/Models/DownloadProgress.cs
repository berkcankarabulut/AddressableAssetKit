namespace AddressableAssetKit.Runtime.Download.Models
{
    public class DownloadProgress
    {
        public string Label { get; }
        public float PercentComplete { get; }
        public long DownloadedBytes { get; }
        public long TotalBytes { get; }

        public DownloadProgress(string label, float percentComplete, long downloadedBytes, long totalBytes)
        {
            Label = label;
            PercentComplete = percentComplete;
            DownloadedBytes = downloadedBytes;
            TotalBytes = totalBytes;
        }
    }
}