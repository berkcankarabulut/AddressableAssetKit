namespace AddressableAssetKit.Runtime.Downloader.Models
{
    public class DownloadTask
    {
        public string Label { get; }
        public int Priority { get; }
        public int RetryCount { get; }

        public DownloadTask(string label, int priority = 0, int retryCount = 0)
        {
            Label = label;
            Priority = priority;
            RetryCount = retryCount;
        }

        public DownloadTask WithRetry()
        {
            return new DownloadTask(Label, Priority, RetryCount + 1);
        }
    }
}