using System;
using System.Collections.Generic;
using System.Threading;
using AddressableAssetKit.Runtime.Download.Interfaces;
using AddressableAssetKit.Runtime.Download.Models;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Zenject;

namespace AddressableAssetKit.Runtime.Download.Services
{
    public class DownloadService : IInitializable, IDisposable
    {
        private readonly IDownloadExecutor _executor;
        private readonly IDownloadQueue _queue;
        private readonly IProgressTracker _tracker;
        private readonly IDownloadStrategy _strategy;
        private readonly ICatalogManager _catalogManager;

        private readonly Subject<DownloadTask> _downloadStarted = new();
        private readonly Subject<DownloadResult> _downloadCompleted = new();
        private readonly Subject<DownloadResult> _downloadFailed = new();
        private readonly Subject<Unit> _allCompleted = new();

        private readonly ReactiveProperty<bool> _isDownloading = new(false);
        private readonly Dictionary<string, CancellationTokenSource> _activeDownloads = new();
        private readonly CompositeDisposable _disposables = new();
        private CancellationTokenSource _processCts;

        public IObservable<DownloadTask> OnDownloadStarted => _downloadStarted;
        public IObservable<DownloadProgress> OnDownloadProgress => _executor.OnProgress;
        public IObservable<DownloadResult> OnDownloadCompleted => _downloadCompleted;
        public IObservable<DownloadResult> OnDownloadFailed => _downloadFailed;
        public IObservable<Unit> OnAllDownloadsCompleted => _allCompleted;

        public IReadOnlyReactiveProperty<bool> IsDownloading => _isDownloading;
        public IReadOnlyReactiveProperty<int> ActiveDownloadCount => _tracker.ActiveDownloadCount;
        public IReadOnlyReactiveProperty<int> QueuedDownloadCount => _tracker.QueuedDownloadCount;

        public IObservable<float> TotalProgressPercentage =>
            Observable.CombineLatest(
                _tracker.TotalDownloaded,
                _tracker.TotalSize,
                (d, t) => t > 0 ? (float)d / t : 0f
            );

        public DownloadService(
            IDownloadExecutor executor,
            IDownloadQueue queue,
            IProgressTracker tracker,
            IDownloadStrategy strategy,
            ICatalogManager catalogManager)
        {
            _executor = executor;
            _queue = queue;
            _tracker = tracker;
            _strategy = strategy;
            _catalogManager = catalogManager;
        }

        public void Initialize()
        {
            _downloadCompleted
                .Subscribe(result => Debug.Log($"✓ Downloaded: {result.Label}"))
                .AddTo(_disposables);

            _downloadFailed
                .Subscribe(result => Debug.LogError($"✗ Failed: {result.Label} - {result.Error}"))
                .AddTo(_disposables);

            _allCompleted
                .Subscribe(_ =>
                {
                    Debug.Log("=== ALL DOWNLOADS COMPLETED ===");
                    _isDownloading.Value = false;
                })
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            CancelAll();
            _disposables?.Dispose();
            _isDownloading?.Dispose();
            _downloadStarted?.Dispose();
            _downloadCompleted?.Dispose();
            _downloadFailed?.Dispose();
            _allCompleted?.Dispose();
        }

        #region Public API

        public void DownloadContent(string label, int priority = 0)
        {
            if (string.IsNullOrEmpty(label))
            {
                Debug.LogError("Label cannot be empty");
                return;
            }

            var task = new DownloadTask(label, priority);
            _queue.Enqueue(task);

            if (!_isDownloading.Value)
            {
                _isDownloading.Value = true;
                ProcessQueueAsync().Forget();
            }
        }

        public async UniTask DownloadAsync(string label, int priority = 0, CancellationToken ct = default)
        {
            var tcs = new UniTaskCompletionSource();
            var disposables = new CompositeDisposable();

            _downloadCompleted
                .Where(r => r.Label == label)
                .Take(1)
                .Subscribe(_ => tcs.TrySetResult())
                .AddTo(disposables);

            _downloadFailed
                .Where(r => r.Label == label)
                .Take(1)
                .Subscribe(r => tcs.TrySetException(new Exception(r.Error)))
                .AddTo(disposables);

            ct.Register(() =>
            {
                disposables.Dispose();
                tcs.TrySetCanceled();
            });

            DownloadContent(label, priority);

            try
            {
                await tcs.Task;
            }
            finally
            {
                disposables.Dispose();
            }
        }

        public async UniTask<List<string>> DownloadMultipleAsync(List<string> labels, int priority = 0, CancellationToken ct = default)
        {
            var completed = new List<string>();

            foreach (var label in labels)
            {
                await DownloadAsync(label, priority, ct);
                completed.Add(label);
            }

            return completed;
        }

        public UniTask<long> CheckDownloadSizeAsync(string label, CancellationToken ct = default)
        {
            return _executor.GetDownloadSizeAsync(label, ct);
        }

        public void CancelAll()
        {
            _processCts?.Cancel();

            foreach (var cts in _activeDownloads.Values)
                cts?.Cancel();

            _activeDownloads.Clear();
            _queue.Clear();
            _tracker.Reset();
            _isDownloading.Value = false;
        }

        public UniTask<bool> UpdateCatalogsAsync(CancellationToken ct = default)
        {
            return _catalogManager.UpdateCatalogsAsync(ct);
        }

        public void ClearCache()
        {
            _catalogManager.ClearCache();
        }

        #endregion

        #region Private Methods

        private async UniTaskVoid ProcessQueueAsync()
        {
            _processCts = new CancellationTokenSource();
            var ct = _processCts.Token;

            try
            {
                while ((_queue.Count > 0 || _activeDownloads.Count > 0) && !ct.IsCancellationRequested)
                {
                    var maxConcurrent = _strategy.GetMaxConcurrentDownloads();

                    while (_activeDownloads.Count < maxConcurrent && _queue.TryDequeue(out var task))
                    {
                        ExecuteDownloadAsync(task, ct).Forget();
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                _isDownloading.Value = false;
                _allCompleted.OnNext(Unit.Default);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Download queue cancelled");
            }
        }

        private async UniTaskVoid ExecuteDownloadAsync(DownloadTask task, CancellationToken ct)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _activeDownloads[task.Label] = cts;

            _tracker.OnDownloadStarted(task);
            _downloadStarted.OnNext(task);

            try
            {
                var result = await _executor.ExecuteAsync(task, cts.Token);

                _tracker.OnDownloadCompleted(result);

                if (result.Success)
                {
                    _downloadCompleted.OnNext(result);
                }
                else
                {
                    if (_strategy.ShouldRetry(task, result))
                    {
                        _queue.Enqueue(task.WithRetry());
                    }
                    else
                    {
                        _downloadFailed.OnNext(result);
                    }
                }
            }
            finally
            {
                _activeDownloads.Remove(task.Label);
                cts?.Dispose();
            }
        }

        #endregion
    }
}