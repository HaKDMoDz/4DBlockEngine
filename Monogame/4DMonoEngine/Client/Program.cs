#region Using Statements

using System;

#endregion

namespace _4DMonoEngine.Client
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
