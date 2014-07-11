using System.Collections.Generic;
using Microsoft.Xna.Framework;
using _4DMonoEngine.Core.AbstractClasses;
using _4DMonoEngine.Core.Structs;
using _4DMonoEngine.Core.Structs.Vector;

namespace _4DMonoEngine.Core.Universe
{
    public class CloudTarget : VertexBuilderTarget
    {
        public CloudTarget(Vector3Int position, int sizeInBlocks)
        {
            // calculate the real world position.
            Position = position;

            // calculate bounding-box.
            BoundingBox = new BoundingBox(new Vector3(Position.X, Position.Y, Position.Z),
                new Vector3(Position.X + sizeInBlocks, Position.Y + sizeInBlocks,
                    Position.Z + sizeInBlocks));

            // create vertex & index lists.
            VertexList = new List<BlockVertex>();
            IndexList = new List<short>();
        }
    }
}