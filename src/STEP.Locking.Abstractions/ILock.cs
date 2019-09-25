using System;
using System.Threading;
#if !NET35
using System.Threading.Tasks;
#endif

namespace STEP.Locking
{
    /// <summary>
    /// 提供锁信息的接口。
    /// </summary>
    public interface ILock : ILockable, IDisposable
    {
        /// <summary>
		/// 获取锁的标识。
		/// </summary>
		string Key { get; }

        /// <summary>
        /// 获取锁在当前 <see cref="ILock"/> 对象的值。
        /// </summary>
        string Value { get; }

        /// <summary>
        /// 获取一个值，该值锁是否被当前 <see cref="ILock"/> 对象持有。
        /// </summary>
        bool IsAcquired { get; }

        /// <summary>
        /// 获取一个值，该值锁是否可续约。
        /// </summary>
        bool IsRenewable { get; }

        /// <summary>
        /// 检查当前 <see cref="ILockable"/> 对象是否仍持有锁。
        /// </summary>
        /// <returns></returns>
        bool CheckLocked();

#if !NET35
        /// <summary>
        /// 异步检查当前 <see cref="ILockable"/> 对象是否仍持有锁。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> CheckLockedAsync(CancellationToken cancellationToken = default(CancellationToken));
#endif
    }
}
