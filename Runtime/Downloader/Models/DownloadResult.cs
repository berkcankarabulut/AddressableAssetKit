namespace AddressableAssetKit.Runtime.Downloader.Models
{
    public class DownloadResult
    {
        public string Label { get; }
        public bool Success { get; }
        public string Error { get; }
        public long Size { get; }

        public DownloadResult(string label, bool success, string error = null, long size = 0)
        {
            Label = label;
            Success = success;
            Error = error;
            Size = size;
        }

        public static DownloadResult Succeeded(string label, long size)
        {
            return new DownloadResult(label, true, null, size);
        }

        public static DownloadResult Failed(string label, string error)
        {
            return new DownloadResult(label, false, error);
        }
    }
}