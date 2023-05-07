using System.Numerics;
using MazeGame.Algorithms;
using MazeGame.Common;
using Raylib_cs;

namespace MazeGame
{
    internal static class Program
    {
        private static int sizex = 1600;
        private static int sizey = 900;
        private static int mazesize = 20;

        public static void Main()
        {
            Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT);
            Raylib.InitWindow(sizex, sizey, "Maze Game");
            Raylib.SetTargetFPS(60);

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