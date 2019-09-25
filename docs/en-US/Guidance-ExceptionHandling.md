## Guidance

---

### Exception Handling

**STEP.Locking** throws a *LockException* if an error has occured on lock.

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
    // catch here if you want to handle exceptions.
    // ex.Message contains rich details
    LogError(ex.Message);
}
```

If you just want to ignore errors silently, you can use it like so:

``` CSharp
using (var @lock = lockFactory.Create())
{
    @lock.TryLock();
}
```