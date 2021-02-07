using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace SKIT.Locking.Redis
{
    /// <summary>
    /// 一个基于 Redis 的 <see cref="ILock"/> 的实现。
    /// </summary>
    public sealed class RedisLock : ILock
    {
        const string ROOT = "_SKITLOCKING";

        private readonly IDatabase _redisDatabase;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _expiry;

        private Timer _renewalTimer; // 定时保活
        private bool _disposed = false;

        /// <summary>
        /// 获取锁的标识。
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// 获取锁在当前 <see cref="RedisLock"/> 对象的值。
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// 获取一个值，该值锁是否被当前 <see cref="RedisLock"/> 对象持有。
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
                    try
                    {
                        return _redisDatabase.IsConnected(ROOT);
                    }
                    catch (RedisException) { }
                }

                return false;
            } 
        }

        internal RedisLock(IDatabase redisDatabase, string key, TimeSpan timeout, TimeSpan expiry)
        {
            if (redisDatabase == null)
                throw new ArgumentNullException(nameof(redisDatabase));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            _redisDatabase = redisDatabase;
            _timeout = timeout;
            _expiry = expiry;

            Key = key;
            Value = Guid.NewGuid().ToString("N");
        }

        private string GetRedisKey()
        {
            return string.Concat(ROOT, "/", this.Key);
        }

        private void StartWatchDogs()
        {
            if (_expiry == Timeout.InfiniteTimeSpan || _expiry >= TimeSpan.MaxValue)
                return;

            if (this.IsAcquired)
            {
                TimeSpan interval = TimeSpan.FromMilliseconds(_expiry.TotalMilliseconds / 3);
                _renewalTimer = new Timer(state =>
                {
                    // 在锁被主动释放之前定时续约过期时间
                    if (IsRenewable)
                    {
                        if (_redisDatabase.LockExtend(GetRedisKey(), this.Value, _expiry))
                        {
                            StopWatchDogs();
                        }
                    }
                }, this.Key, interval, interval);
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
        }

        private void ReleaseIfEnsureLocked()
        {
            try
            {
                _redisDatabase.LockRelease(GetRedisKey(), this.Value);
            }
            catch (RedisTimeoutException) { }
            catch (RedisException) { }
        }

        #region Implements SKIT.Locking.ILock
        /// <summary>
        /// 检查当前 <see cref="RedisLock"/> 对象是否仍持有锁。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="LockException"></exception>
        public bool CheckLocked()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisLock));

            if (!this.IsAcquired)
                return false;

            try
            {
                return string.Equals(_redisDatabase.LockQuery(GetRedisKey()), this.Value);
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                throw new LockException("An error occured when ensure locked.", ex);
            }
        }

        /// <summary>
        /// 异步检查当前 <see cref="RedisLock"/> 对象是否仍持有锁。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="LockException"></exception>
        public async Task<bool> CheckLockedAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisLock));

            if (!this.IsAcquired)
                return false;

            try
            {
                return string.Equals(await _redisDatabase.LockQueryAsync(GetRedisKey()), this.Value);
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException) && !(ex is OperationCanceledException))
            {
                throw new LockException("An error occured when ensure locked.", ex);
            }
        }
        #endregion

        #region Implements SKIT.Locking.ILockable
        /// <summary>
        /// 等待加锁，直到超时前始终阻塞，并在等待锁错误时抛出异常。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="LockException"></exception>
        public RedisLock Wait()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisLock));

            if (this.IsAcquired)
                return this;

            try
            {
                using (CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(_timeout))
                {
                    while (!timeoutTokenSource.IsCancellationRequested)
                    {
                        this.IsAcquired = _redisDatabase.LockTake(GetRedisKey(), this.Value, _expiry);

                        if (this.IsAcquired)
                            break;
                    }
                }
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                throw new LockException("An error occured when acquire lock.", ex);
            }

            if (this.IsAcquired)
            {
                StartWatchDogs();
            }

            return this;
        }

        /// <summary>
        /// 尝试等待加锁，直到超时前始终阻塞。
        /// </summary>
        /// <returns></returns>
        public RedisLock TryWait()
        {
            try
            {
                Wait();
            }
            catch (ObjectDisposedException) { throw; }
            catch { }

            return this;
        }

        /// <summary>
        /// 异步等待加锁，直到超时前始终阻塞，并在等待锁错误时抛出异常。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="LockException"></exception>
        public async Task<RedisLock> WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisLock));

            if (this.IsAcquired)
                return this;

            try
            {
                using (CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(_timeout))
                using (CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token))
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        this.IsAcquired = await _redisDatabase.LockTakeAsync(GetRedisKey(), this.Value, _expiry);

                        if (this.IsAcquired)
                            break;
                    }
                }
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException) && !(ex is OperationCanceledException))
            {
                throw new LockException("An error occured when acquire lock.", ex);
            }

            if (this.IsAcquired)
            {
                StartWatchDogs();
            }

            return this;
        }

        /// <summary>
        /// 尝试异步等待加锁，直到超时前始终阻塞。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<RedisLock> TryWaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await WaitAsync(cancellationToken);
            }
            catch (ObjectDisposedException) { throw; }
            catch { }

            return this;
        }

        ILock ILockable.Wait()
        {
            return this.Wait();
        }

        ILock ILockable.TryWait()
        {
            return this.TryWait();
        }

        async Task<ILock> ILockable.WaitAsync(CancellationToken cancellationToken)
        {
            return await this.WaitAsync(cancellationToken);
        }

        async Task<ILock> ILockable.TryWaitAsync(CancellationToken cancellationToken)
        {
            return await this.TryWaitAsync(cancellationToken);
        }
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
