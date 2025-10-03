using System.Collections.Generic;
using System.Linq;
using AddressableAssetKit.Runtime.Download.Interfaces;
using AddressableAssetKit.Runtime.Download.Models;

namespace AddressableAssetKit.Runtime.Download.Services
{
    public class PriorityDownloadQueue : IDownloadQueue
    {
        private readonly List<DownloadTask> _queue = new();

        public int Count => _queue.Count;

        public void Enqueue(DownloadTask task)
        {
            _queue.Add(task);
            _queue.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public bool TryDequeue(out DownloadTask task)
        {
            if (_queue.Count == 0)
            {
                task = null;
                return false;
            }

            task = _queue[0];
            _queue.RemoveAt(0);
            return true;
        }

        public void Clear()
        {
            _queue.Clear();
        }
    }
}