using System;

namespace SKIT.Locking.Semaphore
{
    /// <summary>
    /// 用于创建一个 <see cref="SemaphoreLock"/> 对象。
    /// </summary>
    public sealed class SemaphoreLockFactory : ILockFactory
    {
        private bool _disposed = false;

        /// <summary>
        /// 获取一个值，指示创建出的 <see cref="SemaphoreLock"/> 对象的作用域。
        /// </summary>
        public LockScopes Scope { get { return LockScopes.ApplicationDomain; } }

        /// <summary>
        /// 获取或设置默认加锁等待超时时间。
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeoutHelper.GetInfiniteTimeSpan();

        /// <summary>
        /// 获取或设置默认加锁过期时间，避免死锁。
        /// </summary>
        public TimeSpan DefaultExpiry { get; set; } = TimeoutHelper.GetInfiniteTimeSpan();

        /// <summary>
        /// 
        /// </summary>
        public SemaphoreLockFactory()
        {
        }

        /// <summary>
        /// 创建并返回一个 <see cref="SemaphoreLock" /> 对象。
        /// </summary>
        /// <param name="resource">被锁的资源。</param>
        /// <param name="timeout">加锁等待超时时间（默认值 null，将使用默认超时时间）。</param>
        /// <param name="expiry">加锁过期时间（默认值 null，将使用默认过期时间）。</param>
        /// <returns></returns>
        public SemaphoreLock Create(string resource, TimeSpan? timeout = null, TimeSpan? expiry = null)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            if (_disposed)
                throw new ObjectDisposedException(nameof(SemaphoreLockFactory));

            return new SemaphoreLock(ExSemaphoreStorage.Instance, resource, timeout ?? this.DefaultTimeout, expiry ?? this.DefaultExpiry);
        }

        ILock ILockFactory.Create(string resource, TimeSpan? timout, TimeSpan? expiry)
        {
            return this.Create(resource, timout, expiry);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
