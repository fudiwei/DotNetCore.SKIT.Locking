# SKIT.Locking

一个简单易用的单机或分布式的锁。

本库以 .NET Standard 为目标平台，可同时支持 .NET Core 和 .NET Framework。

---

## 特性

* 统一抽象层，支持异步调用。
* 支持单机或分布式。
* 支持命名锁。

---

## 用法

### 示例代码

``` CSharp
using (ILockFactory lockFactory = new SemaphoreLockFactory()) 
using (ILock @lock = lockFactory.CreateAndWait())
{
    if (@lock.IsAcquired)
    {
        // this block of code is protected by the lock!
    }
}
```

### 使用手册

[点此](./Guidance.md)查看完整的使用手册。

### 注释

每个公共类、方法和属性已包含详细的文档注释。

😟 由于精力问题，这里只提供了简体中文版本的文档注释。

🙂 欢迎开发者们贡献其他语言版本。