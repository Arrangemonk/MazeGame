using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MazeGame.Common;
using Raylib_cs;

namespace MazeGame.Loops
{
    internal class StartupLoop
    {
        private Texture2D banner;
        public StartupLoop()
        {
            banner = Raylib.LoadTexture("resources/aripro_presents_Maze.png");
        }

        public void Draw()
        {
            var startposx = Raylib.GetScreenWidth() / 2 - banner.width / 2;
            var startposy = Raylib.GetScreenHeight() / 2 - banner.height / 2;
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BLACK);
            Raylib.DrawTexture(banner, startposx, startposy, Color.WHITE);
            Raylib.EndDrawing();
        }

        internal void Dispose()
        {
           Raylib.UnloadTexture(banner);
        }
    }
}
