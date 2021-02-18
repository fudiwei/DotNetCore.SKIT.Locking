using System;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace SKIT.Locking.ZooKeeper
{
    internal class EmptyWatcher : Watcher
    {
        public static readonly EmptyWatcher Instance = new EmptyWatcher();

        private EmptyWatcher()
            : base()
        { 
        }

        public override Task process(WatchedEvent e)
        {
#if NETFRAMEWORK
            return Task.Delay(0);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
