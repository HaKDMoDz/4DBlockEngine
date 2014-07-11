using System;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Events.Args
{
    class Vector2Args : EventArgs
    {
        public Vector2 Vector;
        public Vector2Args(Vector2 vector)
        {
            Vector = vector;
        }
    }
}
