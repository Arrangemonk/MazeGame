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

namespace MazeGame
{
    internal class GameLoop : IDisposable
    {
        public const int Mazesize = 250;
        private const int Exitpos = Mazesize - 1;

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

        private const float Scale = 1.0f / 30;
        private static readonly Color Tint = Color.WHITE;
        private static readonly Color ClsColor = Color.BLACK;// Tools.ColorFromFloat(0.05f, 0.1f, 0.055f, 1.0f);
        private Vector3 _oldpos;
        private Vector3 _oldtarget;
        private bool _displayOverlay;
        private bool _wireframe;
        private const int Blocksize = 16;
        public const float Fps = 120;
        public const float Ticks = 60;
        public const float Tickscale = Ticks / Fps;
                private int Maxdepth = 7;


        public GameLoop()
        {
            _shader = Tools.PrepareShader();
            _transit = Tools.PrepareModel("stairs", "stairs", _shader["normal_mapping"], Matrix4x4.Identity, ref _textures, ref _models);
            _wall = Tools.PrepareModel("wall", "pipe", _shader["normal_mapping"], Matrix4x4.Identity, ref _textures, ref _models);
            _spiderweb = Tools.PrepareModel("spiderweb", "spiderweb", _shader["normal_mapping"], Matrix4x4.Identity, ref _textures, ref _models);
            _moss = Tools.PrepareModel("moss", "moss", _shader["normal_mapping"], Matrix4x4.Identity, ref _textures, ref _models);
            _mud = Tools.PrepareModel("floor", "mud", _shader["normal_mapping"], Matrix4x4.Identity, ref _textures, ref _models);
            _mazeblocks = Raylib.LoadTexture($"resources/mazeblocks_{Blocksize}.png");
            _tileset = MazeGenerator.PrepareMazePrint(Blocksize);
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
            var size = Mazesize * Blocksize;
            _mazeTexture = Raylib.LoadRenderTexture(size, size);

            Raylib.BeginTextureMode(_mazeTexture);
            for (var z = 0; z < Mazesize; z++)
                for (var x = 0; x < Mazesize; x++)
                {
                    var dpos = new Vector2(x * Blocksize, z * Blocksize);
                    var tile = _maze[x, z];

                    Raylib.DrawTextureRec(_mazeblocks, _tileset[tile], dpos, Color.WHITE);
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
            Raylib.ClearBackground(ClsColor);
            Raylib.BeginMode3D(_camera);
            var tiles = DrawLevel();
            Raylib.EndMode3D();
            DrawMazeOverlay(tiles);
            Raylib.EndDrawing();
        }

        private void ProcessInputs()
        {
            if (Raylib.IsKeyDown(KeyboardKey.KEY_F3))
            {
                const string screenshots = nameof(screenshots);
                if (!Directory.Exists(screenshots))
                    Directory.CreateDirectory(screenshots);
                Raylib.TakeScreenshot($"{screenshots}/{Guid.NewGuid()}.png");
            }
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_R))
                ResetMaze();
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_F1))
                _displayOverlay = !_displayOverlay;
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_F2))
                _wireframe = !_wireframe;
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_UP))
                Maxdepth++;
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_DOWN))
                Maxdepth--;
        }

        private void ResetMaze()
        {
            _oldpos = _camera.position = defaultOffset;
            _maze = MazeGenerator.GenerateMaze(Mazesize, Mazesize);
            _randoms = MazeGenerator.GenerateRandomIntegers(Mazesize, Mazesize);
            PrepareMazeTexture();
        }

        static readonly Vector3 maxcam = new Vector3(Mazesize, 1, Mazesize);
        private void UpdateCamera()
        {
            const float cameraMoveSpeed = 0.03f * Tickscale;
            const float cameraMouseMoveSensitivity = 0.003f * Tickscale;

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

                cam.position = Tools.Clamp(cam.position, maxcam);

                cam.target = cam.position + relativetarget;

                Raylib.CameraYaw(&cam, -mousePositionDelta.X * cameraMouseMoveSensitivity, false);
                Raylib.CameraPitch(&cam, -mousePositionDelta.Y * cameraMouseMoveSensitivity, true, false, false);

                relativetarget =Vector3.Normalize(cam.target - cam.position); 

                Raylib.SetShaderValue(_shader["normal_mapping"], _lightPosLoc, drawOffsetByQuadrant(Tools.Clamp(cam.position + relativetarget * 0.2f, maxcam), cam.position), ShaderUniformDataType.SHADER_UNIFORM_VEC3);
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
            Raylib.DrawText($"{tiles.Count} {Maxdepth}", 220, 12, 20, Color.WHITE);
            var startposx = Raylib.GetScreenWidth() / 2 - _mazeTexture.texture.width / 2;
            var startposy = Raylib.GetScreenHeight() / 2 - _mazeTexture.texture.height / 2;

            var camx = _camera.position.X;
            var camz = _camera.position.Z;


            var cameradirection = Vector3.Normalize(_camera.target - _camera.position);
            Raylib.DrawTexturePro(_mazeTexture.texture, 
                new Rectangle(MathF.Floor(camx * Blocksize + _mazeTexture.texture.width/2f),
                    MathF.Floor(-camz * Blocksize - _mazeTexture.texture.height / 2f),
                    _mazeTexture.texture.width,
                    -_mazeTexture.texture.height),
                new Rectangle(startposx, startposy, _mazeTexture.texture.width, _mazeTexture.texture.height),
                 new Vector2(0,0),
                0, new Color(255, 255, 255, 128));
            foreach (var tile in tiles)
            {
                Raylib.DrawRectangle(
                    (int)(startposx + _mazeTexture.texture.width / 2f + tile.Item1 * Blocksize - camx * Blocksize),
                    (int)(startposy + _mazeTexture.texture.height / 2f + tile.Item2 * Blocksize - camz * Blocksize),
                    Blocksize - 2, Blocksize - 2, new Color(0,0,255,64));
            }
            Raylib.DrawRectangle(
                startposx + _mazeTexture.texture.width / 2,
                startposy + _mazeTexture.texture.height / 2, 
                Blocksize - 2, Blocksize - 2, Color.RED);
            var half =Blocksize / 2;
            cameradirection = cameradirection * half;
            Raylib.DrawRectangle(
                startposx + _mazeTexture.texture.width / 2 + (int)cameradirection.X + half/2,
                startposy + _mazeTexture.texture.height / 2 + (int)cameradirection.Z + half / 2, half, half, Color.BLACK);
        }

        private (int, int) TileIndexFromCamera()
        {
            return ((int)Math.Floor(_camera.position.X), (int)Math.Floor(_camera.position.Z));
        }

        private IEnumerable<Directions> DirectionsFromCamera()
        {
            var floatDirection = Vector3.Normalize(_camera.target - _camera.position);
            foreach (var dir in MazeGenerator.Dirx(floatDirection.X))
                yield return dir;
            foreach (var dir in MazeGenerator.Diry(floatDirection.Z))
                yield return dir;

        }

        private static readonly Vector3 defaultOffset = new(0.5f, 0, 0.5f);

        private new HashSet<(int, int)> DrawLevel()
        {
            var dpos = drawOffsetByQuadrant( defaultOffset, _camera.position);
            Raylib.DrawModel(_transit, dpos, Scale, Tint);
            dpos = drawOffsetByQuadrant(new Vector3(Exitpos, 1, Exitpos) + defaultOffset, _camera.position);
            Raylib.DrawModel(_transit, dpos, Scale, Tint);
            var index = TileIndexFromCamera();
            var cd = DirectionsFromCamera().ToArray();

            var drawList = new HashSet<(int, int)>();
            Checkvisibility(index.Item1, index.Item2,Directions.Undefined, cd, 0, ref drawList);

            foreach (var tup in drawList)
            {
                DrawTile(tup.Item1, tup.Item2);
            }
            Rlgl.rlDisableDepthMask();
            Raylib.BeginBlendMode(BlendMode.BLEND_ADD_COLORS);
            foreach (var tup in drawList.Where(elem => TileCondition(elem, 10)))
            {
                 dpos = drawOffsetByQuadrant(new Vector3(tup.Item1, 0, tup.Item2) + defaultOffset, _camera.position);
                Raylib.DrawModel(_spiderweb, dpos, Scale, Tint);
            }
            Raylib.EndBlendMode();
            Rlgl.rlEnableDepthMask();
            //if (_displayOverlay)
            //    Raylib.DrawSphere(_camera.position + Vector3.Normalize(_camera.target - _camera.position) * .1f, .001f, Color.WHITE);
            return drawList;
        }

        private bool TileCondition((int, int) elem, int threshold)
        {
            return _randoms[elem.Item1, elem.Item2] < threshold && _maze[elem.Item1, elem.Item2] < Blocks.Room;
        }

        private void Checkvisibility(int x, int z, Directions old, Directions[] cd, int depth, ref HashSet<(int, int)> drawList)
        {

            drawList.Add((x, z));
            //if (_maze[x,z] < Blocks.Room)
            depth++;
            if (depth > Maxdepth)
                return;

            var directions = MazeGenerator.DirectionsFromblock(_maze[x, z]);
            foreach (var direction in directions)
            {
                var percievedDepth = depth;//cd.Contains(direction) ? depth : Maxdepth;

                var cx = Tools.Clamp(x + MazeGenerator.Dx(direction), Mazesize);
                var cz = Tools.Clamp(z + MazeGenerator.Dy(direction), Mazesize);

                if (drawList.Contains((cx, cz)))
                    continue;
                Checkvisibility(cx, cz, direction, cd, percievedDepth, ref drawList);
            }
        }

        Vector3 drawOffsetByQuadrant(Vector3 drawpos, Vector3 campos)
        {
            var drawx = drawpos.X < Mazesize / 2;
            var drawz = drawpos.Z < Mazesize / 2;

            var camx = campos.X < Mazesize / 2;
            var camz = campos.Z < Mazesize / 2;

            var resultx = drawpos.X + Mazesize * ((drawx && !camx) ? 1 : (!drawx && camx) ? -1 : 0);
            var resultz = drawpos.Z + Mazesize * ((drawz && !camz) ? 1 : (!drawz && camz) ? -1 : 0);

            return new Vector3(resultx, drawpos.Y, resultz);
        }

        private void DrawTile(int x, int z)
        {
            var dpos = drawOffsetByQuadrant(new Vector3(x, 0, z) + defaultOffset,_camera.position);
            var tile = _maze[(int)x, (int)z];
            if (_wireframe)
            {
                Raylib.DrawModelWires(_parts[tile], dpos, Scale, Tint);
                if (tile < Blocks.Room)
                {
                    Raylib.DrawModelWires(_moss, dpos, Scale, Tint);
                    Raylib.DrawModelWires(_mud, dpos, Scale, Tint);
                }
                else
                {
                    Raylib.DrawModelWires(_wall, dpos, Scale, Tint);
                }
            }
            else
            {
                Raylib.DrawModel(_parts[tile], dpos, Scale, Tint);
                if (tile < Blocks.Room)
                {
                    Raylib.DrawModel(_moss, dpos, Scale, Tint);
                    Raylib.DrawModel(_mud, dpos, Scale, Tint);
                }
                else
                {
                    Raylib.DrawModel(_wall, dpos, Scale, Tint);
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
