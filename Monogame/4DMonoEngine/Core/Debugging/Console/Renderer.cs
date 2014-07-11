

/* Code based on: http://code.google.com/p/xnagameconsole/ */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Debugging.Console
{
    class Renderer
    {
        enum State
        {
            Opened,
            Opening,
            Closed,
            Closing
        }

        public bool IsOpen
        {
            get
            {
                return m_currentState == State.Opened;
            }
        }

        private readonly SpriteBatch m_spriteBatch;
        private readonly InputProcessor m_inputProcessor;
        private readonly Texture2D m_pixel;
        private readonly int m_width;
        private readonly GameConsoleOptions m_options;
        private State m_currentState;
        private Vector2 m_openedPosition, m_closedPosition, m_position;
        private DateTime m_stateChangeTime;
        private Vector2 m_firstCommandPositionOffset;
        private Vector2 FirstCommandPosition
        {
            get
            {
                return new Vector2(InnerBounds.X, InnerBounds.Y) + m_firstCommandPositionOffset;
            }
        }

        Rectangle Bounds
        {
            get
            {
                return new Rectangle((int)m_position.X, (int)m_position.Y, m_width - (m_options.Margin * 2), m_options.Height);
            }
        }

        Rectangle InnerBounds
        {
            get
            {
                return new Rectangle(Bounds.X + m_options.Padding, Bounds.Y + m_options.Padding, Bounds.Width - m_options.Padding, Bounds.Height);
            }
        }

        private readonly float m_oneCharacterWidth;
        private readonly int m_maxCharactersPerLine;

        public Renderer(Game game, SpriteBatch spriteBatch, InputProcessor inputProcessor, GameConsoleOptions options)
        {
            m_currentState = State.Closed;
            m_width = game.GraphicsDevice.Viewport.Width;
            m_position = m_closedPosition = new Vector2(m_options.Margin, -m_options.Height - m_options.RoundedCorner.Height);
            m_openedPosition = new Vector2(m_options.Margin, 0);
            m_spriteBatch = spriteBatch;
            m_inputProcessor = inputProcessor;
            m_options = options;
            m_pixel = new Texture2D(game.GraphicsDevice, 1, 1,false, SurfaceFormat.Color);
            m_pixel.SetData(new[] { Color.White });
            m_firstCommandPositionOffset = Vector2.Zero;
            m_oneCharacterWidth = m_options.Font.MeasureString("x").X;
            m_maxCharactersPerLine = (int)((Bounds.Width - m_options.Padding * 2) / m_oneCharacterWidth);
        }

        public void Update(GameTime gameTime)
        {
            if (m_currentState == State.Opening)
            {
                m_position.Y = MathHelper.SmoothStep(m_position.Y, m_openedPosition.Y, ((float)((DateTime.Now - m_stateChangeTime).TotalSeconds / m_options.AnimationSpeed)));
                if (m_position.Y >= m_openedPosition.Y)
                {
                    m_currentState = State.Opened;
                }
            }
            if (m_currentState == State.Closing)
            {
                m_position.Y = MathHelper.SmoothStep(m_position.Y, m_closedPosition.Y, ((float)((DateTime.Now - m_stateChangeTime).TotalSeconds / m_options.AnimationSpeed)));
                if (m_position.Y <= m_closedPosition.Y)
                {
                    m_currentState = State.Closed;
                }
            }
        }

        public void Draw(GameTime gameTime)
        {
            if (m_currentState == State.Closed) //Do not draw if the console is closed
            {
                return;
            }
            m_spriteBatch.Draw(m_pixel, Bounds, m_options.BackgroundColor);
            DrawRoundedEdges();
            var nextCommandPosition = DrawCommands(m_inputProcessor.Out, FirstCommandPosition);
            nextCommandPosition = DrawPrompt(nextCommandPosition);
            var bufferPosition = DrawCommand(m_inputProcessor.Buffer.ToString(), nextCommandPosition, m_options.BufferColor); //Draw the buffer
            DrawCursor(bufferPosition, gameTime);
        }

        void DrawRoundedEdges()
        {
            //Bottom-left edge
            m_spriteBatch.Draw(m_options.RoundedCorner, new Vector2(m_position.X, m_position.Y + m_options.Height), null, m_options.BackgroundColor, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            //Bottom-right edge 
            m_spriteBatch.Draw(m_options.RoundedCorner, new Vector2(m_position.X + Bounds.Width - m_options.RoundedCorner.Width, m_position.Y + m_options.Height), null, m_options.BackgroundColor, 0, Vector2.Zero, 1, SpriteEffects.FlipHorizontally, 1);
            //connecting bottom-rectangle
            m_spriteBatch.Draw(m_pixel, new Rectangle(Bounds.X + m_options.RoundedCorner.Width, Bounds.Y + m_options.Height, Bounds.Width - m_options.RoundedCorner.Width * 2, m_options.RoundedCorner.Height), m_options.BackgroundColor);
        }

        void DrawCursor(Vector2 pos, GameTime gameTime)
        {
            if (!IsInBounds(pos.Y))
            {
                return;
            }
            var split = SplitCommand(m_inputProcessor.Buffer.ToString(), m_maxCharactersPerLine).Last();
            pos.X += m_options.Font.MeasureString(split).X;
            pos.Y -= m_options.Font.LineSpacing;
            m_spriteBatch.DrawString(m_options.Font, (int)(gameTime.TotalGameTime.TotalSeconds / m_options.CursorBlinkSpeed) % 2 == 0 ? m_options.Cursor.ToString() : "", pos, m_options.CursorColor);
        }

        /// <summary>
        /// Draws the specified command and returns the position of the next command to be drawn
        /// </summary>
        /// <param name="command"></param>
        /// <param name="pos"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        Vector2 DrawCommand(string command, Vector2 pos, Color color)
        {
            var splitLines = command.Length > m_maxCharactersPerLine ? SplitCommand(command, m_maxCharactersPerLine) : new[] { command };
            foreach (var line in splitLines)
            {
                if (IsInBounds(pos.Y))
                {
                    m_spriteBatch.DrawString(m_options.Font, line, pos, color);
                }
                ValidateFirstCommandPosition(pos.Y + m_options.Font.LineSpacing);
                pos.Y += m_options.Font.LineSpacing;
            }
            return pos;
        }

        static IEnumerable<string> SplitCommand(string command, int max)
        {
            var lines = new List<string>();
            while (command.Length > max)
            {
                var splitCommand = command.Substring(0, max);
                lines.Add(splitCommand);
                command = command.Substring(max, command.Length - max);
            }
            lines.Add(command);
            return lines;
        }

        /// <summary>
        /// Draws the specified collection of commands and returns the position of the next command to be drawn
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        Vector2 DrawCommands(IEnumerable<OutputLine> lines, Vector2 pos)
        {
            var originalX = pos.X;
            foreach (var command in lines)
            {
                if (command.Type == OutputLineType.Command)
                {
                    pos = DrawPrompt(pos);
                }
                //position.Y = DrawCommand(command.ToString(), position, m_options.FontColor).Y;
                pos.Y = DrawCommand(command.ToString(), pos, command.Type == OutputLineType.Command ? m_options.PastCommandColor : m_options.PastCommandOutputColor).Y;
                pos.X = originalX;
            }
            return pos;
        }

        /// <summary>
        /// Draws the prompt at the specified position and returns the position of the text that will be drawn next to it
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        Vector2 DrawPrompt(Vector2 pos)
        {
            m_spriteBatch.DrawString(m_options.Font, m_options.Prompt, pos, m_options.PromptColor);
            pos.X += m_oneCharacterWidth * m_options.Prompt.Length + m_oneCharacterWidth;
            return pos;
        }

        public void Open()
        {
            if (m_currentState == State.Opening || m_currentState == State.Opened)
            {
                return;
            }
            m_stateChangeTime = DateTime.Now;
            m_currentState = State.Opening;
        }

        public void Close()
        {
            if (m_currentState == State.Closing || m_currentState == State.Closed)
            {
                return;
            }
            m_stateChangeTime = DateTime.Now;
            m_currentState = State.Closing;
        }

        void ValidateFirstCommandPosition(float nextCommandY)
        {
            if (!IsInBounds(nextCommandY))
            {
                m_firstCommandPositionOffset.Y -= m_options.Font.LineSpacing;
            }
        }

        bool IsInBounds(float yPosition)
        {
            return yPosition < m_openedPosition.Y + m_options.Height;
        }
    }
}