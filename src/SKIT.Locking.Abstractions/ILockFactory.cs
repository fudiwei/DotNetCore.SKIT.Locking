using System;

namespace SKIT.Locking
{
    /// <summary>
    /// 提供锁的工厂方法的接口。
    /// </summary>
    public interface ILockFactory : IDisposable
    {
        /// <summary>
        /// 获取一个值，指示创建出的 <see cref="ILock"/> 对象的作用域。
        /// </summary>
        LockScopes Scope { get; }

        /// <summary>
        /// 获取或设置默认加锁等待超时时间。
        /// </summary>
        TimeSpan DefaultTimeout { get; set; }

        /// <summary>
        /// 获取或设置默认加锁过期时间，避免死锁。
        /// </summary>
        TimeSpan DefaultExpiry { get; set; }

        /// <summary>
        /// 创建并返回一个 <see cref="ILock" /> 对象。
        /// </summary>
        /// <param name="resource">被锁的资源。</param>
        /// <param name="timeout">加锁等待超时时间（默认值 null，将使用默认超时时间）。</param>
        /// <param name="expiry">加锁过期时间（默认值 null，将使用默认过期时间）。</param>
        /// <returns></returns>
        ILock Create(string resource, TimeSpan? timeout = null, TimeSpan? expiry = null);
    }
}
