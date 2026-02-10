using System.Numerics;
using MazeGame.Algorithms;
using MazeGame.Common;
using MazeGame.Loops;
using Raylib_CSharp;
using Raylib_CSharp.Audio;
using Raylib_CSharp.Interact;
using Raylib_CSharp.Windowing;

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
            Raylib.SetConfigFlags(ConfigFlags.Msaa4XHint);
            Window.Init(sizex, sizey, "Maze Game");
            AudioDevice.Init();
            var monitor = Window.GetCurrentMonitor();
            Time.SetTargetFPS(Window.GetMonitorRefreshRate(monitor));
            var startupLoop = new StartupLoop();
            _gameLoop = new GameLoop();
            var task = _gameLoop.StartInit();
            _menuLoop = new MenuLoop();

            Input.DisableCursor();
            var startTime = DateTime.Now;
            while (!Window.ShouldClose())
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
            AudioDevice.Close();
            Window.Close();
        }

        //private static async Task Create()
        //{
        //    gameLoop = new GameLoop();
        //    menuLoop = new MenuLoop();
        //}

        public static void Togglefullscreen()
        {
            if (Window.IsFullscreen())
            {
                Window.SetSize(sizex, sizey);
                Window.ToggleFullscreen();
            }
            else
            {
                var monitor = Window.GetCurrentMonitor();
                var x = Window.GetMonitorWidth(monitor);
                var y = Window.GetMonitorHeight(monitor);
                Window.SetSize(x, y);
                Window.ToggleFullscreen();
            }

        }
    }
}