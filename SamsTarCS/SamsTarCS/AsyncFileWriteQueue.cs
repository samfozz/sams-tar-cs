using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SamsTarCS
{
    /// <summary>
    /// A background file writer queue.
    /// This is useful for queuing up many little files, without blocking the main thread while the files are created.
    /// Not useful for large files!
    /// </summary>
    public class AsyncFileWriteQueue : IDisposable
    {
        private readonly long _maxSize;

        public long MaxSize => _maxSize;

        private object _lock = new object();

        private Task _worker;

        private long _currentSize = 0;

        public AsyncFileWriteQueue(long maxSize)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _maxSize = maxSize;
        }

        private readonly Dictionary<string, byte[]> _work  = new Dictionary<string, byte[]>();
        
        private readonly CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// The ammount of data buffered for writing.
        /// </summary>
        public long CurrentSize
        {
            get { return _currentSize; }
        }

        /// <summary>
        /// If the buffer has data pending
        /// </summary>
        public bool HasWork
        {
            get { return _work.Count > 0; }
        }

        /// <summary>
        /// Adds the file for writing.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public async Task Enqueue(string fileName, byte[] data, TimeSpan timeOut, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_worker == null)
            {
                throw new InvalidOperationException("AsyncFileWriteQueue.Start has not been called.");
            }

            await WaitForRoom(data.Length, timeOut, cancellationToken);

            lock (_lock)
            {
                if (_work.ContainsKey(fileName))
                {
                    throw new DuplicateNameException();
                }

                _work[fileName] = data;
                _currentSize += data.Length;
            }
        }

        /// <summary>
        /// Starts background writing.
        /// </summary>
        public void Start()
        { 
            _worker = Task.Run(()=> DoWork(), _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Cancels background writing.
        /// </summary>
        public async Task Stop()
        {
            if (_worker != null)
            {
                _cancellationTokenSource.Cancel();

                await _worker;

                _worker.Dispose();

                _worker = null;
            }

           
            
        }
        

        private void DoWork()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                KeyValuePair<string, byte[]> val;

                lock (_lock)
                {
                    val = _work.FirstOrDefault();

                    if (val.Key != null)
                    {
                        _work.Remove(val.Key);
                    }
                }

                if (val.Key == null)
                {
                    Thread.Sleep(100);
                    continue;
                }
                
                var len = val.Value.Length;
                File.WriteAllBytes(val.Key, val.Value);
                
                lock (_lock)
                {
                    this._currentSize -= len;
                }
            }
           
        }

        /// <summary>
        /// Waits for the specified amount of space to be freed in the buffer.
        /// </summary>
        /// <param name="allocate"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task WaitForRoom(long allocate, TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken))
        {
            long waited = 0;

            if (_worker == null)
            {
                throw new InvalidOperationException("AsyncFileWriteQueue.Start has not been called. Cannot wait.");
            }

            if (allocate > _maxSize)
            {
                throw new InvalidOperationException($"Cannot allocate '{allocate}' to a buffer of size '{_maxSize}'");
            }

            while ((_maxSize - CurrentSize) < allocate)
            {
                if (waited > timeout.TotalMilliseconds)
                {
                    throw new TimeoutException();
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);

                waited += 10;
            }
        }

        /// <summary>
        /// Waits for all data to write to disk
        /// </summary>
        /// <returns></returns>
        public async Task WaitComplete()
        {
            while (HasWork)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        public void Dispose()
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
            
            _cancellationTokenSource?.Dispose();
        }
    }
}