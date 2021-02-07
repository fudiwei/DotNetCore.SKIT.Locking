using System;
using System.Threading;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace SKIT.Locking.ZooKeeper
{
    /// <summary>
    /// 用于创建一个 <see cref="ZooKeeperLock"/> 对象。
    /// </summary>
    public sealed partial class ZooKeeperLockFactory : ILockFactory, IDisposable
    {
        private readonly string _zookeeperConnectionString;
        private bool _disposed = false;

        /// <summary>
        /// 获取一个值，指示创建出的 <see cref="ZooKeeperLock"/> 对象的作用域。
        /// </summary>
        public LockScopes Scope { get { return LockScopes.DistributedCluster; } }

        /// <summary>
        /// 获取或设置默认加锁等待超时时间。
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = Timeout.InfiniteTimeSpan;

        /// <summary>
        /// 获取或设置默认加锁过期时间，避免死锁。
        /// </summary>
        public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="zookeeperConnectionString"></param>
        public ZooKeeperLockFactory(string zookeeperConnectionString)
        {
            if (string.IsNullOrWhiteSpace(zookeeperConnectionString))
                throw new ArgumentNullException(nameof(zookeeperConnectionString));

            _zookeeperConnectionString = zookeeperConnectionString;
        }

        /// <summary>
        /// 创建并返回一个 <see cref="ZooKeeperLock" /> 对象。
        /// </summary>
        /// <param name="resource">被锁的资源。</param>
        /// <param name="timeout">加锁等待超时时间（默认值 null，将使用默认超时时间）。</param>
        /// <returns></returns>
        public ZooKeeperLock Create(string resource, TimeSpan? timeout = null)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            if (_disposed)
                throw new ObjectDisposedException(nameof(ZooKeeperLockFactory));

            org.apache.zookeeper.ZooKeeper connection = new org.apache.zookeeper.ZooKeeper(_zookeeperConnectionString, (int)DefaultExpiry.TotalMilliseconds, EmptyWatcher.Instance);
            return new ZooKeeperLock(connection, resource, timeout ?? DefaultTimeout);
        }

        ILock ILockFactory.Create(string resource, TimeSpan? timout, TimeSpan? expiry)
        {
            return this.Create(resource, timout);
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
