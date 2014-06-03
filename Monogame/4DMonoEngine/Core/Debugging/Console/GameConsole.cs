

/* Code based on: http://code.google.com/p/xnagameconsole/ */

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace _4DMonoEngine.Core.Debugging.Console
{
    public class GameConsole
    {
        public GameConsoleOptions Options { get { return GameConsoleOptions.Options; } }
        public bool Enabled { get; set; }

        /// <summary>
        /// Indicates whether the console is currently opened
        /// </summary>
        public bool Opened { get { return console.IsOpen; } }

        private readonly GameConsoleComponent console;

        public GameConsole(Game game, SpriteBatch spriteBatch) :this(game,spriteBatch,new GameConsoleOptions()){}

        public GameConsole(Game game, SpriteBatch spriteBatch, GameConsoleOptions options) 
        {
            if (options.Font == null)
            {
                options.Font = game.Content.Load<SpriteFont>("ConsoleFont");
            }
            options.RoundedCorner = game.Content.Load<Texture2D>(@"Textures/roundedCorner");
            GameConsoleOptions.Options = options;
            Enabled = true;
            console = new GameConsoleComponent(this, game, spriteBatch);
            game.Services.AddService(typeof(GameConsole), this);
            game.Components.Add(console); 
        }

        /// <summary>
        /// Write directly to the output stream of the console
        /// </summary>
        /// <param name="text"></param>
        public void WriteLine(string text)
        {
            console.WriteLine(text);
        }
    }
}
