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
        public List<BlockVertex> VertexList;
        public List<short> IndexList;
        public short Index;
        public abstract void SetDirty();
    }
}
