using System;
namespace _4DMonoEngine.Client
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            var game = new MainGame();
             game.Run();
        }
    }
}
