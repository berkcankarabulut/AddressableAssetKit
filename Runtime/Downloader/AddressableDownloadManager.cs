using System;
using System.Collections.Generic;
using System.Threading;
using AddressableAssetKit.Runtime.Data;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Zenject;

namespace AddressableAssetKit.Runtime
{
    public class AddressableDownloadManager : IInitializable, IDisposable
    {
        [Inject(Optional = true)] private readonly DownloadSettings _settings;

        private readonly Subject<string> _downloadStarted = new();
        private readonly Subject<ProgressData> _downloadProgress = new();
        private readonly Subject<string> _downloadCompleted = new();
        private readonly Subject<ErrorData> _downloadFailed = new();
        private readonly Subject<Unit> _allCompleted = new();

        private readonly ReactiveProperty<bool> _isDownloading = new(false);
        private readonly ReactiveProperty<int> _activeCount = new(0);
        private readonly ReactiveProperty<int> _queuedCount = new(0);
        private readonly ReactiveProperty<long> _totalSize = new(0);
        private readonly ReactiveProperty<long> _totalDownloaded = new(0);

        public IObservable<string> OnDownloadStarted => _downloadStarted;
        public IObservable<ProgressData> OnDownloadProgress => _downloadProgress;
        public IObservable<string> OnDownloadCompleted => _downloadCompleted;
        public IObservable<ErrorData> OnDownloadFailed => _downloadFailed;
        public IObservable<Unit> OnAllDownloadsCompleted => _allCompleted;

        public IReadOnlyReactiveProperty<bool> IsDownloading => _isDownloading;
        public IReadOnlyReactiveProperty<int> ActiveDownloadCount => _activeCount;
        public IReadOnlyReactiveProperty<int> QueuedDownloadCount => _queuedCount;

        public IObservable<float> TotalProgressPercentage =>
            Observable.CombineLatest(_totalDownloaded, _totalSize,
                (d, t) => t > 0 ? (float)d / t : 0f);

        private readonly Dictionary<string, CancellationTokenSource> _activeDownloads = new();
        private readonly Queue<DownloadRequest> _downloadQueue = new();
        private readonly CompositeDisposable _disposables = new();
        private CancellationTokenSource _processCts;

        private int MaxConcurrent => _settings?.MaxConcurrentDownloads ?? 3;
        private bool AutoRetry => _settings?.AutoRetryOnFail ?? true;
        private int MaxRetry => _settings?.MaxRetryCount ?? 3;

        public void Initialize()
        {
            _downloadCompleted
                .Subscribe(label => Debug.Log($"✓ Downloaded: {label}"))
                .AddTo(_disposables);

            _downloadFailed
                .Subscribe(data => Debug.LogError($"✗ Failed: {data.Label} - {data.Error}"))
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
            _processCts?.Cancel();
            _processCts?.Dispose();

            foreach (var cts in _activeDownloads.Values)
                cts?.Cancel();
            _activeDownloads.Clear();

            _disposables?.Dispose();
            _downloadStarted?.Dispose();
            _downloadProgress?.Dispose();
            _downloadCompleted?.Dispose();
            _downloadFailed?.Dispose();
            _allCompleted?.Dispose();
            _isDownloading?.Dispose();
            _activeCount?.Dispose();
            _queuedCount?.Dispose();
            _totalSize?.Dispose();
            _totalDownloaded?.Dispose();
        }

        #region Download API

        public void DownloadContent(string label, int priority = 0)
        {
            if (string.IsNullOrEmpty(label))
            {
                Debug.LogError("Label cannot be empty");
                return;
            }

            _downloadQueue.Enqueue(new DownloadRequest { Label = label, Priority = priority });
            _queuedCount.Value = _downloadQueue.Count;

            if (!_isDownloading.Value)
                ProcessQueue().Forget();
        }

        public async UniTask DownloadAsync(string label, int priority = 0, CancellationToken ct = default)
        {
            var tcs = new UniTaskCompletionSource();
            var disposables = new CompositeDisposable();

            _downloadCompleted
                .Where(l => l == label)
                .Take(1)
                .Subscribe(_ => tcs.TrySetResult())
                .AddTo(disposables);

            _downloadFailed
                .Where(d => d.Label == label)
                .Take(1)
                .Subscribe(d => tcs.TrySetException(new Exception(d.Error)))
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

        public async UniTask<List<string>> DownloadMultipleAsync(List<string> labels, int priority = 0,
            CancellationToken ct = default)
        {
            var completed = new List<string>();

            foreach (var label in labels)
            {
                await DownloadAsync(label, priority, ct);
                completed.Add(label);
            }

            return completed;
        }

        public async UniTask<long> CheckDownloadSizeAsync(string label, CancellationToken ct = default)
        {
            var handle = Addressables.GetDownloadSizeAsync(label);

            try
            {
                await handle.ToUniTask(cancellationToken: ct);
                return handle.Result;
            }
            finally
            {
                Addressables.Release(handle);
            }
        }

        public void CancelAll()
        {
            _processCts?.Cancel();

            foreach (var cts in _activeDownloads.Values)
                cts?.Cancel();

            _activeDownloads.Clear();
            _downloadQueue.Clear();
            _isDownloading.Value = false;
            _activeCount.Value = 0;
            _queuedCount.Value = 0;
        }

        #endregion

        #region Catalog & Cache

        public async UniTask<bool> UpdateCatalogsAsync(CancellationToken ct = default)
        {
            var checkHandle = Addressables.CheckForCatalogUpdates(false);

            try
            {
                await checkHandle.ToUniTask(cancellationToken: ct);
                var catalogs = checkHandle.Result;

                if (catalogs?.Count > 0)
                {
                    var updateHandle = Addressables.UpdateCatalogs(catalogs, false);

                    try
                    {
                        await updateHandle.ToUniTask(cancellationToken: ct);
                        return updateHandle.Status == AsyncOperationStatus.Succeeded;
                    }
                    finally
                    {
                        Addressables.Release(updateHandle);
                    }
                }

                return false;
            }
            finally
            {
                Addressables.Release(checkHandle);
            }
        }

        public void ClearCache() => Caching.ClearCache(); 

        #endregion

        #region Private Methods

        private async UniTaskVoid ProcessQueue()
        {
            _isDownloading.Value = true;
            _processCts = new CancellationTokenSource();
            var ct = _processCts.Token;

            try
            {
                while ((_downloadQueue.Count > 0 || _activeDownloads.Count > 0) && !ct.IsCancellationRequested)
                {
                    while (_activeDownloads.Count < MaxConcurrent && _downloadQueue.Count > 0)
                    {
                        var req = _downloadQueue.Dequeue();
                        _queuedCount.Value = _downloadQueue.Count;
                        DownloadInternal(req, ct).Forget();
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

        private async UniTaskVoid DownloadInternal(DownloadRequest req, CancellationToken ct)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _activeDownloads[req.Label] = cts;

            _downloadStarted.OnNext(req.Label);
            _activeCount.Value++;

            try
            {
                var sizeHandle = Addressables.GetDownloadSizeAsync(req.Label);
                long size;

                try
                {
                    await sizeHandle.ToUniTask(cancellationToken: cts.Token);
                    size = sizeHandle.Result;
                }
                finally
                {
                    Addressables.Release(sizeHandle);
                }

                if (size == 0)
                {
                    _downloadCompleted.OnNext(req.Label);
                    return;
                }

                _totalSize.Value += size;

                var handle = Addressables.DownloadDependenciesAsync(req.Label, true);
                long lastDownloaded = 0;

                try
                {
                    while (!handle.IsDone && !cts.Token.IsCancellationRequested)
                    {
                        long current = (long)(size * handle.PercentComplete);
                        long delta = current - lastDownloaded;

                        if (delta > 0)
                        {
                            _totalDownloaded.Value += delta;
                            lastDownloaded = current;
                        }

                        _downloadProgress.OnNext(new ProgressData
                        {
                            Label = req.Label,
                            Progress = handle.PercentComplete,
                            DownloadedBytes = current,
                            TotalBytes = size
                        });

                        await UniTask.Yield(PlayerLoopTiming.Update, cts.Token);
                    }

                    await handle.ToUniTask(cancellationToken: cts.Token);

                    if (handle.Status == AsyncOperationStatus.Succeeded)
                        _downloadCompleted.OnNext(req.Label);
                    else
                    {
                        _totalSize.Value -= size;
                        HandleError(req, handle.OperationException?.Message);
                    }
                }
                finally
                {
                    Addressables.Release(handle);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Download cancelled: {req.Label}");
            }
            catch (Exception ex)
            {
                HandleError(req, ex.Message);
            }
            finally
            {
                _activeDownloads.Remove(req.Label);
                _activeCount.Value--;
                cts?.Dispose();
            }
        }

        private void HandleError(DownloadRequest req, string error)
        {
            if (AutoRetry && req.RetryCount < MaxRetry)
            {
                req.RetryCount++;
                _downloadQueue.Enqueue(req);
                _queuedCount.Value = _downloadQueue.Count;
            }
            else
            {
                _downloadFailed.OnNext(new ErrorData
                {
                    Label = req.Label,
                    Error = error ?? "Unknown error"
                });
            }
        }

        #endregion
    }
}