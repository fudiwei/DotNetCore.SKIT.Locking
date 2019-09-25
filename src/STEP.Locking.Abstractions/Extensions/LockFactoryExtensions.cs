using System;
using System.Threading;
#if !NET35
using System.Threading.Tasks;
#endif

namespace STEP.Locking
{
    /// <summary>
    /// 
    /// </summary>
    public static class LockFactoryExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lockFactory"></param>
        /// <param name="resource"></param>
        /// <param name="timeout"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public static ILock CreateAndWait(this ILockFactory lockFactory, string resource, TimeSpan? timeout = null, TimeSpan? expiry = null)
        {
            return lockFactory.Create(resource, timeout, expiry).Wait();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lockFactory"></param>
        /// <param name="resource"></param>
        /// <param name="timeout"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public static ILock CreateAndTryWait(this ILockFactory lockFactory, string resource, TimeSpan? timeout = null, TimeSpan? expiry = null)
        {
            return lockFactory.Create(resource, timeout, expiry).TryWait();
        }

#if !NET35
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lockFactory"></param>
        /// <param name="resource"></param>
        /// <param name="timeout"></param>
        /// <param name="expiry"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<ILock> CreateAndWaitAsync(this ILockFactory lockFactory, string resource, TimeSpan? timeout = null, TimeSpan? expiry = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await lockFactory.Create(resource, timeout, expiry).WaitAsync(cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lockFactory"></param>
        /// <param name="resource"></param>
        /// <param name="timeout"></param>
        /// <param name="expiry"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<ILock> CreateAndTryWaitAsync(this ILockFactory lockFactory, string resource, TimeSpan? timeout = null, TimeSpan? expiry = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await lockFactory.Create(resource, timeout, expiry).TryWaitAsync(cancellationToken);
        }
#endif
    }
}
