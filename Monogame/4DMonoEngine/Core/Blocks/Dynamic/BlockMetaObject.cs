using System.Collections.Generic;
using _4DMonoEngine.Core.Common.Structs.Vector;

namespace _4DMonoEngine.Core.Blocks.Dynamic
{
    public abstract class BlockMetaObject
    {
        protected List<Vector3Int> m_blockPositions;
        public ushort Id { get; private set; }

        protected BlockMetaObject(ushort id)
        {
            Id = id;
        }

        public abstract void RemoveBlock(Vector3Int position);
    }
}
