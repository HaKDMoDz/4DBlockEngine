using System;

namespace _4DMonoEngine.Core.Events
{
    public interface IEventSink
    {
        bool CanHandleEvent(string eventName);
        Action<EventArgs> GetHandlerForEvent(string eventName);
    }
}
