using System.Numerics;
using MazeGame.Algorithms;
using MazeGame.Common;
using Raylib_cs;

namespace MazeGame
{
    public static class Program
    {
        private static int sizex = 1600;
        private static int sizey = 900;
        public static void Main()
        {
            Raylib.InitWindow(sizex, sizey, "Maze Game");
            //Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT);
            //Raylib.SetConfigFlags(ConfigFlags.FLAG_FULLSCREEN_MODE);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_UNDECORATED);
            Raylib.SetTargetFPS((int)GameLoop.Fps);
            //Raylib.SetConfigFlags(ConfigFlags.FLAG_VSYNC_HINT);


            var loop = new GameLoop();

            Raylib.DisableCursor();
            while (!Raylib.WindowShouldClose())
            {
                loop.Draw();
            }
            loop.Dispose();
            Raylib.CloseWindow();
        }
    }
}