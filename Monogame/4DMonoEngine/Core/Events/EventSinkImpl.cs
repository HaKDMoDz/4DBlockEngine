using System;

namespace _4DMonoEngine.Core.Events
{
    public class EventSinkImpl
    {
    	private readonly Dictionaty<string, Action<EventArgs>> m_registerdHandlers;

    	public EventSinkImpl()
    	{
    		m_registerdHandlers = new Dictionaty<string, Action<EventArgs>>();
    	}

    	public void AddHandler<T>(string eventName, Action<T> handler) where T : EventArgs
    	{
    		var wrapped = EventHelper.Wrap<Vector3Args>(handler);
    		m_registerdHandlers[eventName] = wrapped;
    	}

        public bool CanHandleEvent(string eventName)
        {
        	return m_registerdHandlers.ContainsKey(eventName);
        }

        Action<EventArgs> GetHandlerForEvent(string eventName)
        {
        	Debug.Assert(m_registerdHandlers.ContainsKey(eventName));
        	return m_registerdHandlers[eventName];
        }




    }
}
