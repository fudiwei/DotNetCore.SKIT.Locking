## Guidance

---

### Getting Started

The central object in **SKIT.Locking** is the *ILock* class. You also need a *ILockFactory* to create *ILock*.

Because the *ILockFactory* does a lot, it is designed to be shared and reused between callers. You should not create a *ILockFactory* per operation. It is fully thread-safe and ready for this usage.

**SKIT.Locking** defines an abstraction layer for each type of locks, and divide locks into several types based on their scopes:

* ApplicationDomain: The locks are great for synchronizing between threads in the same .NET application domain.
* OperationSystem: The locks can be used between processes in a single machine.
* DistributedCluster: The locks are available even if the processes are running on different machines.

---

#### Basic Usage

You should create a *ILockFactory* at first:

``` CSharp
using SKIT.Locking;

var lockFactory = new SampleLockFactory();
lockFactory.DefaultTimeout = TimeSpan.FromSeconds(30);
lockFactory.DefaultExpiry = TimeSpan.FromMinutes(10);
```

Note that *ILockFactory* implements *IDisposable* and can be disposed when no longer required. This is deliberately not showing using statement usage, because it is exceptionally rare that you would want to use a *ILockFactory* briefly, as the idea is to re-use this object.

And there is not a *ILockFactory* called *SampleLockFactory* actually. It is for the example purposes only.

Once you have a *ILockFactory*, you can create a *ILock* and try to acquire the lock like so:

``` CSharp
using (var @lock = lockFactory.Create("SampleLockName"))
{
    @lock.Wait();
    if (@lock.IsAcquired)
    {
        // this block of code is protected by the lock!
    }
}
```

Or just like so:

``` CSharp
using (var @lock = lockFactory.CreateAndWait("SampleLockName"))
{
    if (@lock.IsAcquired)
    {
        // this block of code is protected by the lock!
    }
}
```

All locks support asynchronous acquisition:

``` CSharp
using (var @lock = await lockFactory.CreateAndWaitAsync("SampleLockName"))
{
    if (@lock.IsAcquired)
    {
        // this block of code is protected by the lock!
    }
}
```

Note that *ILock* implements *IDisposable* and can be disposed if you want to release the lock.

You can cancel blocking in an asynchronous lock like so:

``` CSharp
var cancellationTokenSource = new CancellationTokenSource();
@lock.WaitAsync(cancellationTokenSource.Token);
/* do something */
cancellationTokenSource.Cancel();
```

---

#### Naming Locks

For all types of locks, the name of the lock defines its identity within its scope. While in general most names will work, the names are ultimately constrained by the underlying technologies used for locking.

---

#### Timeout / Expiry

The thread will be blocked when waiting on a lock, until the lock is acquired, or just time out.

You may have noticed the *DefaultExpiry*. This property is aim to avoid deadlocks when the lock holder shuts down without releasing by a timer policy.

There are two ways to set the timeout and the expiry.

One which is in the example above. The default value will make an effect on every lock.

Or specify the values for each lock.

``` CSharp
var @lock = lockFactory.Create("SampleLockName", TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(10));
```

---

#### Semaphore Locks

The Semaphore lock is great for synchronizing between threads in the same .NET application domains. It is based on *System.Threading.SemaphoreSlim*.

``` CSharp
using SKIT.Locking.Semaphore;

var lockFactory = new SemaphoreLockFactory();
```

---

#### Redis Locks

The Redis lock is useful for ensuring only one process is using a particular resource at any given time. It is based on the Redlock algorithm [(see this for more detail)](http://redis.io/topics/distlock#why-failover-based-implementations-are-not-enough).

This uses an awesome .NET library called [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis).

``` CSharp
using SKIT.Locking.Redis;

var lockFactory = new RedisLockFactory("127.0.0.1:6379");
```

You can also use an existing *IConnectionMultiplexer* object like so:

``` CSharp
IConnectionMultiplexer redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
var lockFactory = new RedisLockFactory(redis, true);
```

---

#### ZooKeeper Locks

The ZooKeeper lock is another kind of locks which designed for the distributed environment. It is based on *EPHEMERAL_SEQUENTIAL* in Apache ZooKeeper.

``` CSharp
using SKIT.Locking.ZooKeeper;

string connString = "127.0.0.1:2181";
var lockFactory = new ZooKeeperLockFactory(connString);
```