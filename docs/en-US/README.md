# STEP.Locking

An easy way to set up and use system-wide or fully-distributed locks.

It is designed to work with .NET Core and .NET Framework both, and it targets .NET Standard.

---

## Features

* Modern, abstract, asynchronous.
* Supports system-wide or fully-distributed locks.
* Supports naming locks.

---

## Usage

### Example

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

### Usage Guidance

Please click [here](./Guidance.md) for the guidance.

### Documentation Comment

The documentation comment is supported in each public class, method, and property.

😟 However, there is only Simplified Chinese versions is available.

🙂 Welcome developers to contribute other language versions.