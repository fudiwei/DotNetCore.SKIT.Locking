## 使用手册

---

### 异常处理

**SKIT.Locking** 会在尝试取得锁期间发生错误时抛出一个 *LockException*。

``` CSharp
try 
{
    using (var @lock = lockFactory.Create())
    {
        @lock.Lock();
    }
}
catch (LockException ex) 
{
    // 在此处理错误
    // ex.Message 包含更多细节信息
    LogError(ex.Message);
}
```

如果你只想静默地忽略这些错误而不去处理它们，你可以像这样：

``` CSharp
using (var @lock = lockFactory.Create())
{
    @lock.TryLock();
}
```