using System.Numerics;
using MazeGame.Algorithms;
using MazeGame.Common;
using Raylib_cs;

namespace MazeGame
{
    internal static class Program
    {
        private static int sizex = 2560;
        private static int sizey = 1440;

        public static void Main()
        {
            Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_FULLSCREEN_MODE);
            Raylib.InitWindow(sizex, sizey, "Maze Game");
            Raylib.SetTargetFPS(60);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_VSYNC_HINT);

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