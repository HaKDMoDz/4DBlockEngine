using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Common.AbstractClasses;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Common.Structs;
using _4DMonoEngine.Core.Debugging.Ingame;
using _4DMonoEngine.Core.Graphics;
using _4DMonoEngine.Core.Utils.Vector;

namespace _4DMonoEngine.Core.Universe
{
    public class CloudTarget : VertexBuilderTarget, IInGameDebuggable
    {
        public BoundingBox RenderingBoundingBox { get; private set; }
        public CloudTarget(Vector3Int position, int sizeX, int sizeY, int sizeZ, int renderScale)
        {
            // calculate the real world position.
            Position = position;

            // calculate bounding-box.
            BoundingBox = new BoundingBox(new Vector3(Position.X, Position.Y, Position.Z),
                new Vector3(Position.X + sizeX, Position.Y + sizeY,
                    Position.Z + sizeZ));

            RenderingBoundingBox = new BoundingBox(new Vector3(Position.X, Position.Y, Position.Z),
               new Vector3(Position.X + sizeX * renderScale, Position.Y + sizeY * renderScale,
                   Position.Z + sizeZ * renderScale));

            // create vertex & index lists.
            VertexList = new List<BlockVertex>();
            IndexList = new List<short>();
        }

        public override void SetDirty()
        {
            //NO OP
        }

        public void DrawInGameDebugVisual(GraphicsDevice graphicsDevice, Camera camera, SpriteBatch spriteBatch, SpriteFont spriteFont)
        {
            BoundingBoxRenderer.Render(BoundingBox, graphicsDevice, camera.View, camera.Projection, Color.DarkRed);
        }
    }
}