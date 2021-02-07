using System;

namespace SKIT.Locking.Redis
{
    internal static class DateTimeOffsetHelper
    {
        public static long ToUnixTimeMilliseconds(DateTimeOffset dateTimeOffset)
        {
#if NETFRAMEWORK
            return dateTimeOffset.UtcDateTime.Ticks / 10000000L - 62135596800L;
#else
            return dateTimeOffset.ToUnixTimeMilliseconds();
#endif
        }
    }
}
