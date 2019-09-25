using System;
using System.Threading;
#if !NET35
using System.Threading.Tasks;
#endif

namespace STEP.Locking.Semaphore
{
    internal class ExSemaphore : IDisposable
    {
#if !NET35
        private readonly SemaphoreSlim _semaphore;
#else
        private readonly System.Threading.Semaphore _semaphore;
#endif

        public string Value { get; set; }

        public ExSemaphore()
            : this(string.Empty)
        {
        }

        public ExSemaphore(string value)
        {
#if !NET35
            _semaphore = new SemaphoreSlim(1, 1);
#else
            _semaphore = new System.Threading.Semaphore(1, 1);
#endif
            Value = value;
        }

#if !NET35
        public bool Wait() => _semaphore.Wait(Timeout.Infinite);

        public bool Wait(TimeSpan timeout) => _semaphore.Wait(timeout);

        public async Task<bool> WaitAsync(CancellationToken cancellationToken = default(CancellationToken)) => await _semaphore.WaitAsync(Timeout.Infinite, cancellationToken);

        public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default(CancellationToken)) => await _semaphore.WaitAsync(timeout, cancellationToken);
#else
        public bool Wait() => _semaphore.WaitOne();

        public bool Wait(TimeSpan timeout) => _semaphore.WaitOne(timeout);
#endif

        public void Release() => _semaphore.Release();

#if !NET35
        public void Dispose() => _semaphore.Dispose();
#else
        public void Dispose() => _semaphore.Close();
#endif
    }
}
