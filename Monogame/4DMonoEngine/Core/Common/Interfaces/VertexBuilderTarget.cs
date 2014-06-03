using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Vector;

namespace _4DMonoEngine.Core.Common.Enums
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
