using System;

namespace SKIT.Locking
{
    /// <summary>
    /// 表示锁的作用域。
    /// </summary>
    public enum LockScopes
    {
        /// <summary>
        /// 表示可作用于应用程序域级的锁。
        /// </summary>
        ApplicationDomain,

        /// <summary>
        /// 表示可作用于操作系统级的锁。
        /// </summary>
        OperationSystem,

        /// <summary>
        /// 表示可作用于分布式集群级的锁。
        /// </summary>
        DistributedCluster,
    }
}
