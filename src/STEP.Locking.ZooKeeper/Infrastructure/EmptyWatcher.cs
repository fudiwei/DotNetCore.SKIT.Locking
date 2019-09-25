using System;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace STEP.Locking.ZooKeeper
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
#if NETSTANDARD
            return Task.CompletedTask;
#else
            return Task.Run(() => {});
#endif
        }
    }
}
