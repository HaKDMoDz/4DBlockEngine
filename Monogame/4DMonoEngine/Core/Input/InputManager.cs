using System;
using _4DMonoEngine.Core.Chunks;
using _4DMonoEngine.Core.Common.Logging;
using _4DMonoEngine.Core.Debugging.Console;
using _4DMonoEngine.Core.Debugging.Ingame;
using _4DMonoEngine.Core.Graphics;
using _4DMonoEngine.Core.Universe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace _4DMonoEngine.Core.Input
{
    public class InputManager : GameComponent
    {
        // properties.
        public bool CaptureMouse { get; set; } // Should the game capture mouse?
        public bool CursorCentered { get; set; } // Should the mouse cursor centered on screen?

        // previous input states.
        private MouseState m_previousMouseState;
        private KeyboardState m_previousKeyboardState;

        public InputManager(Game game)
            : base(game)
        {
            CaptureMouse = true; // capture the mouse by default.
            CursorCentered = true; // center the mouse by default.        
        }

        public override void Initialize()
        {
            // get current mouse & keyboard states.
            m_previousKeyboardState = Keyboard.GetState();
            m_previousMouseState = Mouse.GetState();

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            ProcessMouse();
            ProcessKeyboard(gameTime);
        }

        private void ProcessMouse()
        {
            var currentState = Mouse.GetState();

            if (currentState == m_previousMouseState || !CaptureMouse) // if there's no mouse-state change or if it's not captured, just return.
                return;

            float rotation = currentState.X - Core.Core.Instance.Configuration.Graphics.Width / 2;
            if (rotation != 0) _cameraController.RotateCamera(rotation);

            float elevation = currentState.Y - Core.Core.Instance.Configuration.Graphics.Height / 2;
            if (elevation != 0) _cameraController.ElevateCamera(elevation);

            if (currentState.LeftButton == ButtonState.Pressed && m_previousMouseState.LeftButton == ButtonState.Released)
                _player.Weapon.Use();
            if (currentState.RightButton == ButtonState.Pressed && m_previousMouseState.RightButton == ButtonState.Released) 
                _player.Weapon.SecondaryUse();

            m_previousMouseState = currentState;
            CenterCursor();
        }

        private void ProcessKeyboard(GameTime gameTime)
        {
            var currentState = Keyboard.GetState();

            if (currentState.IsKeyDown(Keys.Escape)) // allows quick exiting of the game.
                Game.Exit();

            if (!Core.Core.Instance.Console.Opened)
            {
                if (m_previousKeyboardState.IsKeyUp(Keys.OemTilde) && currentState.IsKeyDown(Keys.OemTilde)) // tilda
                    KeyDown(null, new KeyEventArgs(Keys.OemTilde));

                // movement keys.
                if (currentState.IsKeyDown(Keys.Up) || currentState.IsKeyDown(Keys.W))
                    _player.Move(gameTime, MoveDirection.Forward);
                if (currentState.IsKeyDown(Keys.Down) || currentState.IsKeyDown(Keys.S))
                    _player.Move(gameTime, MoveDirection.Backward);
                if (currentState.IsKeyDown(Keys.Left) || currentState.IsKeyDown(Keys.A))
                    _player.Move(gameTime, MoveDirection.Left);
                if (currentState.IsKeyDown(Keys.Right) || currentState.IsKeyDown(Keys.D))
                    _player.Move(gameTime, MoveDirection.Right);
                if (m_previousKeyboardState.IsKeyUp(Keys.Space) && currentState.IsKeyDown(Keys.Space)) _player.Jump();

                // debug keys.

                if (m_previousKeyboardState.IsKeyUp(Keys.F1) && currentState.IsKeyDown(Keys.F1)) // toggles infinitive world on or off.
                    Core.Core.Instance.Configuration.World.ToggleInfinitiveWorld();

                if (m_previousKeyboardState.IsKeyUp(Keys.F2) && currentState.IsKeyDown(Keys.F2)) // toggles flying on or off.
                    _player.ToggleFlyForm();

                if (m_previousKeyboardState.IsKeyUp(Keys.F5) && currentState.IsKeyDown(Keys.F5))
                {
                    CaptureMouse = !CaptureMouse;
                    Game.IsMouseVisible = !CaptureMouse;
                }

                if (m_previousKeyboardState.IsKeyUp(Keys.F10) && currentState.IsKeyDown(Keys.F10))
                    _ingameDebuggerService.ToggleInGameDebugger();
            }
            else
            {
                // console chars.
                foreach (var @key in Enum.GetValues(typeof(Keys)))
                {
                    if (m_previousKeyboardState.IsKeyUp((Keys)@key) && currentState.IsKeyDown((Keys)@key))
                        KeyDown(null, new KeyEventArgs((Keys)@key));
                }
            }

            m_previousKeyboardState = currentState;
        }      

        private void CenterCursor()
        {
            Mouse.SetPosition(Game.Window.ClientBounds.Width/2, Game.Window.ClientBounds.Height/2);
        }

        public delegate void KeyEventHandler(object sender, KeyEventArgs e);
        public event KeyEventHandler KeyDown;
    }
}