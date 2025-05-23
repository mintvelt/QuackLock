using QuackLock.Core.Events;

namespace QuackLock.Core
{
    public interface IDetectionEventHandler
    {
        void OnDetectionEvent(DetectionEvent evt);
    }
}
