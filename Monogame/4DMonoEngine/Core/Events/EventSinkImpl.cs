using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Events.Args;
using System.Diagnostics;

namespace _4DMonoEngine.Core.Events
{
    public class EventSinkImpl
    {
    	private readonly Dictionary<string, Action<EventArgs>> m_registerdHandlers;

    	public EventSinkImpl()
    	{
    		m_registerdHandlers = new Dictionary<string, Action<EventArgs>>();
    	}

    	public void AddHandler<T>(string eventName, Action<T> handler) where T : EventArgs
    	{
    		var wrapped = EventHelper.Wrap<T>(handler);
    		m_registerdHandlers[eventName] = wrapped;
    	}

        public bool CanHandleEvent(string eventName)
        {
        	return m_registerdHandlers.ContainsKey(eventName);
        }

        public Action<EventArgs> GetHandlerForEvent(string eventName)
        {
        	Debug.Assert(m_registerdHandlers.ContainsKey(eventName));
        	return m_registerdHandlers[eventName];
        }




    }
}
