using System;

namespace _4DMonoEngine.Core.Events.Args
{
    class MouseButtonArgs : EventArgs
    {
        public enum MouseButtons
        {
            Left,
            Right
        }

        public MouseButtonArgs(MouseButtons mouseButton)
        {
            MouseButton = mouseButton;
        }

        public MouseButtons MouseButton { get; set; }
    }
}
