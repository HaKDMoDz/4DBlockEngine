#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace Client
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new MainGame())
                game.Run();
        }
    }
}
