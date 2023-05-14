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

namespace MazeGame
{
    internal class GameLoop : IDisposable
    {
        private const int Mazesize = 112;
        private const int Exitpos = 1 - Mazesize;

        private readonly Dictionary<string,Shader> _shader;
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
        private const int Blocksize = 8;
        private const int Maxdepth = 7;
        public const float Fps = 120;
        public const float Ticks = 60;
        public const float Tickscale = Ticks / Fps;


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
            _parts = MazeGenerator.PrepareMazeParts(_shader["normal_mapping"], ref _textures,ref _models);
            _camera = Tools.CameraSetup();
            ResetMaze();
            // Diffuse light
            _lightPosLoc = Raylib.GetShaderLocation(_shader["normal_mapping"], "lightPos");
            //specular light
            _specularPosLoc = Raylib.GetShaderLocation(_shader["normal_mapping"], "viewPos");

            _oldpos = _camera.position;
            _oldtarget = _camera.target;
            Raylib.DisableCursor();

            PrepareMazeTexture();
        }

        private void PrepareMazeTexture()
        {
            var size = (Mazesize + 1) * Blocksize;
            _mazeTexture = Raylib.LoadRenderTexture(size, size);

            Raylib.BeginTextureMode(_mazeTexture);
            for (var z = 0; z < Mazesize + 1; z++)
            for (var x = 0; x < Mazesize + 1; x++)
            {
                var dpos = new Vector2(x * Blocksize, z * Blocksize);
                var xinbounds = x < Mazesize;
                var zinbounds = z < Mazesize;
                var tile = xinbounds && zinbounds ? _maze[x, z] :
                    xinbounds ? Blocks.Horizontal :
                    zinbounds ? Blocks.Vertical : Blocks.Cross;

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
        }

        private void ResetMaze()
        {
            _oldpos = _camera.position = Vector3.Zero;
            _maze = MazeGenerator.GenerateMaze(Mazesize, Mazesize);
            _randoms = MazeGenerator.GenerateRandomIntegers(Mazesize, Mazesize);
            PrepareMazeTexture();
        }

        private void UpdateCamera()
        {

            float cameraMoveSpeed = 0.03f * Tickscale;
            float cameraMouseMoveSensitivity = 0.003f * Tickscale;

            Vector2 mousePositionDelta = Raylib.GetMouseDelta();
            Camera3D cam = _camera;
            unsafe
            {
                Raylib.CameraYaw(&cam, -mousePositionDelta.X * cameraMouseMoveSensitivity, false);
                Raylib.CameraPitch(&cam, -mousePositionDelta.Y * cameraMouseMoveSensitivity, false, false, false);

                if (Raylib.IsKeyDown(KeyboardKey.KEY_W)) Raylib.CameraMoveForward(&cam, cameraMoveSpeed, true);
                if (Raylib.IsKeyDown(KeyboardKey.KEY_A)) Raylib.CameraMoveRight(&cam, -cameraMoveSpeed, true);
                if (Raylib.IsKeyDown(KeyboardKey.KEY_S)) Raylib.CameraMoveForward(&cam, -cameraMoveSpeed, true);
                if (Raylib.IsKeyDown(KeyboardKey.KEY_D)) Raylib.CameraMoveRight(&cam, cameraMoveSpeed, true);
            }

            _camera = cam;
            (_camera.position, _camera.target) = Tools.Collision(_oldpos, _camera.position, _oldtarget, _camera.target, _maze);
            _oldpos = _camera.position;
            _oldtarget = _camera.target;

            Raylib.SetShaderValue(_shader["normal_mapping"], _lightPosLoc, _camera.position + Vector3.Normalize(_camera.target - _camera.position) * .1f, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
            Raylib.SetShaderValue(_shader["normal_mapping"], _specularPosLoc, _camera.position, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
        }

        private void DrawMazeOverlay(int tiles)
        {
            if (!_displayOverlay)
                return;
            var index = TileIndexFromCamera();
            Raylib.DrawText(Raylib.GetFPS().ToString(), 12, 12, 20, Color.WHITE);
            Raylib.DrawText($"{index.Item1},{index.Item2}", 50, 12, 20, Color.WHITE);
            Raylib.DrawText($"{tiles}", 112, 12, 20, Color.WHITE);
            var startpos = Raylib.GetScreenWidth() / 2 - _mazeTexture.texture.width / 2;

            //Raylib.DrawTextureEx(mazeTexture.texture,new Vector2(startpos, 0),0f,10f,new Color(255,255,255,128));
            Raylib.DrawTextureRec(_mazeTexture.texture,new Rectangle(0,0,_mazeTexture.texture.width, -_mazeTexture.texture.height), new Vector2(startpos, 0), new Color(255, 255, 255, 128));
            Raylib.DrawRectangle(startpos + index.Item1 * Blocksize, index.Item2 * Blocksize, Blocksize, Blocksize,Color.RED);
        }

        private (int, int) TileIndexFromCamera()
        {
            return (-(int)Math.Floor(_camera.position.X + 0.5f), -(int)Math.Floor(_camera.position.Z + 0.5f));
        }

        private IEnumerable<Directions> DirectionsFromCamera()
        {
            var floatDirection = Vector3.Normalize(_camera.target - _camera.position);
            foreach (var dir in MazeGenerator.Dirx(floatDirection.X))
                yield return dir;
            foreach (var dir in MazeGenerator.Diry(floatDirection.Z))
                yield return dir;

        }

        private int DrawLevel()
        {
            Raylib.DrawModel(_transit, new Vector3(0, 0, 0), Scale, Tint);
            Raylib.DrawModel(_transit, new Vector3(Exitpos, 1, Exitpos), Scale, Tint);
            var index = TileIndexFromCamera();
            var cd = DirectionsFromCamera().ToArray();

            var drawList = new HashSet<Tuple<int, int>>();
            Checkvisibility(index.Item1, index.Item2, Directions.Undefined, cd, 0,ref drawList);

            foreach (var tup in drawList)
            {
                DrawTile(tup.Item1, tup.Item2);
            }
            Rlgl.rlDisableDepthMask();
            Raylib.BeginBlendMode(BlendMode.BLEND_ADD_COLORS);
            foreach (var tup in drawList.Where(elem => _randoms[elem.Item1,elem.Item2] < 10 && _maze[elem.Item1, elem.Item2] < Blocks.Room))
            {
                Raylib.DrawModel(_spiderweb, new Vector3(-tup.Item1, 0, -tup.Item2), Scale, Tint);
            }
            Raylib.EndBlendMode();
            Rlgl.rlEnableDepthMask();

            if (_displayOverlay)
                Raylib.DrawSphere(_camera.position + Vector3.Normalize(_camera.target - _camera.position) * .1f, .001f, Color.WHITE);
            return drawList.Count;
        }

        private void Checkvisibility(int x, int z, Directions old, Directions[] cd, int depth,ref HashSet<Tuple<int,int>> drawList)
        {

            drawList.Add(Tuple.Create(x,z));
            //if (_maze[x,z] < Blocks.Room)
                depth++;
                if (depth > Maxdepth)
                    return;

            var directions = MazeGenerator.DirectionsFromblock(_maze[x, z])
                .Except(new[] { MazeGenerator.Opposite(old) });
            foreach (var direction in directions)
            {
                var percievedDepth = cd.Contains(direction) ? depth : Maxdepth;

                var cx = x + MazeGenerator.Dx(direction);
                var cy = z + MazeGenerator.Dy(direction);

                if (0 > cx || cx >= _maze.GetLength(0)
                 || 0 > cy || cy >= _maze.GetLength(1))
                    continue;
                Checkvisibility(cx, cy, direction, cd, percievedDepth, ref drawList);
            }
        }

        private void DrawTile(int x, int z)
        {
            var dpos = new Vector3(-x, 0, -z);
            var tile = _maze[x, z];
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
