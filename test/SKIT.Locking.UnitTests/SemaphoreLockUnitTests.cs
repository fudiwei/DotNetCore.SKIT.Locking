using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SKIT.Locking.UnitTests
{
    using Semaphore;

    public class SemaphoreLockUnitTests
    {
        [Fact]
        public void LockTest()
        {
            const string RESOURCE_FOO = "foo";
            const string RESOURCE_BAR = "bar";

            using (ILockFactory lockFactory = new SemaphoreLockFactory())
            {
                lockFactory.DefaultTimeout = TimeSpan.FromSeconds(5);
                Assert.Equal(LockScopes.ApplicationDomain, lockFactory.Scope);

                using (ILock lock1 = lockFactory.CreateAndWait(RESOURCE_FOO))
                {
                    Assert.True(lock1.IsAcquired);

                    using (ILock lock2 = lockFactory.CreateAndWait(RESOURCE_FOO))
                    {
                        Assert.False(lock2.IsAcquired);
                    }

                    using (ILock lock3 = lockFactory.CreateAndWait(RESOURCE_BAR))
                    {
                        Assert.True(lock3.IsAcquired);
                    }
                }

                using (ILock lock4 = lockFactory.CreateAndWait(RESOURCE_FOO))
                {
                    Assert.True(lock4.IsAcquired);
                }
            }
        }

        [Fact]
        public async Task LockAsyncTest()
        {
            const string RESOURCE_FOO = "foo";
            const string RESOURCE_BAR = "bar";

            using (ILockFactory lockFactory = new SemaphoreLockFactory())
            {
                lockFactory.DefaultTimeout = TimeSpan.FromSeconds(5);

                using (ILock lock1 = await lockFactory.CreateAndWaitAsync(RESOURCE_FOO))
                {
                    Assert.True(lock1.IsAcquired);

                    using (ILock lock2 = await lockFactory.CreateAndWaitAsync(RESOURCE_FOO))
                    {
                        Assert.False(lock2.IsAcquired);
                    }

                    using (ILock lock3 = await lockFactory.CreateAndWaitAsync(RESOURCE_BAR))
                    {
                        Assert.True(lock3.IsAcquired);
                    }
                }

                using (ILock lock4 = await lockFactory.CreateAndWaitAsync(RESOURCE_FOO))
                {
                    Assert.True(lock4.IsAcquired);
                }
            }
        }

        [Fact]
        public void ConcurrentTest()
        {
            const string RESOURCE = "baz";
            TimeSpan timeout = TimeSpan.FromSeconds(1);
            TimeSpan expiry = TimeSpan.FromMilliseconds(500);

            using (ILockFactory lockFactory = new SemaphoreLockFactory())
            {
                int counter = 0;
                Stopwatch watch = new Stopwatch();

                const double RATIO = 1.5;
                const int MAX_COUNT = 64;

                watch.Start();

                ParallelLoopResult parallel = Parallel.For(0, MAX_COUNT, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
                {
                    while (true)
                    {
                        using (ILock @lock = lockFactory.CreateAndTryWait(RESOURCE, timeout, expiry))
                        {
                            if (!@lock.IsAcquired)
                                continue;

                            if (counter == i)
                            {
                                Thread.Sleep((int)(expiry.TotalMilliseconds * RATIO));
                                counter = i + 1;
                                break;
                            }
                        }
                    }
                });

                while (!parallel.IsCompleted) ;

                watch.Stop();

                Assert.Equal(MAX_COUNT, counter);
                Assert.True(watch.ElapsedMilliseconds >= expiry.TotalMilliseconds * RATIO * MAX_COUNT);
            }
        }
    }
}
