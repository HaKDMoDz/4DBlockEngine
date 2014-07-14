using System;
using System.Collections.Generic;
using System.Linq;

namespace _4DMonoEngine.Core.Events
{
    public sealed class EventDispatcher : IEventDispatcher
    {
        private readonly Dictionary<string, List<WeakReference<Action<EventArgs>>>> m_registeredHandlers;
        private readonly Dictionary<string, Action<EventArgs>> m_handlerWrappers;

        public EventDispatcher()
        {
            m_registeredHandlers = new Dictionary<string, List<WeakReference<Action<EventArgs>>>>();
            m_handlerWrappers = new Dictionary<string, Action<EventArgs>>();
            EventsEnabled = true;
        }

        public bool CanHandleEvent(string eventName)
        {
            return true;
        }

        public Action<EventArgs> GetHandlerForEvent(string eventName)
        {
            Action<EventArgs> handler;
            if (!m_handlerWrappers.TryGetValue(eventName, out handler))
            {
                handler = BuildHandlerWrapper(eventName);
                m_handlerWrappers.Add(eventName, handler);
            }
            return handler;
        }

        private Action<EventArgs> BuildHandlerWrapper(string eventName)
        {
            return args =>
            {
                if (EventsEnabled)
                {
                    List<WeakReference<Action<EventArgs>>> handlerReferences;
                    if (m_registeredHandlers.TryGetValue(eventName, out handlerReferences))
                    {
                        for (var i = 0; i < handlerReferences.Count; ++i)
                        {
                            var handlerReference = handlerReferences[i];
                            Action<EventArgs> innerHandler;
                            if (handlerReference.TryGetTarget(out innerHandler))
                            {
                                innerHandler(args);
                            }
                            else
                            {
                                handlerReferences.RemoveAt(i--);
                            }
                        }
                    }
                }
            };
        }

        IEnumerable<string> IEventSource.EventsFired
        {
            get
            {
                return m_registeredHandlers.Keys.ToArray();
            }
        }

        public bool EventsEnabled { get; set; }
        public void Register(string eventName, Action<EventArgs> handler)
        {
            List<WeakReference<Action<EventArgs>>> handlerReferences;
            if (!m_registeredHandlers.TryGetValue(eventName, out handlerReferences))
            {
                handlerReferences = new List<WeakReference<Action<EventArgs>>>();
                m_registeredHandlers.Add(eventName, handlerReferences);
            }
            handlerReferences.Add(new WeakReference<Action<EventArgs>>(handler));
            if (!m_handlerWrappers.ContainsKey(eventName))
            {
                m_handlerWrappers.Add(eventName, BuildHandlerWrapper(eventName));
            }
        }

        public void Unregister(string eventName, Action<EventArgs> handler)
        {
            List<WeakReference<Action<EventArgs>>> handlerReferences;
            if (m_registeredHandlers.TryGetValue(eventName, out handlerReferences))
            {
                for (var i = 0; i < handlerReferences.Count; ++i)
                {
                    var handlerReference = handlerReferences[i];
                    Action<EventArgs> innerHandler;
                    if (handlerReference.TryGetTarget(out innerHandler) && innerHandler.Equals(handler))
                    {
                        handlerReferences.RemoveAt(i--);
                        break;
                    }
                }
            }
        }
    }
}
