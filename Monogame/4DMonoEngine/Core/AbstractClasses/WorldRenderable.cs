using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Graphics;

namespace _4DMonoEngine.Core.AbstractClasses
{
    public abstract class WorldRenderable : Renderable
    {
        public delegate float GetTimeOfDay();
        public delegate Vector2 GetFogVector();
        protected GetTimeOfDay m_getTimeOfDay;
        protected GetFogVector m_getFogVector;
        protected GraphicsDevice m_graphicsDevice;
        protected Camera m_camera;
        public bool Initialized { get; private set; } 

        public virtual void Initialize(GraphicsDevice graphicsDevice, Camera camera, GetTimeOfDay getTimeOfDay, GetFogVector getFogVector)
        {
            m_getTimeOfDay = getTimeOfDay;
            m_getFogVector = getFogVector;
            m_graphicsDevice = graphicsDevice;
            m_camera = camera;
            Initialized = true;
        }
    }
}
