using System;

namespace STEP.Locking.Semaphore
{
    internal static class DateTimeOffsetHelper
    {
        public static long ToUnixTimeMilliseconds(DateTimeOffset dateTimeOffset)
        {
#if !NETSTANDARD
            return dateTimeOffset.UtcDateTime.Ticks / 10000000L - 62135596800L;
#else
            return dateTimeOffset.ToUnixTimeMilliseconds();
#endif
        }
    }
}
