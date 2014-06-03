

/* Code based on: http://code.google.com/p/xnagameconsole/ */

using System;
using Microsoft.Xna.Framework.Input;

namespace _4DMonoEngine.Core.Debugging.Console
{
    public class KeyEventArgs : EventArgs
    {
        public KeyEventArgs( Keys keyCode )
        {
            KeyCode = keyCode;
        }

        public Keys KeyCode { get; private set; }
    }
    
}
