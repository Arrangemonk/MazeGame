using MazeGame.Algorithms;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MazeGame.Algorithms;
using MazeGame.Common;
using Raylib_cs;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Diagnostics;
using System.Threading;

namespace MazeGame.Loops
{
    internal class GameLoop : IDisposable
    {
        private readonly Dictionary<string, Shader> _shader;
        private readonly Model _transit;
        private readonly Model _moss;
        private readonly Model _mud;
        private readonly Model _wall;
        private readonly Model _spiderweb;
        private readonly Texture2D _mazeblocks;
        private RenderTexture2D _mazeTexture;

        private readonly Dictionary<string, Dictionary<string, Texture2D>> _textures = new();
        private readonly List<Model> _models = new();

        private Blocks[,] _maze;
        private int[,] _randoms;

        private readonly Dictionary<Blocks, Rectangle> _tileset;
        private readonly Dictionary<Blocks, Model> _parts;
        private Camera3D _camera;

        private readonly int _lightPosLoc;
        private readonly int _specularPosLoc;

        private Vector3 _oldpos;
        private Vector3 _oldtarget;
        private bool _displayOverlay;
        private bool _wireframe;
        public static float Tickscale => Constants.Ticks / Raylib.GetFPS().Map(a => a == 0 ? 1 : a);
        private int _maxdepth = 7;


        public GameLoop()
        {
            _shader = Tools.PrepareShader();
            _transit = Tools.PrepareModel("stairs", "stairs", _shader["normal_mapping"], Matrix4x4.Identity, ref _textures, ref _models);
            _wall = Tools.PrepareModel("wall", "pipe", _shader["normal_mapping"], Matrix4x4.Identity, ref _textures, ref _models);
            _spiderweb = Tools.PrepareModel("spiderweb", "spiderweb", _shader["normal_mapping"], Matrix4x4.Identity, ref _textures, ref _models);
            _moss = Tools.PrepareModel("moss", "moss", _shader["normal_mapping"], Matrix4x4.Identity, ref _textures, ref _models);
            _mud = Tools.PrepareModel("floor", "mud", _shader["normal_mapping"], Matrix4x4.Identity, ref _textures, ref _models);
            _mazeblocks = Raylib.LoadTexture($"resources/mazeblocks_{Constants.Blocksize}.png");
            _tileset = MazeGenerator.PrepareMazePrint(Constants.Blocksize);
            _parts = MazeGenerator.PrepareMazeParts(_shader["normal_mapping"], ref _textures, ref _models);
            _camera = Tools.CameraSetup();
            ResetMaze();
            // Diffuse light
            _lightPosLoc = Raylib.GetShaderLocation(_shader["normal_mapping"], "lightPos");
            //specular light
            _specularPosLoc = Raylib.GetShaderLocation(_shader["normal_mapping"], "viewPos");
            Raylib.DisableCursor();

            PrepareMazeTexture();
        }

        private void PrepareMazeTexture()
        {
            var size = Constants.Mazesize * Constants.Blocksize;
            _mazeTexture = Raylib.LoadRenderTexture(size, size);

            Raylib.BeginTextureMode(_mazeTexture);
            for (var z = 0; z < Constants.Mazesize; z++)
                for (var x = 0; x < Constants.Mazesize; x++)
                {
                    var dpos = new Vector2(x * Constants.Blocksize, z * Constants.Blocksize);
                    var tile = _maze[x, z];
                    if (tile >= Blocks.Room)
                    {
                        var top = _maze[x, Tools.Clamp(z - 1, Constants.Mazesize)];
                        var left = _maze[Tools.Clamp(x - 1, Constants.Mazesize), z];

                        var ntop = ((int)top & (int)Directions.South) != 0;
                        var nleft = ((int)left & (int)Directions.West) != 0;

                        var rect = MazeGenerator.Mazerect((ntop && nleft) ? 0 : nleft ? 1 : ntop ? 2 : 4, 0, Constants.Blocksize);
                        Raylib.DrawTextureRec(_mazeblocks, rect, dpos, Color.WHITE);
                    }
                    else
                    {
                        Raylib.DrawTextureRec(_mazeblocks, _tileset[tile], dpos, Color.WHITE);
                    }
                }


            Raylib.EndTextureMode();
            Raylib.GenTextureMipmaps(ref _mazeTexture.texture);
            Raylib.SetTextureFilter(_mazeTexture.texture, TextureFilter.TEXTURE_FILTER_BILINEAR);
        }

        public void Draw()
        {
            ProcessInputs();
            UpdateCamera();
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Constants.ClsColor);
            Raylib.BeginMode3D(_camera);
            var tiles = DrawLevel();
            Raylib.EndMode3D();
            DrawMazeOverlay(tiles);
            Raylib.EndDrawing();
        }

        private void ProcessInputs()
        {
            var key = Raylib.GetKeyPressed();

            switch (key)
            {
                case (int)KeyboardKey.KEY_F3:
                    {
                        const string screenshots = nameof(screenshots);
                        if (!Directory.Exists(screenshots))
                            Directory.CreateDirectory(screenshots);
                        Raylib.TakeScreenshot($"{screenshots}/{Guid.NewGuid()}.png");
                        break;
                    }
                case (int)KeyboardKey.KEY_R:
                    ResetMaze();
                    break;
                case (int)KeyboardKey.KEY_F1:
                    _displayOverlay = !_displayOverlay;
                    break;
                case (int)KeyboardKey.KEY_F2:
                    _wireframe = !_wireframe;
                    break;
                case (int)KeyboardKey.KEY_UP:
                    _maxdepth++;
                    break;
                case (int)KeyboardKey.KEY_DOWN:
                    _maxdepth--;
                    break;
                case (int)KeyboardKey.KEY_F4:
                    Program.Togglefullscreen();
                    break;
            }
        }

        private void ResetMaze()
        {
            _oldpos = _camera.position = Constants.DefaultOffset;
            _maze = MazeGenerator.GenerateMaze(Constants.Mazesize, Constants.Mazesize);
            _randoms = MazeGenerator.GenerateRandomIntegers(Constants.Mazesize, Constants.Mazesize);
            PrepareMazeTexture();
        }

        private void UpdateCamera()
        {
            float cameraMoveSpeed = 0.03f * Tickscale;
            float cameraMouseMoveSensitivity = 0.003f * Tickscale;

            var mousePositionDelta = Raylib.GetMouseDelta();
            var cam = _camera;
            unsafe
            {

                if (Raylib.IsKeyDown(KeyboardKey.KEY_W)) Raylib.CameraMoveForward(&cam, cameraMoveSpeed, true);
                if (Raylib.IsKeyDown(KeyboardKey.KEY_A)) Raylib.CameraMoveRight(&cam, -cameraMoveSpeed, true);
                if (Raylib.IsKeyDown(KeyboardKey.KEY_S)) Raylib.CameraMoveForward(&cam, -cameraMoveSpeed, true);
                if (Raylib.IsKeyDown(KeyboardKey.KEY_D)) Raylib.CameraMoveRight(&cam, cameraMoveSpeed, true);

                (cam.position, cam.target) = Tools.Collision(_oldpos, cam.position, _oldtarget, cam.target, _maze);

                var relativetarget = Vector3.Normalize(cam.target - cam.position);

                cam.position = Tools.Clamp(cam.position, Constants.Maxcam);

                cam.target = cam.position + relativetarget;

                Raylib.CameraYaw(&cam, -mousePositionDelta.X * cameraMouseMoveSensitivity, false);
                Raylib.CameraPitch(&cam, -mousePositionDelta.Y * cameraMouseMoveSensitivity, true, false, false);

                relativetarget = Vector3.Normalize(cam.target - cam.position);

                Raylib.SetShaderValue(_shader["normal_mapping"], _lightPosLoc, Tools.DrawOffsetByQuadrant(Tools.Clamp(cam.position + relativetarget * 0.2f, Constants.Maxcam), cam.position), ShaderUniformDataType.SHADER_UNIFORM_VEC3);
                Raylib.SetShaderValue(_shader["normal_mapping"], _specularPosLoc, cam.position, ShaderUniformDataType.SHADER_UNIFORM_VEC3);

                _oldpos = cam.position;
                _oldtarget = cam.target;

            }

            _camera = cam;

        }

        private void DrawMazeOverlay(HashSet<(int, int)> tiles)
        {
            if (!_displayOverlay)
                return;
            var index = TileIndexFromCamera();
            Raylib.DrawText(Raylib.GetFPS().ToString(), 12, 12, 20, Color.WHITE);
            Raylib.DrawText($"{_camera.position.X:0.000},{_camera.position.Z:0.000}", 60, 12, 20, Color.WHITE);
            Raylib.DrawText($"{index.Item1:0.000},{index.Item2:0.000}", 60, 32, 20, Color.WHITE);
            Raylib.DrawText($"{tiles.Count} {_maxdepth}", 220, 12, 20, Color.WHITE);
            var startposx = Raylib.GetScreenWidth() / 2 - _mazeTexture.texture.width / 2;
            var startposy = Raylib.GetScreenHeight() / 2 - _mazeTexture.texture.height / 2;

            var camx = _camera.position.X;
            var camz = _camera.position.Z;


            var cameradirection = Vector3.Normalize(_camera.target - _camera.position);
            Raylib.DrawTexturePro(_mazeTexture.texture,
                new Rectangle(MathF.Floor(camx * Constants.Blocksize + _mazeTexture.texture.width / 2f),
                    MathF.Floor(-camz * Constants.Blocksize - _mazeTexture.texture.height / 2f),
                    _mazeTexture.texture.width,
                    -_mazeTexture.texture.height),
                new Rectangle(startposx, startposy, _mazeTexture.texture.width, _mazeTexture.texture.height),
                 new Vector2(0, 0),
                0, new Color(255, 255, 255, 128));
            foreach (var tile in tiles)
            {
                var dx = startposx + _mazeTexture.texture.width / 2f + Tools.DrawOffsetByQuadrantUi(tile.Item1 - camx) * Constants.Blocksize;
                var dy = startposy + _mazeTexture.texture.height / 2f + Tools.DrawOffsetByQuadrantUi(tile.Item2 - camz) * Constants.Blocksize;

                Raylib.DrawRectangle(
                    (int)dx,
                    (int)dy, Constants.Blocksize - 2, Constants.Blocksize - 2, new Color(0, 0, 255, 64));
            }
            Raylib.DrawRectangle(
                startposx + _mazeTexture.texture.width / 2,
                startposy + _mazeTexture.texture.height / 2, Constants.Blocksize - 2, Constants.Blocksize - 2, Color.RED);
            var half = Constants.Blocksize / 2;
            cameradirection = cameradirection * half;
            Raylib.DrawRectangle(
                startposx + _mazeTexture.texture.width / 2 + (int)cameradirection.X + half / 2,
                startposy + _mazeTexture.texture.height / 2 + (int)cameradirection.Z + half / 2, half, half, Color.BLACK);
        }

        private (int, int) TileIndexFromCamera()
        {
            return ((int)Math.Floor(_camera.position.X), (int)Math.Floor(_camera.position.Z));
        }

        private new HashSet<(int, int)> DrawLevel()
        {
            var dpos = Tools.DrawOffsetByQuadrant(Constants.DefaultOffset, _camera.position);
            Raylib.DrawModel(_transit, dpos, Constants.Scale, Constants.Tint);
            dpos = Tools.DrawOffsetByQuadrant(new Vector3(Constants.Exitpos, 1, Constants.Exitpos) + Constants.DefaultOffset, _camera.position);
            Raylib.DrawModel(_transit, dpos, Constants.Scale, Constants.Tint);
            var index = TileIndexFromCamera();

            var drawList = new HashSet<(int, int)>();
            Checkvisibility(index.Item1, index.Item2, ref drawList);

            foreach (var tup in drawList)
            {
                DrawTile(tup.Item1, tup.Item2);
            }
            Rlgl.rlDisableDepthMask();
            Raylib.BeginBlendMode(BlendMode.BLEND_ADD_COLORS);
            foreach (var tup in drawList.Where(elem => TileCondition(elem, 10)))
            {
                dpos = Tools.DrawOffsetByQuadrant(new Vector3(tup.Item1, 0, tup.Item2) + Constants.DefaultOffset, _camera.position);
                Raylib.DrawModel(_spiderweb, dpos, Constants.Scale, Constants.Tint);
            }
            Raylib.EndBlendMode();
            Rlgl.rlEnableDepthMask();
            if (_displayOverlay)
                Raylib.DrawSphere(_camera.position + Vector3.Normalize(_camera.target - _camera.position) * .1f, .001f, new Color(255, 255, 255, 64));
            return drawList;
        }

        private bool TileCondition((int, int) elem, int threshold)
        {
            return _randoms[elem.Item1, elem.Item2] < threshold && _maze[elem.Item1, elem.Item2] < Blocks.Room;
        }

        private void Checkvisibility(int x, int z, ref HashSet<(int, int)> drawList)
        {
            for (var tx = -1; tx <= 1; tx++)
            {
                for (var tz = -1; tz <= 1; tz++)
                {
                    var cx = Tools.Clamp(x + tx, Constants.Mazesize);
                    var cz = Tools.Clamp(z + tz, Constants.Mazesize);
                    drawList.Add((cx, cz));
                }
            }

            var floatDirection = Vector3.Normalize(_camera.target - _camera.position);
            Tools.Drawtrangle(floatDirection, x, z, _maxdepth, ref drawList);
        }



        private void DrawTile(int x, int z)
        {
            var dpos = Tools.DrawOffsetByQuadrant(new Vector3(x, 0, z) + Constants.DefaultOffset, _camera.position);
            var tile = _maze[x, z];
            if (_wireframe)
            {
                Raylib.DrawModelWires(_parts[tile], dpos, Constants.Scale, Constants.Tint);
                if (tile < Blocks.Room)
                {
                    Raylib.DrawModelWires(_moss, dpos, Constants.Scale, Constants.Tint);
                    Raylib.DrawModelWires(_mud, dpos, Constants.Scale, Constants.Tint);
                }
                else
                {
                    Raylib.DrawModelWires(_wall, dpos, Constants.Scale, Constants.Tint);
                }
            }
            else
            {
                Raylib.DrawModel(_parts[tile], dpos, Constants.Scale, Constants.Tint);
                if (tile < Blocks.Room)
                {
                    Raylib.DrawModel(_moss, dpos, Constants.Scale, Constants.Tint);
                    Raylib.DrawModel(_mud, dpos, Constants.Scale, Constants.Tint);
                }
                else
                {
                    Raylib.DrawModel(_wall, dpos, Constants.Scale, Constants.Tint);
                }
            }
        }

        public void Dispose()
        {

            foreach (var model in _models)
                Raylib.UnloadModel(model);

            foreach (var texture in _textures.SelectMany(t => t.Value.Values))
                Raylib.UnloadTexture(texture);

            Raylib.UnloadTexture(_mazeblocks);
            Raylib.UnloadRenderTexture(_mazeTexture);
        }
    }
}
