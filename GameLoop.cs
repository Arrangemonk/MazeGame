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

namespace MazeGame
{
    internal class GameLoop : IDisposable
    {
        private const int Mazesize = 20;

        private readonly Shader _shader;
        private readonly Model _start;
        private readonly Model _moss;
        private readonly Model _exit;
        private readonly Model _mud;
        private readonly Texture2D _mazeblocks;

        private readonly Dictionary<string, Dictionary<string, Texture2D>> _textures = new();

        private Blocks[,] _maze;

        private readonly Dictionary<Blocks, Rectangle> _tileset;
        private readonly Dictionary<Blocks, Model> _parts;
        private Camera3D _camera;

        private readonly int _lightPosLoc;
        private readonly int _specularPosLoc;

        private static readonly float Scale = 1.0f / 30;
        private static readonly Color Tint = Color.WHITE;
        private static readonly Color clsColor = Tools.ColorFromFloat(0.05f, 0.1f, 0.055f, 1.0f);
        private static readonly float Speed = 0.2f;
        private Vector3 _oldpos;
        private bool _displayOverlay;
        private bool _wireframe;


        public GameLoop()
        {
            _maze = MazeGenerator.GenerateMaze(Mazesize, Mazesize);
            _shader = Tools.PrepareShader();
            _start = Tools.PrepareModel("stairs", "stairs", _shader, Matrix4x4.Identity, ref _textures);
            _moss = Tools.PrepareModel("moss", "moss", _shader, Matrix4x4.Identity, ref _textures);
            _exit = Tools.PrepareModel("exit", "exit", _shader, Matrix4x4.Identity, ref _textures);
            _mud = Tools.PrepareModel("floor", "mud", _shader, Matrix4x4.Identity, ref _textures);
            _mazeblocks = Raylib.LoadTexture("resources/mazeblocks.png");
            _tileset = MazeGenerator.PrepareMazePrint();
            _parts = MazeGenerator.PrepareMazeParts(_shader, ref _textures);
            _camera = Tools.CameraSetup();

            // Diffuse light
            _lightPosLoc = Raylib.GetShaderLocation(_shader, "lightPos");
            //specular light
            _specularPosLoc = Raylib.GetShaderLocation(_shader, "viewPos");

            _oldpos = _camera.position;
            Raylib.DisableCursor();
        }

        public void Draw()
        {
            ProcessInputs();
            UpdateCamera();
            Raylib.BeginDrawing();
            Raylib.ClearBackground(clsColor);
            Raylib.BeginMode3D(_camera);
            var tiles = DrawLevel();
            Raylib.EndMode3D();
            DrawMazeOverlay(tiles);
            Raylib.EndDrawing();
        }

        private void ProcessInputs()
        {
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
        }

        private void UpdateCamera()
        {
            Raylib.UpdateCamera(ref _camera, CameraMode.CAMERA_FIRST_PERSON);
            _camera.position = Vector3.Lerp(_oldpos, _camera.position, Speed);

            _camera.position = Tools.Collision(_oldpos, _camera.position, _maze);
            _oldpos = _camera.position;
            Raylib.SetShaderValue(_shader, _lightPosLoc, _camera.position + Vector3.Normalize(_camera.target - _camera.position) * .1f, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
            Raylib.SetShaderValue(_shader, _specularPosLoc, _camera.position, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
        }

        private void DrawMazeOverlay(int tiles)
        {
            if (!_displayOverlay)
                return;
            var index = TileIndexFromCamera();
            Raylib.DrawText(Raylib.GetFPS().ToString(), 12, 12, 20, Color.WHITE);
            Raylib.DrawText($"{index.Item1},{index.Item2}", 50, 12, 20, Color.WHITE);
            Raylib.DrawText($"{tiles}", 112, 12, 20, Color.WHITE);
            for (var z = 0; z < _maze.GetLength(0); z++)
                for (var x = 0; x < _maze.GetLength(1); x++)
                {
                    var dpos = new Vector2(x * 32f, z * 32f + 40f);
                    Raylib.DrawTextureRec(_mazeblocks, _tileset[_maze[x, z]], dpos, Color.WHITE);
                }
            Raylib.DrawTextureRec(_mazeblocks, new Rectangle(0, 0, 32, 32),
                new Vector2(index.Item1 * 32f, index.Item2 * 32f + 40f), Color.WHITE);
        }

        private (int, int) TileIndexFromCamera()
        {
            return (-(int)Math.Floor(_camera.position.X + 0.5f), -(int) Math.Floor(_camera.position.Z + 0.5f));
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
            var tilesDrawn = 0;
            Raylib.DrawModel(_start, new Vector3(0, 0, 0), Scale, Tint);
            Raylib.DrawModel(_exit, new Vector3(-9, 0, -9), Scale, Tint);
            //for (var z = 0; z < _maze.GetLength(0); z++)
            //    for (var x = 0; x < _maze.GetLength(1); x++)
            //    {
            //        tilesDrawn++;
            //        var dpos = new Vector3(-x, 0, -z);
            //        if (_wireframe)
            //        {
            //            Raylib.DrawModelWires(_parts[_maze[x, z]], dpos, Scale, Tint);
            //            Raylib.DrawModelWires(_mud, dpos, Scale, Tint);
            //            Raylib.DrawModelWires(_moss, dpos, Scale, Tint);
            //        }
            //        else
            //        {
            //            Raylib.DrawModel(_parts[_maze[x, z]], dpos, Scale, Tint);
            //            Raylib.DrawModel(_mud, dpos, Scale, Tint);
            //            Raylib.DrawModel(_moss, dpos, Scale, Tint);
            //        }
            //    }
            var index = TileIndexFromCamera();
            Checkvisibility(index.Item1, index.Item2, Directions.Undefined, 0,ref tilesDrawn);

            Raylib.DrawSphere(_camera.position + Vector3.Normalize(_camera.target - _camera.position) * .1f, .001f, Color.WHITE);
            return tilesDrawn;
        }

        private void Checkvisibility(int x, int z,Directions old, int depth,ref int tilesDrawn)
        {
            if (depth > 7)
                return;
            depth++;
            tilesDrawn++;
            var dpos = new Vector3(-x, 0, -z);
            if (_wireframe)
            {
                Raylib.DrawModelWires(_parts[_maze[x, z]], dpos, Scale, Tint);
                Raylib.DrawModelWires(_mud, dpos, Scale, Tint);
                Raylib.DrawModelWires(_moss, dpos, Scale, Tint);
            }
            else
            {
                Raylib.DrawModel(_parts[_maze[x, z]], dpos, Scale, Tint);
                Raylib.DrawModel(_mud, dpos, Scale, Tint);
                Raylib.DrawModel(_moss, dpos, Scale, Tint);
            }

            var cd = DirectionsFromCamera();
            var directions = MazeGenerator.DirectionsFromblock(_maze[x, z])
                .Except(new[] { MazeGenerator.Opposite(old) }).Where(dir => cd.Contains(dir));
            foreach (var direction in directions)
            {
                var cx = x + MazeGenerator.Dx(direction);
                var cy = z + MazeGenerator.Dy(direction);

                if (0 > cx || cx >= _maze.GetLength(0)
                 || 0 > cy || cy >= _maze.GetLength(1))
                    continue;
                Checkvisibility(cx, cy, direction,depth,ref tilesDrawn);
            }
        }

        public void Dispose()
        {
            Raylib.UnloadModel(_exit);
            foreach (var part in _parts.Values)
                Raylib.UnloadModel(part);
            Raylib.UnloadModel(_mud);
            Raylib.UnloadShader(_shader);

            foreach (var texture in _textures.SelectMany(t => t.Value.Values))
            {
                Raylib.UnloadTexture(texture);
            }
        }
    }
}
