using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MazeGame.Common;
using Raylib_cs;

namespace MazeGame.Loops
{
    public class StartupLoop
    {
        private Texture2D banner;
        private DateTime? startTime;
        private bool start;
        public StartupLoop()
        {
            banner = Raylib.LoadTexture("resources/aripro_presents_Maze.png");

        }

        public void Draw()
        {
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_F4))
            {
                Program.Togglefullscreen();
            }

            var now = DateTime.Now;
            startTime ??= now;
            var floatblend = MathF.Min(1,
                MathF.Max(0, (float)((now - startTime.Value).TotalMilliseconds / 1000.0) - 1.0f));
            var blend = (int)(255 * floatblend * floatblend);

            Raylib.BeginDrawing();
            Raylib.BeginBlendMode(BlendMode.BLEND_ALPHA);
            Raylib.ClearBackground(Tools.ColorLerp(Color.WHITE, Color.RAYWHITE, floatblend));
            DrawAripro(blend);
            DrawRaylib(255 - blend);
            Raylib.EndBlendMode();
            Raylib.EndDrawing();

        }

        private void DrawAripro(int blend)
        {
            var startposx = Raylib.GetScreenWidth() / 2 - banner.width / 2;
            var startposy = Raylib.GetScreenHeight() / 2 - banner.height / 2;
            Raylib.DrawTexture(banner, startposx, startposy, new Color(255, 255, 255, blend));
        }


        private void DrawRaylib(int blend)
        {
            int width = Raylib.GetScreenWidth() / 2;
            int height = Raylib.GetScreenHeight() / 2;
            float scale = Raylib.GetScreenWidth() / 800f;

            int _16 = (int)scale * 16;
            int _44 = (int)scale * 44;
            int _48 = (int)scale * 48;
            int _50 = (int)scale * 50;
            int _224 = (int)scale * 224;
            int _256 = (int)scale * 256;

            Raylib.DrawRectangle(width - _256 / 2, height - _256 / 2, _16, _256, new Color(0, 0, 0, blend));
            Raylib.DrawRectangle(width + _224 / 2, height - _256 / 2, _16, _256, new Color(0, 0, 0, blend));
            Raylib.DrawRectangle(width - _224 / 2, height - _256 / 2, _224, _16, new Color(0, 0, 0, blend));
            Raylib.DrawRectangle(width - _224 / 2, height + _224 / 2, _224, _16, new Color(0, 0, 0, blend));
            Raylib.DrawRectangle(width - _224 / 2, height - _224 / 2, _224, _224, new Color(245, 245, 245, blend));
            Raylib.DrawText("raylib", width - _44, height + _48, _50, new Color(0, 0, 0, blend));

            const string text = "made with raylib";
            var size = (int)(scale * 10);
            var tw = Raylib.MeasureText(text, size);

            Raylib.DrawText(text, width - tw / 2, (int)(Raylib.GetScreenHeight() * 0.85f), size, new Color(130, 130, 130, blend));

        }

        internal void Dispose()
        {
            Raylib.UnloadTexture(banner);
        }
    }
}
