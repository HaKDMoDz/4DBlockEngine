using System;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.Common.AbstractClasses;

namespace _4DMonoEngine.Core.Blocks.Dynamic
{
    public abstract class DynamicBlockComponent : Updateable, IComparable<DynamicBlockComponent>, IDisposable
    {

        public int CompareTo(DynamicBlockComponent other)
        {
            int value;
            if(Enabled && !other.Enabled)
            {
                value = -1;
            }
            else if(!Enabled && other.Enabled)
            {
                value = 1;
            }
            else
            {
                value = UpdateOrder - other.UpdateOrder;
            }
            return value;
        }
        public abstract void Dispose();
        public abstract void Initialize();
        public abstract bool Interact();
    }
}
