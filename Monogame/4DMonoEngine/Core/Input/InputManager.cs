using System;
using System.Collections.Generic;
using _4DMonoEngine.Core.Events;
using _4DMonoEngine.Core.Events.Args;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Keyboard = Microsoft.Xna.Framework.Input.Keyboard;
using Mouse = Microsoft.Xna.Framework.Input.Mouse;

namespace _4DMonoEngine.Core.Input
{
    public class InputManager : GameComponent, IEventSource
    {
        private const int MouseMoveTolerance = 5; // in pixels

        public bool CursorCentered { get; set; } // Should the mouse cursor centered on screen?
        
        private MouseState m_previousMouseState;
        private KeyboardState m_previousKeyboardState;
        private readonly EventSource m_eventSourceImpl;

        public InputManager(Game game)
            : base(game)
        {
            CursorCentered = true;
            EventsFired = new[]
            {
                EventConstants.MousePositionUpdated, 
                EventConstants.LeftMouseDown, 
                EventConstants.LeftMouseUp,
                EventConstants.RightMouseDown, 
                EventConstants.RightMouseUp,
                EventConstants.KeyDown,
                EventConstants.KeyUp
            };
            m_eventSourceImpl = new EventSource(EventsFired, true);
        }

        public override void Initialize()
        {
            m_previousKeyboardState = Keyboard.GetState();
            m_previousMouseState = Mouse.GetState();
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            ProcessMouse();
            ProcessKeyboard();
        }

        private void ProcessMouse()
        {
            var currentState = Mouse.GetState();
            if (Math.Abs(currentState.X - m_previousMouseState.X) > 1 ||
                Math.Abs(currentState.Y - m_previousMouseState.Y) > 1)
            {
                if (Math.Abs(currentState.X - Game.Window.ClientBounds.Width/2) > MouseMoveTolerance ||
                    Math.Abs(currentState.Y - Game.Window.ClientBounds.Height/2) > MouseMoveTolerance)
                {
                    m_eventSourceImpl.FireEvent(EventConstants.MousePositionUpdated,
                        new Vector2Args(new Vector2(currentState.X - Game.Window.ClientBounds.Width/2.0f,
                            currentState.Y - Game.Window.ClientBounds.Height/2.0f)));
                }
                else if (Math.Abs(m_previousMouseState.X - Game.Window.ClientBounds.Width/2) > MouseMoveTolerance ||
                            Math.Abs(m_previousMouseState.Y - Game.Window.ClientBounds.Height/2) > MouseMoveTolerance)
                {
                    m_eventSourceImpl.FireEvent(EventConstants.MousePositionUpdated,
                        new Vector2Args(Vector2.Zero));
                }
            }

            if (currentState.LeftButton == ButtonState.Pressed &&
                m_previousMouseState.LeftButton == ButtonState.Released)
            {
                m_eventSourceImpl.FireEvent(EventConstants.LeftMouseDown, new MouseButtonArgs(MouseButtonArgs.MouseButtons.Left));
            }
            else if (currentState.LeftButton == ButtonState.Released &&
                m_previousMouseState.LeftButton == ButtonState.Pressed)
            {
                m_eventSourceImpl.FireEvent(EventConstants.LeftMouseUp, new MouseButtonArgs(MouseButtonArgs.MouseButtons.Left));
            }
            if (currentState.RightButton == ButtonState.Pressed &&
                m_previousMouseState.RightButton == ButtonState.Released)
            {
                m_eventSourceImpl.FireEvent(EventConstants.RightMouseDown, new MouseButtonArgs(MouseButtonArgs.MouseButtons.Right));
            }
            else if (currentState.LeftButton == ButtonState.Released &&
                m_previousMouseState.LeftButton == ButtonState.Pressed)
            {
                m_eventSourceImpl.FireEvent(EventConstants.RightMouseUp, new MouseButtonArgs(MouseButtonArgs.MouseButtons.Right));
            }
            if (CursorCentered)
            {
                CenterCursor();
            }
            m_previousMouseState = currentState;
        }

        private void ProcessKeyboard()
        {
            var currentState = Keyboard.GetState();
            if (currentState.IsKeyDown(Keys.Escape))
            {
                MainEngine.GetEngineInstance().Exit();
            }

            foreach (var @key in Enum.GetValues(typeof(Keys)))
            {
                if (m_previousKeyboardState.IsKeyUp((Keys) @key) && currentState.IsKeyDown((Keys) @key))
                {
                    m_eventSourceImpl.FireEvent(EventConstants.KeyDown, new KeyArgs((Keys) @key));
                }
                else if (m_previousKeyboardState.IsKeyDown((Keys) @key) && currentState.IsKeyUp((Keys) @key))
                {
                    m_eventSourceImpl.FireEvent(EventConstants.KeyUp, new KeyArgs((Keys)@key));
                }
            }
            m_previousKeyboardState = currentState;
        }      

        private void CenterCursor()
        {
            Mouse.SetPosition(Game.Window.ClientBounds.Width/2, Game.Window.ClientBounds.Height/2);
        }

        public IEnumerable<string> EventsFired { get; private set; }

        public bool EventsEnabled
        {
            get { return m_eventSourceImpl.EventsEnabled; } 
            set { m_eventSourceImpl.EventsEnabled = value; }
        }

        public void Register(string eventName, Action<EventArgs> handler)
        {
            m_eventSourceImpl.Register(eventName, handler);
        }

        public void Unregister(string eventName, Action<EventArgs> handler)
        {
            m_eventSourceImpl.Unregister(eventName, handler);
        }
    }
}