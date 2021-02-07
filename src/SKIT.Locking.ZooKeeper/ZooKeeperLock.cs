using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using org.apache.zookeeper;
using org.apache.zookeeper.data;

namespace SKIT.Locking.ZooKeeper
{
    /// <summary>
    /// 一个基于 ZooKeeper 的 <see cref="ILock"/> 的实现。
    /// </summary>
    public sealed partial class ZooKeeperLock : ILock
    {
        private const string ROOT = "/_SKITLOCKING";

        private readonly org.apache.zookeeper.ZooKeeper _zookeeperConnection;
        private readonly TimeSpan _timeout;

        private bool _disposed = false;

        /// <summary>
        /// 获取锁的标识。
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// 获取锁的当前值。
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// 获取一个值，该值锁是否被当前 <see cref="ZooKeeperLock"/> 持有。
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
                    return _zookeeperConnection.getState() == org.apache.zookeeper.ZooKeeper.States.CONNECTING
                        || _zookeeperConnection.getState() == org.apache.zookeeper.ZooKeeper.States.CONNECTED;
                }

                return false;
            } 
        }

        internal ZooKeeperLock(org.apache.zookeeper.ZooKeeper zookeeperConnection, string key, TimeSpan timeout)
        {
            if (zookeeperConnection == null)
                throw new ArgumentNullException(nameof(zookeeperConnection));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            _zookeeperConnection = zookeeperConnection;
            _timeout = timeout;

            Key = key;
            Value = Guid.NewGuid().ToString("N");
        }

        private string GetZooKeeperPath()
        {
            string encodedKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Key)).Replace("+", "-").Replace("/", "_").Replace("=", "");
            return string.Concat(ROOT, "/", encodedKey);
        }

        private async Task<bool> EnsurePathExists(string fullPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            string currentPath = "";
            foreach (string node in fullPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                cancellationToken.ThrowIfCancellationRequested();

                currentPath = $"{currentPath}/{node}";

                try
                {
                    string str = await _zookeeperConnection.createAsync(currentPath, null, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);

                    if (string.IsNullOrEmpty(str))
                        return false;
                }
                catch (KeeperException.NodeExistsException) { }
            }

            return true;
        }

        private async Task<bool> EnsureIsTheLeastNode(string fullPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ChildrenResult childrenResult = await _zookeeperConnection.getChildrenAsync(GetZooKeeperPath(), false);
                if (childrenResult == null)
                    continue;

                SortedSet<string> allNodes = new SortedSet<string>(childrenResult.Children);
                SortedSet<string> lessThenMeNodes = new SortedSet<string>();

                foreach (string node in allNodes)
                {
                    if (fullPath.Equals($"{GetZooKeeperPath()}/{node}"))
                        break;

                    lessThenMeNodes.Add(node);
                }

                if (!lessThenMeNodes.Any())
                {
                    return true;
                }

                Stat existsStat = await _zookeeperConnection.existsAsync($"{GetZooKeeperPath()}/{lessThenMeNodes.Max}", false);
                if (existsStat == null)
                    continue;
            }

            return false;
        }
        
        #region Implements SKIT.Locking.ILock
        /// <summary>
        /// 检查当前 <see cref="ZooKeeperLock"/> 对象是否仍持有锁。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="LockException"></exception>
        public bool CheckLocked()
        {
            return CheckLockedAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步检查当前 <see cref="ZooKeeperLock"/> 对象是否仍持有锁。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="LockException"></exception>
        /// <returns></returns>
        public async Task<bool> CheckLockedAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ZooKeeperLock));

            if (!this.IsAcquired)
                return false;

            try
            {
                DataResult dataResult = await _zookeeperConnection.getDataAsync(GetZooKeeperPath(), false);
                if (dataResult == null || dataResult.Stat == null)
                    return false;

                return string.Equals(Encoding.UTF8.GetString(dataResult.Data), this.Value);
            }
            catch (Exception ex) when (!(ex is ObjectDisposedException))
            {
                throw new LockException("An error occured when ensure locked.", ex);
            }
        }
        #endregion

        #region Implements SKIT.Locking.ILockable
        /// <summary>
        /// 等待加锁，直到超时前始终阻塞，并在等待锁错误时抛出异常。
        /// </summary>
        /// <exception cref="LockException"></exception>
        public ZooKeeperLock Wait()
        {
            return WaitAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 尝试等待加锁，直到超时前始终阻塞。
        /// </summary>
        /// <returns></returns>
        public ZooKeeperLock TryWait()
        {
            try
            {
                return Wait();
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
        public async Task<ZooKeeperLock> WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ZooKeeperLock));

            if (this.IsAcquired)
                return this;

            try
            {
                using (CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(_timeout))
                using (CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token))
                {
                    bool isZnodeCreated = false;
                    string lockPath = null;
                    Stat stat = null;

                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        if (!isZnodeCreated)
                        {
                            isZnodeCreated = await EnsurePathExists(GetZooKeeperPath(), cancellationTokenSource.Token);
                        }

                        if (string.IsNullOrWhiteSpace(lockPath))
                        {
                            lockPath = await _zookeeperConnection.createAsync($"{GetZooKeeperPath()}/i-", null, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL_SEQUENTIAL);
                        }

                        if (stat == null)
                        {
                            stat = await _zookeeperConnection.existsAsync(lockPath);
                        }

                        this.IsAcquired = await this.EnsureIsTheLeastNode(lockPath, cancellationTokenSource.Token);

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
                await _zookeeperConnection.setDataAsync(GetZooKeeperPath(), Encoding.UTF8.GetBytes(this.Value));
            }

            return this;
        }

        /// <summary>
        /// 尝试异步等待加锁，直到超时前始终阻塞。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ZooKeeperLock> TryWaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await WaitAsync(cancellationToken);
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
                    try
                    {
                        _zookeeperConnection.closeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch (KeeperException) { }
                }

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
