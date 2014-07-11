using System;
using System.Collections.Generic;

namespace _4DMonoEngine.Core.Events
{
    public interface IEventSource
    {
        IEnumerable<string> EventsFired { get; }
        bool EventsEnabled { get; set; }
        void Register(string eventName, Action<EventArgs> handler);
        void Unregister(string eventName, Action<EventArgs> handler);
    }
}
