using System;
using System.Threading;
using AddressableAssetKit.Runtime.Downloader.Interfaces;
using AddressableAssetKit.Runtime.Downloader.Models;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressableAssetKit.Runtime.Downloader.Services
{
    public class AddressableDownloadExecutor : IDownloadExecutor
    {
        private readonly Subject<DownloadProgress> _progressSubject = new();

        public IObservable<DownloadProgress> OnProgress => _progressSubject;

        public async UniTask<DownloadResult> ExecuteAsync(DownloadTask task, CancellationToken ct = default)
        {
            try
            {
                var size = await GetDownloadSizeAsync(task.Label, ct);

                if (size == 0)
                    return DownloadResult.Succeeded(task.Label, 0);

                return await DownloadWithProgressAsync(task, size, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return DownloadResult.Failed(task.Label, ex.Message);
            }
        }

        public async UniTask<long> GetDownloadSizeAsync(string label, CancellationToken ct = default)
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

        private async UniTask<DownloadResult> DownloadWithProgressAsync(DownloadTask task, long size, CancellationToken ct)
        {
            var handle = Addressables.DownloadDependenciesAsync(task.Label, true);
            long lastDownloaded = 0;

            try
            {
                while (!handle.IsDone && !ct.IsCancellationRequested)
                {
                    var current = (long)(size * handle.PercentComplete);
                    var delta = current - lastDownloaded;

                    if (delta > 0)
                        lastDownloaded = current;

                    _progressSubject.OnNext(new DownloadProgress(
                        task.Label,
                        handle.PercentComplete,
                        current,
                        size
                    ));

                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                await handle.ToUniTask(cancellationToken: ct);

                return handle.Status == AsyncOperationStatus.Succeeded
                    ? DownloadResult.Succeeded(task.Label, size)
                    : DownloadResult.Failed(task.Label, handle.OperationException?.Message);
            }
            finally
            {
                Addressables.Release(handle);
            }
        }
    }
}