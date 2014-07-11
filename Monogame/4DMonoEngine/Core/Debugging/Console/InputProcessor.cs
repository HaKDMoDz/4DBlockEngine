using System;
using System.Collections.Generic;
using System.Linq;
using _4DMonoEngine.Core.Common.Helpers;
using _4DMonoEngine.Core.Events;
using _4DMonoEngine.Core.Events.Args;
using Microsoft.Xna.Framework.Input;

namespace _4DMonoEngine.Core.Debugging.Console
{
    class InputProcessor : IEventSink
    {
        public event EventHandler Open = delegate { };
        public event EventHandler Close = delegate { };

        public CommandHistory CommandHistory { get; set; }
        public OutputLine Buffer { get; set; }
        public List<OutputLine> Out { get; set; }

        private bool m_isActive;
        private readonly CommandProcesser m_commandProcesser;
        private readonly GameConsoleOptions m_options;

        public InputProcessor(CommandProcesser commandProcesser, GameConsoleOptions options)
        {
            m_commandProcesser = commandProcesser;
            m_options = options;
            m_isActive = false;
            CommandHistory = new CommandHistory();
            Out = new List<OutputLine>();
            Buffer = new OutputLine("", OutputLineType.Command);
            MainEngine.GetEngineInstance().CentralDispatch.Register(EventConstants.KeyDown, GetHandlerForEvent(EventConstants.KeyDown));
        }

        public void AddToBuffer(string text)
        {
            var lines = text.Split('\n').Where(line => line != "").ToArray();
            int i;
            for (i = 0; i < lines.Length - 1; i++)
            {
                var line = lines[i];
                Buffer.Output += line;
                ExecuteBuffer();
            }
            Buffer.Output += lines[i];
        }

        public void AddToOutput(string text)
        {
            if (m_options.OpenOnWrite)
            {
                m_isActive = true;
                Open(this, EventArgs.Empty);
            }
            foreach (var line in text.Split('\n'))
            {
                Out.Add(new OutputLine(line, OutputLineType.Output));
            }
        }

        void ToggleConsole()
        {
            m_isActive = !m_isActive;
            if (m_isActive)
            {
                Open(this, EventArgs.Empty);
            }
            else
            {
                Close(this, EventArgs.Empty);
            }
        }

        void OnKeyDown(KeyArgs e)
        {
            if (e.KeyCode == m_options.ToggleKey)
            {
                ToggleConsole();
            }

            switch (e.KeyCode)
            {
                case Keys.Enter:
                    ExecuteBuffer();
                    break;
                case Keys.Back:
                    if (Buffer.Output.Length > 0)
                        Buffer.Output = Buffer.Output.Substring(0, Buffer.Output.Length - 1);
                    break;
                case Keys.Tab:
                    AutoComplete();
                    break;
                case Keys.Up: 
                    Buffer.Output = CommandHistory.Previous(); 
                    break;
                case Keys.Down: Buffer.Output = CommandHistory.Next(); 
                    break;
                default:
                    var @char = TranslateChar(e.KeyCode);
                    if (IsPrintable(@char))
                    {
                        Buffer.Output += @char;
                    }
                    break;
            }
        }

        private static char TranslateChar(Keys xnaKey)
        {
            if (xnaKey >= Keys.A && xnaKey <= Keys.Z)
                if (Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift))
                {
                    return (char)('A' + ((int)xnaKey - (int)Keys.A));
                }
                else
                {
                    return (char)('a' + ((int)xnaKey - (int)Keys.A));
                }

            if (xnaKey >= Keys.NumPad0 && xnaKey <= Keys.NumPad9)
                return (char)('0' + ((int)xnaKey - (int)Keys.NumPad0));

            if (xnaKey >= Keys.D0 && xnaKey <= Keys.D9)
                return (char)('0' + ((int)xnaKey - (int)Keys.D0));

            if (xnaKey == Keys.OemPeriod)
                return '.';

            return ' ';
        }

        void ExecuteBuffer()
        {
            if (Buffer.Output.Length == 0)
            {
                return;
            }
            var output = m_commandProcesser.Process(Buffer.Output).Split('\n').Where(l => l != "");
            Out.Add(new OutputLine(Buffer.Output, OutputLineType.Command));
            foreach (var line in output)
            {
                Out.Add(new OutputLine(line, OutputLineType.Output));
            }
            CommandHistory.Add(Buffer.Output);
            Buffer.Output = "";
        }

        void AutoComplete()
        {
            var lastSpacePosition = Buffer.Output.LastIndexOf(' ');
            var textToMatch = lastSpacePosition < 0 ? Buffer.Output : Buffer.Output.Substring(lastSpacePosition + 1, Buffer.Output.Length - lastSpacePosition - 1);
            var match = CommandManager.GetMatchingCommand(textToMatch);
            if (match == null)
            {
                return;
            }
            var restOfTheCommand = match.Attributes.Name.Substring(textToMatch.Length);
            Buffer.Output += restOfTheCommand + " ";
        }

        private bool IsPrintable(char letter)
        {
            return m_options.Font.Characters.Contains(letter);
        }

        public bool CanHandleEvent(string eventName)
        {
            switch (eventName)
            {
                case EventConstants.KeyDown:
                    return true;
                default:
                    return false;
            }
        }

        public Action<EventArgs> GetHandlerForEvent(string eventName)
        {
            switch (eventName)
            {
                case EventConstants.KeyDown:
                    return EventHelper.Wrap<KeyArgs>(OnKeyDown);
                default:
                    return null;
            }
        }
    }
}
