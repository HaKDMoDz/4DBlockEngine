using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Structs;
using _4DMonoEngine.Core.Structs.Vector;

namespace _4DMonoEngine.Core.AbstractClasses
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
    }
}
