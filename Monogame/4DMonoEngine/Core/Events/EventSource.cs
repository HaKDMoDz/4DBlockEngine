using System;
using System.Collections.Generic;
using System.Linq;

namespace _4DMonoEngine.Core.Events
{
    class EventSource : IEventSource
    {
        private readonly Dictionary<string, List<WeakReference<Action<EventArgs>>>> m_registeredHandlers;

        public EventSource(IEnumerable<string> eventsFired, bool autoRegisterWithCentral)
        {
            EventsFired = eventsFired;
            m_registeredHandlers = new Dictionary<string, List<WeakReference<Action<EventArgs>>>>();
            EventsEnabled = true;
            if (autoRegisterWithCentral)
            {
                RegisterWithCentral();
            }
        }

        public void FireEvent(string eventName, EventArgs args)
        {
            if (!EventsEnabled)
            {
                return;
            }
            foreach (var registeredHandler in m_registeredHandlers[eventName])
            {
                Action<EventArgs> handler;
                if(registeredHandler.TryGetTarget(out handler))
                {
                    handler(args);
                }
            }
        }

        public void RegisterWithCentral()
        {
            foreach (var eventName in EventsFired)
            {
                Register(eventName, MainEngine.GetEngineInstance().CentralDispatch.GetHandlerForEvent(eventName));
            }
        }

        public void UnregisterWithCentral()
        {
            foreach (var eventName in EventsFired)
            {
                Unregister(eventName, MainEngine.GetEngineInstance().CentralDispatch.GetHandlerForEvent(eventName));
            }
        }

        public IEnumerable<string> EventsFired { get; private set; }

        public bool EventsEnabled { get; set; }
        public void Register(string eventName, Action<EventArgs> handler)
        {
            if (!EventsFired.Contains(eventName))
            {
                return;
            }
            List<WeakReference<Action<EventArgs>>> handlerReferences;
            if (!m_registeredHandlers.TryGetValue(eventName, out handlerReferences))
            {
                handlerReferences = new List<WeakReference<Action<EventArgs>>>();
                m_registeredHandlers.Add(eventName, handlerReferences);
            }
            handlerReferences.Add(new WeakReference<Action<EventArgs>>(handler));
        }

        public void Unregister(string eventName, Action<EventArgs> handler)
        {
            if (!EventsFired.Contains(eventName))
            {
                return;
            }
            List<WeakReference<Action<EventArgs>>> handlerReferences;
            if (!m_registeredHandlers.TryGetValue(eventName, out handlerReferences))
            {
                return;
            }
            for (var i = 0; i < handlerReferences.Count; ++i)
            {
                var handlerReference = handlerReferences[i];
                Action<EventArgs> innerHandler;
                if (!handlerReference.TryGetTarget(out innerHandler) || !innerHandler.Equals(handler))
                {
                    continue;
                }
                handlerReferences.RemoveAt(i);
                break;
            }
        }
    }
}
