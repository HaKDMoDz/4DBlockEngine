using System;
using Microsoft.Xna.Framework.Input;

namespace _4DMonoEngine.Core.Events.Args
{
    public class KeyArgs : EventArgs
    {
        public KeyArgs( Keys keyCode )
        {
            KeyCode = keyCode;
        }

        public Keys KeyCode { get; private set; }
    }
    
}
