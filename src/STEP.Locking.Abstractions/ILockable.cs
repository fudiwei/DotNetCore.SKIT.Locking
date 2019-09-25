using System;
using System.Threading;
#if !NET35
using System.Threading.Tasks;
#endif

namespace STEP.Locking
{
    /// <summary>
    /// 提供锁等待的方法的接口。
    /// </summary>
    public interface ILockable
    {
        /// <summary>
        /// 等待加锁，直到超时前始终阻塞，并在等待锁错误时抛出异常。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="LockException"></exception>
        ILock Wait();

        /// <summary>
        /// 尝试等待加锁，直到超时前始终阻塞。
        /// </summary>
        /// <returns></returns>
        ILock TryWait();

#if !NET35
        /// <summary>
        /// 异步等待加锁，直到超时前始终阻塞，并在等待锁错误时抛出异常。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="LockException"></exception>
        Task<ILock> WaitAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// 尝试异步等待加锁，直到超时前始终阻塞。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ILock> TryWaitAsync(CancellationToken cancellationToken = default(CancellationToken));
#endif
    }
}
