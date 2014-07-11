using System;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Events.Args
{
    class Vector3Args : EventArgs
    {
        public Vector3 Vector { get; private set;}
        public Vector3Args(Vector3 vector)
        {
            Vector = vector;
        }
    }
}
