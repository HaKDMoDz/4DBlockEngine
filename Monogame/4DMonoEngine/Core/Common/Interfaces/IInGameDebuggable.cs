using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Graphics;

namespace _4DMonoEngine.Core.Common.Interfaces
{
    public interface IInGameDebuggable
    {
        void DrawInGameDebugVisual(GraphicsDevice graphicsDevice, Camera camera, SpriteBatch spriteBatch,
                                   SpriteFont spriteFont);
    }
}
