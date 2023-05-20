using System.Numerics;
using MazeGame.Algorithms;
using MazeGame.Common;
using MazeGame.Loops;
using Raylib_cs;

namespace MazeGame
{
    public static class Program
    {
        private static int sizex = 1920;
        private static int sizey = 1080;
        public static GameState State = GameState.Starting;
        public static void Main()
        {
            Raylib.InitWindow(sizex, sizey, "Maze Game");
            var monitor = Raylib.GetCurrentMonitor();
            Raylib.SetTargetFPS(Raylib.GetMonitorRefreshRate(monitor) *2);

            //var startupLoop = new StartupLoop();
            //var menuLoop = new MenuLoop();
            var gameLoop = new GameLoop();

            Raylib.DisableCursor();
            while (!Raylib.WindowShouldClose())
            {
            //    switch (State)
            //    {
            //        case GameState.Starting:
            //            startupLoop.Draw();
            //            break;
            //        case GameState.Menu:
            //            menuLoop.Draw();
            //            break;
            //        case GameState.Game:
                        gameLoop.Draw();
                //        break;
                //}
            }
            gameLoop.Dispose();
            Raylib.CloseWindow();
        }

        public static void Togglefullscreen()
        {
            if (Raylib.IsWindowFullscreen())
            {
                Raylib.SetWindowSize(sizex, sizey);
                Raylib.ToggleFullscreen();
            }
            else
            {
                var monitor = Raylib.GetCurrentMonitor();
                var x = Raylib.GetMonitorWidth(monitor);
                var y = Raylib.GetMonitorHeight(monitor);
                Raylib.SetWindowSize(x, y);
                Raylib.ToggleFullscreen();
            }

        }
    }
}