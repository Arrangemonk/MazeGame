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
        public static GameLoop _gameLoop;
        public static MenuLoop _menuLoop;
        public static void Main()
        {
            Raylib.SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT);
            Raylib.InitWindow(sizex, sizey, "Maze Game");
            Raylib.InitAudioDevice();
            var monitor = Raylib.GetCurrentMonitor();
            Raylib.SetTargetFPS(Raylib.GetMonitorRefreshRate(monitor) *2);
            var startupLoop = new StartupLoop();
            _gameLoop = new GameLoop();
            var task = _gameLoop.StartInit();
            _menuLoop = new MenuLoop();

            Raylib.DisableCursor();
            var startTime = DateTime.Now;
            while (!Raylib.WindowShouldClose())
            {
                switch (State)
                {
                    case GameState.Starting:
                        startupLoop.Draw();

                        if (DateTime.Now > startTime + TimeSpan.FromSeconds(4))//&& Program._gameLoop.Inialized)
                        {
                            _gameLoop.FinishInit(task);
                            State = GameState.Game;
                        }

                        break;
                    case GameState.Menu:
                        _menuLoop.Draw();
                        break;
                    case GameState.Game:
                        _gameLoop.Draw();
                        break;
                }
            }
            startupLoop.Dispose();
            _menuLoop.Dispose();
            _gameLoop.Dispose();
            Raylib.CloseAudioDevice();
            Raylib.CloseWindow();
        }

        //private static async Task Create()
        //{
        //    gameLoop = new GameLoop();
        //    menuLoop = new MenuLoop();
        //}

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