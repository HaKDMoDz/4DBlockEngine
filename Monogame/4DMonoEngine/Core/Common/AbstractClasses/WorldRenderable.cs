using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Graphics;

namespace _4DMonoEngine.Core.Common.AbstractClasses
{
    public abstract class WorldRenderable : Renderable
    {
        public delegate float GetTimeOfDay();
        public delegate Vector2 GetFogVector();
        protected GetTimeOfDay m_getTimeOfDay;
        protected GetFogVector m_getFogVector;
        protected GraphicsDevice m_graphicsDevice;
        protected Camera m_camera;

        protected WorldRenderable(Game game)
        {
            Game = game;
        }

        public bool Initialized { get; private set; }
        public Game Game { get; private set; }

        public virtual void Initialize(GraphicsDevice graphicsDevice, Camera camera, GetTimeOfDay getTimeOfDay, GetFogVector getFogVector)
        {
            m_getTimeOfDay = getTimeOfDay;
            m_getFogVector = getFogVector;
            m_graphicsDevice = graphicsDevice;
            m_camera = camera;
            Initialized = true;
        }

        public Vector4 GetSunColor()
        {
            var time = m_getTimeOfDay();
            Vector4 retColor;
            if (time < 6)
            {
                retColor = RenderingConstants.NightColor;
            }
            else if (time < 8)
            {
                retColor = Vector4.Lerp(RenderingConstants.NightColor, RenderingConstants.DayColor, (time - 6) / 4);
                retColor = Vector4.Lerp(retColor, RenderingConstants.MorningTint, (time - 6) / 4);
            }
            else if (time < 10)
            {
                retColor = Vector4.Lerp(RenderingConstants.NightColor, RenderingConstants.DayColor, (time - 6) / 4);
                retColor = Vector4.Lerp(retColor, RenderingConstants.MorningTint, 1 - (time - 6) / 4);
            }
            else if (time < 16)
            {
                retColor = RenderingConstants.DayColor;
            }
            else if (time < 18)
            {
                retColor = Vector4.Lerp(RenderingConstants.DayColor, RenderingConstants.NightColor, (time - 16) / 4);
                retColor = Vector4.Lerp(retColor, RenderingConstants.EveningTint, (time - 16) / 4);
            }
            else if (time < 20)
            {
                retColor = Vector4.Lerp(RenderingConstants.DayColor, RenderingConstants.NightColor, (time - 16) / 4);
                retColor = Vector4.Lerp(retColor, RenderingConstants.EveningTint, 1 - (time - 16) / 4);
            }
            else
            {
                retColor = RenderingConstants.NightColor;
            }
            return retColor;
        }

        public Vector4 GetHorizonColor()
        {
            var time = m_getTimeOfDay();
            Vector4 retColor;
            if (time < 6)
            {
                retColor = RenderingConstants.NightHorizonColor;
            }
         /*   else if (time < 8)
            {
                retColor = Vector4.Lerp(RenderingConstants.NightHorizonColor, RenderingConstants.MorningHorizonTint, (time - 6) / 2);
            }*/
            else if (time < 10)
            {
                // retColor = Vector4.Lerp(RenderingConstants.MorningHorizonTint, RenderingConstants.DayHorizonColor, (time - 8) / 2);
                retColor = Vector4.Lerp(RenderingConstants.NightHorizonColor, RenderingConstants.DayHorizonColor, (time - 6) / 4);
            }
            else if (time < 16)
            {
                retColor = RenderingConstants.DayHorizonColor;
            }
            /*else if (time < 18)
            {
                retColor = Vector4.Lerp(RenderingConstants.DayHorizonColor, RenderingConstants.EveningHorizonTint, (time - 16) / 2);
            }*/
            else if (time < 20)
            {
                // retColor = Vector4.Lerp(RenderingConstants.EveningHorizonTint, RenderingConstants.NightHorizonColor, (time - 18) / 2);
                retColor = Vector4.Lerp(RenderingConstants.DayHorizonColor, RenderingConstants.NightHorizonColor, (time - 16) / 4);
            }
            else
            {
                retColor = RenderingConstants.NightHorizonColor;
            }
            return retColor;
        }
    }
}
