using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Graphics;

namespace _4DMonoEngine.Core.Common.Enums
{
    internal interface IInGameDebuggable
    {
        void DrawInGameDebugVisual(GraphicsDevice graphicsDevice, ICamera camera, SpriteBatch spriteBatch,
                                   SpriteFont spriteFont);
    }
}
