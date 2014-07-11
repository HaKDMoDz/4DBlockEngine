using System;

namespace _4DMonoEngine.Core.Helpers
{
    public static class EventHelper
    {
        public static Action<EventArgs> Wrap<T>(Action<T> innerFunction) where T : EventArgs
        {
           return args =>
           {
               if (args is T)
               {
                   innerFunction((T) args);
               }
               else
               {
                   throw new Exception("Handler recieved args of type: " + args.GetType() + ", expected type: " + typeof(T));
               }
           };
        }
    }
}
