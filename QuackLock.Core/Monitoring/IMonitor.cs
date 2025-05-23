using System;

namespace QuackLock.Core.Monitoring
{
    public interface IMonitor : IDisposable
    {
        void Start();
    }
}
