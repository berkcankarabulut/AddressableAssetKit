using AddressableAssetKit.Runtime.Downloader.Interfaces;
using AddressableAssetKit.Runtime.Downloader.Models;
using UniRx;

namespace AddressableAssetKit.Runtime.Downloader.Services
{
    public class ProgressTracker : IProgressTracker
    {
        private readonly ReactiveProperty<int> _activeCount = new(0);
        private readonly ReactiveProperty<int> _queuedCount = new(0);
        private readonly ReactiveProperty<long> _totalDownloaded = new(0);
        private readonly ReactiveProperty<long> _totalSize = new(0);

        public IReadOnlyReactiveProperty<int> ActiveDownloadCount => _activeCount;
        public IReadOnlyReactiveProperty<int> QueuedDownloadCount => _queuedCount;
        public IReadOnlyReactiveProperty<long> TotalDownloaded => _totalDownloaded;
        public IReadOnlyReactiveProperty<long> TotalSize => _totalSize;

        public void OnDownloadStarted(DownloadTask task)
        {
            _activeCount.Value++;
            _queuedCount.Value--;
        }

        public void OnDownloadProgress(DownloadProgress progress)
        {
            var delta = progress.DownloadedBytes - _totalDownloaded.Value;
            if (delta > 0)
                _totalDownloaded.Value += delta;
        }

        public void OnDownloadCompleted(DownloadResult result)
        {
            _activeCount.Value--;
            if (result.Success)
                _totalSize.Value += result.Size;
        }

        public void Reset()
        {
            _activeCount.Value = 0;
            _queuedCount.Value = 0;
            _totalDownloaded.Value = 0;
            _totalSize.Value = 0;
        }
    }
}