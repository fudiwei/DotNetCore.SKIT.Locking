using System;
using System.Collections;
using System.Collections.Generic;

namespace STEP.Locking.Semaphore
{
    internal class ExSemaphoreStorage : IDictionary<string, ExSemaphore>
    {
        public static readonly ExSemaphoreStorage Instance = new ExSemaphoreStorage();

#if !NET35
        private readonly NonBlocking.ConcurrentDictionary<string, ExSemaphore> _dict = new NonBlocking.ConcurrentDictionary<string, ExSemaphore>();
#else
        private readonly object _lockObj = new object();
        private readonly IDictionary<string, ExSemaphore> _dict = new Dictionary<string, ExSemaphore>();
#endif

        public ExSemaphore this[string key]
        {
            get
            {
#if !NET35
                return _dict[key];
#else
                lock (_lockObj)
                {
                    return _dict[key];
                }
#endif
            }
            set { _dict[key] = value; }
        }

        public ICollection<string> Keys { get { return _dict.Keys; } }

        public ICollection<ExSemaphore> Values { get { return _dict.Values; } }

        public int Count { get { return _dict.Count; } }

        public bool IsReadOnly { get { return ((IDictionary<string, ExSemaphore>)_dict).IsReadOnly; } }

        private ExSemaphoreStorage()
        { 
        }

        public void Add(string key, ExSemaphore value)
        {
            _dict.Add(key, value);
        }

        public void Add(KeyValuePair<string, ExSemaphore> item)
        {
            ((IDictionary<string, ExSemaphore>)_dict).Add(item);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<string, ExSemaphore> item)
        {
            return ((IDictionary<string, ExSemaphore>)_dict).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, ExSemaphore>[] array, int arrayIndex)
        {
            ((IDictionary<string, ExSemaphore>)_dict).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, ExSemaphore>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _dict.Remove(key);
        }

        public bool Remove(KeyValuePair<string, ExSemaphore> item)
        {
            return ((IDictionary<string, ExSemaphore>)_dict).Remove(item);
        }

        public bool TryGetValue(string key, out ExSemaphore value)
        {
            return _dict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public ExSemaphore GetOrAdd(string key, string value)
        {
#if !NET35
            return _dict.GetOrAdd(key, new ExSemaphore(value));
#else
            lock (_lockObj)
            {
                if (!_dict.TryGetValue(key, out ExSemaphore semaphore))
                {
                    semaphore = new ExSemaphore(value);

                    try
                    {
                        _dict.Add(key, semaphore);
                    } 
                    catch (ArgumentException)
                    {
                        if (!_dict.TryGetValue(key, out semaphore))
                            throw;
                    }
                }
                return semaphore;
            }
#endif
        }
    }
}
