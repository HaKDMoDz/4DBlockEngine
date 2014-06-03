using _4DMonoEngine.Core.Common.Enums;
using _4DMonoEngine.Core.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Universe
{
    public abstract class Equipable : IItem
    {
        public abstract void Use();

        public abstract void SecondaryUse();

        public abstract void DrawInGameDebugVisual(GraphicsDevice graphicsDevice, ICamera camera, SpriteBatch spriteBatch,
            SpriteFont spriteFont);
    }
}