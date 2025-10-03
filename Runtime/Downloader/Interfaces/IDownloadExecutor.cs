using System;
using System.Threading;
using AddressableAssetKit.Runtime.Download.Models;
using Cysharp.Threading.Tasks;

namespace AddressableAssetKit.Runtime.Download.Interfaces
{
    public interface IDownloadExecutor
    {
        IObservable<DownloadProgress> OnProgress { get; }

        UniTask<DownloadResult> ExecuteAsync(DownloadTask task, CancellationToken ct = default);
        UniTask<long> GetDownloadSizeAsync(string label, CancellationToken ct = default);
    }
}