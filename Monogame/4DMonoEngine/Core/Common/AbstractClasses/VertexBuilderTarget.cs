using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Common.Structs;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Common.AbstractClasses
{
    public abstract class VertexBuilderTarget
    {
        public BoundingBox BoundingBox;
        public Vector3Int Position;
        public VertexBuffer VertexBuffer;
        public IndexBuffer IndexBuffer;
        public readonly List<BlockVertex> VertexList;
        public readonly List<short> IndexList;
        public short Index;
        public abstract void SetDirty(int x, int y, int z);

        protected VertexBuilderTarget()
        {
            // create vertex & index lists.
            VertexList = new List<BlockVertex>();
            IndexList = new List<short>();
        }
    }
}
