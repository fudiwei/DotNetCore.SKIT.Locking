using System;

namespace SKIT.Locking
{
    /// <summary>
    /// 表示由锁引发的异常基类。
    /// </summary>
    public class LockException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public LockException()
            : base()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public LockException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public LockException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
