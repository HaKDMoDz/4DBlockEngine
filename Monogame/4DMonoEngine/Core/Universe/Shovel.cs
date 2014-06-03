using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Common.Vector;
using _4DMonoEngine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Universe
{
    public class Shovel : Equipable
    {

        public Shovel(Game game) : base(game)
        {
        }

        public override void Use()
        {
            m_blockCache.SetBlockAt(_player.AimedSolidBlock.Value.Position,Block.Empty);
        }

        public override void SecondaryUse()
        {
            
        }

        public override void DrawInGameDebugVisual(GraphicsDevice graphicsDevice, ICamera camera, SpriteBatch spriteBatch, SpriteFont spriteFont)
        {
            if (!_player.AimedSolidBlock.HasValue) // make sure we have a solid block.
                return;

            var positionedBlock = _player.AimedSolidBlock.Value;
            var hostChunk = _chunkCache.GetChunkByWorldPosition(positionedBlock.Position.X, positionedBlock.Position.Z);


            var text = string.Format("Block: {0}, Pos: {1}, Chunk: {2}", positionedBlock.Block.ToString(), positionedBlock.Position, hostChunk.ToString());
            

            Vector3 projected = graphicsDevice.Viewport.Project(Vector3.Zero, camera.Projection, camera.View,
                                                                Matrix.CreateTranslation(new Vector3(positionedBlock.Position.X + 0.5f, positionedBlock.Position.Y + 0.5f, positionedBlock.Position.Z + 0.5f)));

            var textSize = spriteFont.MeasureString(text);
            spriteBatch.DrawString(spriteFont, text, new Vector2(projected.X - textSize.X/2, projected.Y - textSize.Y/2), Color.Yellow);
        }
    }
}