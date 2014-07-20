using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using _4DMonoEngine.Core.Common.Interfaces;
using _4DMonoEngine.Core.Graphics;

namespace _4DMonoEngine.Core.Debugging.Ingame
{
    public sealed class InGameDebugger : DrawableGameComponent
    {
        private SpriteBatch m_spriteBatch;
        private SpriteFont m_spriteFont;
        private readonly Camera m_camera;
        private bool m_active;

        private readonly List<WeakReference<IInGameDebuggable>> m_debugList;
        private readonly ConcurrentQueue<IInGameDebuggable>  m_debugQueue; 

        public InGameDebugger(Game game, Camera camera)
            : base(game)
        {
            m_debugList = new List<WeakReference<IInGameDebuggable>>();
            m_debugQueue = new ConcurrentQueue<IInGameDebuggable>();
            m_camera = camera;
        }

        public override void Initialize()
        {
            m_spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            m_spriteFont = MainEngine.GetEngineInstance().GetAsset<SpriteFont>("Verdana");
        }

        public override void Draw(GameTime gameTime)
        {
           
                IInGameDebuggable tryReference;
                while (m_debugQueue.TryDequeue(out tryReference))
                {
                    foreach (var weakReference in m_debugList)
                    {
                        IInGameDebuggable outDebug;
                        if (!weakReference.TryGetTarget(out outDebug))
                        {
                            weakReference.SetTarget(tryReference);
                            goto Continue;
                        }
                    }
                    m_debugList.Add(new WeakReference<IInGameDebuggable>(tryReference));
                Continue:
                    ;
                }
            if (m_active)
            {
                m_spriteBatch.Begin();
                foreach (var weakReference in m_debugList)
                {
                    IInGameDebuggable debuggable;
                    if (weakReference.TryGetTarget(out debuggable))
                    {
                        debuggable.DrawInGameDebugVisual(Game.GraphicsDevice, m_camera, m_spriteBatch, m_spriteFont);
                    }
                }
                m_spriteBatch.End();
            }
        }

        public void RegisterInGameDebuggable(IInGameDebuggable debuggable)
        {
            m_debugQueue.Enqueue(debuggable);
        }

        public void ToggleInGameDebugger()
        {
            m_active = !m_active;
        }
    }
}