using System;
using System.Threading;
using StackExchange.Redis;

namespace STEP.Locking.Redis
{
    /// <summary>
    /// 用于创建一个 <see cref="RedisLock"/> 对象。
    /// </summary>
    public sealed class RedisLockFactory : ILockFactory, IDisposable
    {
        private readonly IDatabase _redisDatabase;
        private readonly bool _disposeConnection;
        private bool _disposed = false;

        /// <summary>
        /// 获取一个值，指示创建出的 <see cref="RedisLock"/> 对象的作用域。
        /// </summary>
        public LockScopes Scope { get { return LockScopes.DistributedCluster; } }

        /// <summary>
        /// 获取或设置默认加锁等待超时时间。
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = Timeout.InfiniteTimeSpan;

        /// <summary>
        /// 获取或设置默认加锁过期时间，避免死锁。
        /// </summary>
        public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisConnectionString"></param>
        public RedisLockFactory(string redisConnectionString)
            : this(ConnectionMultiplexer.Connect(redisConnectionString), true)
        {
            _disposeConnection = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisConnectionOptions"></param>
        public RedisLockFactory(ConfigurationOptions redisConnectionOptions)
            : this(ConnectionMultiplexer.Connect(redisConnectionOptions), true)
        {
            _disposeConnection = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisConnection"></param>
        /// <param name="disposeConnection"></param>
        public RedisLockFactory(IConnectionMultiplexer redisConnection, bool disposeConnection = false)
        {
            if (redisConnection == null)
                throw new ArgumentNullException(nameof(redisConnection));

            int idx = 0;
            try { idx = ConfigurationOptions.Parse(redisConnection.Configuration).DefaultDatabase.GetValueOrDefault(); } catch { }
            _redisDatabase = redisConnection.GetDatabase(idx);
            _disposeConnection = disposeConnection;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="redisDatabase"></param>
        /// <param name="disposeConnection"></param>
        public RedisLockFactory(IDatabase redisDatabase, bool disposeConnection = false)
        {
            if (redisDatabase == null)
                throw new ArgumentNullException(nameof(redisDatabase));

            _redisDatabase = redisDatabase;
            _disposeConnection = disposeConnection;
        }

        /// <summary>
        /// 创建并返回一个 <see cref="RedisLock" /> 对象。
        /// </summary>
        /// <param name="resource">被锁的资源。</param>
        /// <param name="timeout">加锁等待超时时间（默认值 null，将使用默认超时时间）。</param>
        /// <param name="expiry">加锁过期时间（默认值 null，将使用默认过期时间）。</param>
        /// <returns></returns>
        public RedisLock Create(string resource, TimeSpan? timeout = null, TimeSpan? expiry = null)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            if (_disposed)
                throw new ObjectDisposedException(nameof(RedisLockFactory));

            return new RedisLock(_redisDatabase, resource, timeout ?? this.DefaultTimeout, expiry ?? this.DefaultExpiry);
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
                if (_disposeConnection)
                {
                    _redisDatabase?.Multiplexer?.Dispose();
                }

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
