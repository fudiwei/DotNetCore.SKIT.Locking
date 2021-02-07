using System;
using System.Threading;

namespace SKIT.Locking.Semaphore
{
    internal static class TimeoutHelper
    {
        public static TimeSpan GetInfiniteTimeSpan()
        {
#if !NET35
            return Timeout.InfiniteTimeSpan;
#else
            return new TimeSpan(0, 0, 0, 0, Timeout.Infinite);
#endif
        }
    }
}
