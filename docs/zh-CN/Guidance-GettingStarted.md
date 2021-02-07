## 使用手册

---

### 开始入门

**SKIT.Locking** 的核心是通过 *ILockFactory* 创建的 *ILock* 对象。

因为 *ILockFactory* 有一定开销，因此它被设计为被整个应用程序域共享和重用的。强烈建议使用单例模式，而不是在每次操作中不停的创建该对象的实例。该对象是线程安全的。

**SKIT.Locking** 为每种锁定义了一个抽象层，并根据它们的作用域分为几种类型：

* 应用程序域级：这类锁只用于同一个 .NET 应用程序域的线程同步。
* 操作系统级：这类锁可以在单机环境下用于进程间同步。
* 分布式集群级：这类锁可以用于分布式集群环境下的多进程同步。

---

#### 基础用法

首先，你应该实例化一个 *ILockFactory* 对象。

``` CSharp
using SKIT.Locking;

var lockFactory = new SampleLockFactory();
lockFactory.DefaultTimeout = TimeSpan.FromSeconds(30);
lockFactory.DefaultExpiry = TimeSpan.FromMinutes(10);
```

需注意的是， *ILockFactory* 实现了 *IDisposable* 接口。本例中特意没有使用 using 语句的用法。再次强调，你应该使用单例模式。

其实这里并没有一个 *ILockFactory* 真的叫做 *SampleLockFactory*。它仅仅是为了演示而存在。

一旦你拥有了一个 *ILockFactory* 对象，你就可以创建一个 *ILock* 对象，并像这样尝试持有锁：

``` CSharp
using (var @lock = lockFactory.Create("SampleLockName"))
{
    @lock.Wait();
    if (@lock.IsAcquired)
    {
        // 本代码块将被锁保护
    }
}
```

或者像这样的简便用法：

``` CSharp
using (var @lock = lockFactory.CreateAndWait("SampleLockName"))
{
    if (@lock.IsAcquired)
    {
        // 本代码块将被锁保护
    }
}
```

这些锁也支持异步用法：

``` CSharp
using (var @lock = await lockFactory.CreateAndWaitAsync("SampleLockName"))
{
    if (@lock.IsAcquired)
    {
        // 本代码块将被锁保护
    }
}
```

需注意的是，*ILock* 实现了 *IDisposable* 接口，当它的实例被销毁时，锁也将被释放。

你可以在异步方法中取消因加锁产生的阻塞：

``` CSharp
var cancellationTokenSource = new CancellationTokenSource();
@lock.WaitAsync(cancellationTokenSource.Token);
/* do something */
cancellationTokenSource.Cancel();
```

---

#### 命名锁

对于所有类型的锁，锁的名称就是其作用域内的标识。虽然你可以可以指定任意名称，但这些名称最终会受到其底层技术的约束。

---

#### 超时 / 过期

直到成功取得锁或超时前，线程将被阻塞，在此期间会反复尝试取得锁。

也许你已经注意到了上面出现的 *DefaultExpiry*。该属性是为了防止因锁的持有者意外退出、锁未能释放而导致的死锁，这里采用了一种定时回收的策略。

有两种方式可以设置超时或过期时间。一种就是像上面的例子一样设置默认值，这将影响每个锁；另一种就是为每个所单独指定值：

``` CSharp
var @lock = lockFactory.Create("SampleLockName", TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(10));
```

---

#### Semaphore 锁

Semaphore 锁是一种应用程序域级的锁，它基于 *System.Threading.SemaphoreSlim*。

``` CSharp
using SKIT.Locking.Semaphore;

var lockFactory = new SemaphoreLockFactory();
```

---

#### Redis 锁

Redis 锁是为分布式环境设计的，在单位时间内想要确保只有一个进程可以访问特定资源的情况下很有用，它基于 Redlock 算法[（点此查看详情）](http://redis.io/topics/distlock#why-failover-based-implementations-are-not-enough)。

该功能依赖一个非常优秀的 .NET 库 —— [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)。

``` CSharp
using SKIT.Locking.Redis;

var lockFactory = new RedisLockFactory("127.0.0.1:6379");
```

也可以传入一个已存在的 *IConnectionMultiplexer* 对象：

``` CSharp
IConnectionMultiplexer redis = ConnectionMultiplexer.Connect("127.0.0.1:6379");
var lockFactory = new RedisLockFactory(redis, true);
```

---

#### ZooKeeper 锁

Redis 锁也是为分布式环境设计得，它基于 Apache ZooKeeper 的临时顺序节点。

``` CSharp
using SKIT.Locking.ZooKeeper;

string connString = "127.0.0.1:2181";
var lockFactory = new ZooKeeperLockFactory(connString);
```