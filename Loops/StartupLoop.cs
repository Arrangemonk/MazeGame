using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MazeGame.Common;
using Raylib_CSharp;
using Raylib_CSharp.Colors;
using Raylib_CSharp.Fonts;
using Raylib_CSharp.Interact;
using Raylib_CSharp.Rendering;
using Raylib_CSharp.Textures;
using Raylib_CSharp.Windowing;

namespace MazeGame.Loops
{
    public class StartupLoop
    {
        private Texture2D banner;
        private DateTime? startTime;
        private bool start;
        public StartupLoop()
        {
            banner = Texture2D.Load("resources/aripro_presents_Maze.png");

        }

        public void Draw()
        {
            if (Input.IsKeyPressed(KeyboardKey.F4))
            {
                Program.Togglefullscreen();
            }

            var now = DateTime.Now;
            startTime ??= now;
            var floatblend = MathF.Min(1,
                MathF.Max(0, (float)((now - startTime.Value).TotalMilliseconds / 1000.0) - 1.0f));
            var blend = (byte)(255 * floatblend );

            Graphics.BeginDrawing();
            Graphics.BeginBlendMode(BlendMode.Alpha);
            Graphics.ClearBackground(Tools.ColorLerp(Color.White, Color.RayWhite, floatblend));
            //DrawAripro(blend);
            DrawRaylib(blend);
            Graphics.EndBlendMode();
            Graphics.EndDrawing();

        }

        private void DrawAripro(byte blend)
        {
            var startposx = Window.GetScreenWidth() / 2 - banner.Width / 2;
            var startposy = Window.GetScreenHeight() / 2 - banner.Height / 2;
            Graphics.DrawTexture(banner, startposx, startposy, new Color(255, 255, 255, blend));
        }


        private void DrawRaylib(byte blend)
        {
            int width = Window.GetScreenWidth() / 2;
            int height = Window.GetScreenHeight() / 2;
            float scale = Window.GetScreenWidth() / 800f;

            int _16 = (int)scale * 16;
            int _44 = (int)scale * 44;
            int _48 = (int)scale * 48;
            int _50 = (int)scale * 50;
            int _224 = (int)scale * 224;
            int _256 = (int)scale * 256;

            Graphics.DrawRectangle(width - _256 / 2, height - _256 / 2, _16, _256, new Color(0, 0, 0, blend));
            Graphics.DrawRectangle(width + _224 / 2, height - _256 / 2, _16, _256, new Color(0, 0, 0, blend));
            Graphics.DrawRectangle(width - _224 / 2, height - _256 / 2, _224, _16, new Color(0, 0, 0, blend));
            Graphics.DrawRectangle(width - _224 / 2, height + _224 / 2, _224, _16, new Color(0, 0, 0, blend));
            Graphics.DrawRectangle(width - _224 / 2, height - _224 / 2, _224, _224, new Color(245, 245, 245, blend));
            Graphics.DrawText("raylib", width - _44, height + _48, _50, new Color(0, 0, 0, blend));

            const string text = "made with raylib";
            var size = (int)(scale * 10);
            var tw = TextManager.MeasureText(text, size);

            Graphics.DrawText(text, width - tw / 2, (int)(Window.GetScreenHeight() * 0.85f), size, new Color(130, 130, 130, blend));

        }

        internal void Dispose()
        {
            banner.Unload();
        }
    }
}
