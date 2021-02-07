using System;
using System.Threading;
#if !NET35
using System.Threading.Tasks;
#endif

namespace SKIT.Locking.Semaphore
{
    /// <summary>
    /// 一个基于系统信号量的 <see cref="ILock"/> 的实现。
    /// </summary>
    public sealed class SemaphoreLock : ILock
    {
        private readonly ExSemaphoreStorage _semaphoreStorage;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _expiry;

        private Timer _renewalTimer; // 定时保活
        private Timer _expiryTimer; // 超时释放
        private bool _disposed = false;

        /// <summary>
        /// 获取锁的标识。
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// 获取锁在当前 <see cref="SemaphoreLock"/> 对象的值。
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// 获取一个值，该值锁是否被当前 <see cref="SemaphoreLock"/> 对象持有。
        /// </summary>
        public bool IsAcquired { get; private set; }

        /// <summary>
        /// 获取一个值，该值锁是否可续约。
        /// </summary>
        public bool IsRenewable
        {
            get
            {
                if (IsAcquired && !_disposed)
                {
                    return !_semaphoreStorage.ContainsKey(this.Key);
                }

                return false;
            }
        }

        internal SemaphoreLock(ExSemaphoreStorage semaphoreStorage, string key, TimeSpan timeout, TimeSpan expiry)
        {
            if (semaphoreStorage == null)
                throw new ArgumentNullException(nameof(semaphoreStorage));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            _semaphoreStorage = semaphoreStorage;
            _timeout = timeout;
            _expiry = expiry;

            Key = key;
            Value = Guid.NewGuid().ToString("N");
        }

        private ExSemaphore GetSemaphore()
        {
            return _semaphoreStorage.GetOrAdd(this.Key, this.Value);
        }

        private void StartWatchDogs()
        {
            if (_expiry == TimeoutHelper.GetInfiniteTimeSpan() || _expiry >= TimeSpan.MaxValue)
                return;

            long _nextExpiryUnixTimeMilliseconds = DateTimeOffsetHelper.ToUnixTimeMilliseconds(DateTimeOffset.Now.Add(_expiry));

            if (this.IsAcquired)
            {
                _renewalTimer = new Timer(state =>
                {
                    // 在锁被主动释放之前定时续约过期时间
                    if (IsRenewable)
                    {
                        _nextExpiryUnixTimeMilliseconds = DateTimeOffsetHelper.ToUnixTimeMilliseconds(DateTimeOffset.Now.Add(_expiry));
                    }
                }, this.Key, TimeSpan.Zero, TimeSpan.FromMilliseconds(_expiry.TotalMilliseconds / 3));

                _expiryTimer = new Timer(state =>
                {
                    if (_nextExpiryUnixTimeMilliseconds <= DateTimeOffsetHelper.ToUnixTimeMilliseconds(DateTimeOffset.Now))
                    {
                        if (CheckLocked())
                        {
                            StopWatchDogs();
                            ReleaseIfEnsureLocked();
                        }
                    }
                }, this.Key, _expiry, _expiry);
            }
        }

        private void StopWatchDogs()
        {
            if (_renewalTimer != null)
            {
                _renewalTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _renewalTimer.Dispose();
                _renewalTimer = null;
            }

            if (_expiryTimer != null)
            {
                _expiryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _expiryTimer.Dispose();
                _expiryTimer = null;
            }
        }

        private void ReleaseIfEnsureLocked()
        {
            if (CheckLocked())
            {
                try
                {
                    GetSemaphore().Release();
                }
                catch (SemaphoreFullException) { }

                _semaphoreStorage.Remove(this.Key);
            }
        }

        #region Implements SKIT.Locking.ILock
        /// <summary>
        /// 检查当前 <see cref="SemaphoreLock"/> 对象是否仍持有锁。
        /// </summary>
        /// <returns></returns>
        public bool CheckLocked()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SemaphoreLock));

            if (!this.IsAcquired)
                return false;

            ExSemaphore semaphore = GetSemaphore();
            return string.Equals(semaphore.Value, this.Value);
        }

#if !NET35
        /// <summary>
        /// 异步检查当前 <see cref="SemaphoreLock"/> 对象是否仍持有锁。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> CheckLockedAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(CheckLocked, cancellationToken);
        }
#endif
        #endregion

        #region Implements SKIT.Locking.ILockable
        /// <summary>
        /// 等待加锁，直到超时前始终阻塞，并在持有锁失败时抛出异常。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="LockException"></exception>
        public SemaphoreLock Wait()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SemaphoreLock));

            if (this.IsAcquired)
                return this;

            ExSemaphore semaphore = GetSemaphore();

            try
            {
                this.IsAcquired = semaphore.Wait(_timeout);
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                throw new LockException("An error occured when acquire lock.", ex);
            }

            if (this.IsAcquired)
            {
                semaphore.Value = this.Value;
                StartWatchDogs();
            }

            return this;
        }

        /// <summary>
        /// 尝试等待加锁，直到超时前始终阻塞。
        /// </summary>
        /// <returns></returns>
        public SemaphoreLock TryWait()
        {
            try
            {
                Wait();
            }
            catch (ObjectDisposedException) { throw; }
            catch { }

            return this;
        }

#if !NET35
        /// <summary>
        /// 异步等待加锁，直到超时前始终阻塞，并在等待锁错误时抛出异常。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="LockException"></exception>
        public async Task<SemaphoreLock> WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SemaphoreLock));

            if (this.IsAcquired)
                return this;

            ExSemaphore semaphore = GetSemaphore();

            try
            {
                this.IsAcquired = await semaphore.WaitAsync(_timeout, cancellationToken);
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException) && !(ex is OperationCanceledException))
            {
                throw new LockException("An error occured when acquire lock.", ex);
            }

            if (this.IsAcquired)
            {
                semaphore.Value = this.Value;
                StartWatchDogs();
            }

            return this;
        }

        /// <summary>
        /// 尝试异步等待加锁，直到超时前始终阻塞。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SemaphoreLock> TryWaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await WaitAsync(cancellationToken);
            }
            catch (ObjectDisposedException) { throw; }
            catch { }

            return this;
        }
#endif

        ILock ILockable.Wait()
        {
            return this.Wait();
        }

        ILock ILockable.TryWait()
        {
            return this.TryWait();
        }

#if !NET35
        async Task<ILock> ILockable.WaitAsync(CancellationToken cancellationToken)
        {
            return await this.WaitAsync(cancellationToken);
        }

        async Task<ILock> ILockable.TryWaitAsync(CancellationToken cancellationToken)
        {
            return await this.TryWaitAsync(cancellationToken);
        }
#endif
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                if (this.IsAcquired)
                {
                    StopWatchDogs();
                    ReleaseIfEnsureLocked();
                }

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
