using MazeGame.Algorithms;
using MazeGame.Algorithms;
using MazeGame.Common;
using Raylib_CSharp;
using Raylib_CSharp.Audio;
using Raylib_CSharp.Camera.Cam3D;
using Raylib_CSharp.Collision;
using Raylib_CSharp.Colors;
using Raylib_CSharp.Geometry;
using Raylib_CSharp.Images;
using Raylib_CSharp.Interact;
using Raylib_CSharp.Rendering;
using Raylib_CSharp.Shaders;
using Raylib_CSharp.Textures;
using Raylib_CSharp.Transformations;
using Raylib_CSharp.Windowing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MazeGame.Loops
{
    public unsafe class GameLoop : IDisposable
    {
        private Dictionary<string, Shader> shader;
        private Model transit;
        private Model moss;
        private Model mud;
        private Model wall;
        private Model spiderweb;
        private Texture2D mazeblocks;
        private RenderTexture2D mazeTexture;

        private Dictionary<string, Dictionary<string, Texture2D>> textures = new();
        private Dictionary<string, Image> images = new();
        private List<Model> models = new();

        private Blocks[,] maze;
        private int[,] randoms;

        private Dictionary<Blocks, Rectangle> tileset;
        private Dictionary<Blocks, Model> parts;
        //private Dictionary<Blocks, Model> upipe;
        //private Dictionary<Blocks, Model> ustairs;
        private Camera3D camera;

        private int lightPosLoc;
        private int viewPosLoc;
        private int timePosLoc;
        private int instancePosLoc;
        private int instanceLightPosLoc;
        private int instanceSpecularPosLoc;

        private Vector3 oldpos;
        private Vector3 oldtarget;
        private bool displayOverlay;
        private bool wireframe;
        private bool render3d = true;
        private bool collision = true;
        private Music backroundNoise;
        private Music footsteps;
        public static float Tickscale => Constants.Ticks / Time.GetFPS().Map(a => Math.Max(Math.Min(a, 120), 15));
        private int maxdepth = 7;
        public bool Inialized = false;
        private bool moving = false;

        public GameLoop()
        {

            shader = Tools.PrepareShader();

            //mrt = Gbuffer.LoadMultiRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());


            Input.DisableCursor();
        }

        public Task StartInit()
        {
            return Task.Run(() =>
            {
                backroundNoise = Music.Load("resources/audio/super strange ambient.ogg");
                footsteps = Music.Load("resources/audio/footsteps.ogg");
                images = Tools.PrepareImages();
            });
        }

        public void FinishInit(Task task)
        {
            task.Wait();

            var modelshader = shader["normal_mapping"];
            //var instanceshader = shader["normal_mapping_instanced"];

           

            transit = Tools.PrepareModel("stairs", "stairs", modelshader, Matrix4x4.Identity, ref textures, ref images, ref models, Constants.ClsColor);
            //wall = Tools.PrepareModel("wall", "pipe", modelshader, Matrix4x4.Identity, ref textures, ref images, ref models);
            spiderweb = Tools.PrepareModel("spiderweb", "spiderweb", modelshader, Matrix4x4.Identity, ref textures, ref images, ref models);
            moss = Tools.PrepareModel("moss", "moss", modelshader, Matrix4x4.Identity, ref textures, ref images, ref models, Constants.ClsColor);
            mud = Tools.PrepareModel("floor", "mud", modelshader, Matrix4x4.Identity, ref textures, ref images, ref models, Constants.ClsColor);
            mazeblocks = Texture2D.Load($"resources/mazeblocks_{Constants.Blocksize}.png");
            tileset = MazeGenerator.PrepareMazePrint(Constants.Blocksize);
            parts = MazeGenerator.PrepareMazeParts(modelshader, "brick", ref textures, ref images, ref models, Constants.ClsColor);
            //_parts = MazeGenerator.PrepareMazeParts(_modelshader, "concrete", ref _textures, ref _models);
            //upipe = MazeGenerator.PrepareUpwardsParts(modelshader, ref textures, ref models);
            //ustairs = MazeGenerator.PrepareStairsParts(modelshader, ref textures, ref models);

            camera = Tools.CameraSetup();
            ResetMaze();
            lightPosLoc = modelshader.GetLocation("lightPos");
            viewPosLoc = modelshader.GetLocation("viewPos");
            timePosLoc = modelshader.GetLocation("time");
            //instanceLightPosLoc = modelshader.GetLocation("lightPos");
            //instanceSpecularPosLoc = modelshader.GetLocation("viewPos");
            //instancePosLoc = Raylib.GetShaderLocationAttrib(modelshader, "instanceTransform");
            //instanceshader.locs[(int)ShaderLocationIndex.SHADER_LOC_MATRIX_MODEL] = Raylib.GetShaderLocationAttrib(instanceshader, "instanceTransform");
            backroundNoise.PlayStream();
            PrepareMazeTexture();
        }

        private void PrepareMazeTexture()
        {
            var size = Constants.Mazesize * Constants.Blocksize;
            mazeTexture = RenderTexture2D.Load(size, size);

            Graphics.BeginTextureMode(mazeTexture);
            for (var z = 0; z < Constants.Mazesize; z++)
                for (var x = 0; x < Constants.Mazesize; x++)
                {
                    var dpos = new Vector2(x * Constants.Blocksize, z * Constants.Blocksize);
                    var tile = maze[x, z];
                    if (tile >= Blocks.Room)
                    {
                        var top = maze[x, Tools.Clamp(z - 1, Constants.Mazesize)];
                        var left = maze[Tools.Clamp(x - 1, Constants.Mazesize), z];

                        var ntop = top < Blocks.Room && ((int)top & (int)Directions.South) == 0;
                        var nleft = left < Blocks.Room && ((int)left & (int)Directions.West) == 0;

                        var rect = MazeGenerator.Mazerect((ntop && nleft) ? 0 : nleft ? 1 : ntop ? 2 : 4, 0, Constants.Blocksize);
                        Graphics.DrawTextureRec(mazeblocks, rect, dpos, Color.White);
                    }
                    else
                    {
                        Graphics.DrawTextureRec(mazeblocks, tileset[tile], dpos, Color.White);
                    }
                }


            Graphics.EndTextureMode();
            mazeTexture.Texture.GenMipmaps();
            mazeTexture.Texture.SetFilter(TextureFilter.Bilinear);
        }

        public void Draw()
        {
            ProcessAudio();
            ProcessInputs();
            UpdateCamera();
            Graphics.BeginDrawing();
            Graphics.ClearBackground(Constants.ClsColor);
            Graphics.BeginMode3D(camera);
            var tiles = DrawLevel();
            Graphics.EndMode3D();
            DrawMazeOverlay(tiles);
            Graphics.EndDrawing();
        }

        private void ProcessAudio()
        {
            if (moving)
            {
                if (!footsteps.IsStreamPlaying())
                    footsteps.PlayStream();
            }
            else
            {
                if (footsteps.IsStreamPlaying())
                    footsteps.StopStream();
            }

            backroundNoise.UpdateStream();
            footsteps.UpdateStream();
        }

        private void ProcessInputs()
        {
            var key = (KeyboardKey)Input.GetKeyPressed();

            switch (key)
            {
                case KeyboardKey.F3:
                    {
                        const string screenshots = nameof(screenshots);
                        if (!Directory.Exists(screenshots))
                            Directory.CreateDirectory(screenshots);
                        Raylib.TakeScreenshot($"{screenshots}/{Guid.NewGuid()}.png");
                        break;
                    }
                case KeyboardKey.R:
                    ResetMaze();
                    break;
                case KeyboardKey.F1:
                    displayOverlay = !displayOverlay;
                    break;
                case KeyboardKey.F2:
                    wireframe = !wireframe;
                    break;
                case KeyboardKey.Up:
                    maxdepth++;
                    break;
                case KeyboardKey.Down:
                    maxdepth--;
                    break;
                case KeyboardKey.F4:
                    Program.Togglefullscreen();
                    break;
                case KeyboardKey.F5:
                    render3d = !render3d;
                    break;
                case KeyboardKey.F6:
                    collision = !collision;
                    break;
            }
        }

        private void ResetMaze()
        {
            oldpos = camera.Position = Constants.DefaultOffset;
            maze = MazeGenerator.GenerateMaze(Constants.Mazesize, Constants.Mazesize);
            randoms = MazeGenerator.GenerateRandomIntegers(Constants.Mazesize, Constants.Mazesize);
            PrepareMazeTexture();
            Tools.ResetCamera(ref camera, maze[0, 0]);
        }

        private void UpdateCamera()
        {
            float cameraMoveSpeed = 0.03f * Tickscale;
            float cameraMouseMoveSensitivity = 0.003f * Tickscale;

            var mousePositionDelta = Input.GetMouseDelta();
            var cam = camera;
            moving = false;
            unsafe
            {

                if (Input.IsKeyDown(KeyboardKey.W))
                {
                    cam.MoveForward(cameraMoveSpeed,true);
                    moving = true;
                }

                if (Input.IsKeyDown(KeyboardKey.A))
                {
                    cam.MoveRight(-cameraMoveSpeed, true);
                    moving = true;
                }

                if (Input.IsKeyDown(KeyboardKey.S))
                {
                    cam.MoveForward(-cameraMoveSpeed, true);
                    moving = true;
                }

                if (Input.IsKeyDown(KeyboardKey.D))
                {
                    cam.MoveRight(cameraMoveSpeed, true);
                    moving = true;
                }

                if (collision)
                    (cam.Position, cam.Target) = Tools.Collision(oldpos, cam.Position, oldtarget, cam.Target, maze);

                var relativetarget = Vector3.Normalize(cam.Target - cam.Position);

                cam.Position = Tools.Clamp(cam.Position, Constants.Maxcam);

                cam.Target = cam.Position + relativetarget;

                cam.RotateYaw(-mousePositionDelta.X * cameraMouseMoveSensitivity, false);
               cam.RotatePitch(-mousePositionDelta.Y * cameraMouseMoveSensitivity, true, false, false);

                var modelshader = shader["normal_mapping"];
                modelshader.SetValue( lightPosLoc, Tools.DrawOffsetByQuadrant(Tools.Clamp(cam.Position - relativetarget * 0.2f, Constants.Maxcam), cam.Position), ShaderUniformDataType.Vec3);
                modelshader.SetValue( viewPosLoc, cam.Position, ShaderUniformDataType.Vec3);
                modelshader.SetValue(timePosLoc,(float)Time.GetTime(), ShaderUniformDataType.Float);
                modelshader.SetValue(modelshader.GetLocation("resolution"), new Vector2(Window.GetScreenWidth(), Window.GetScreenHeight()), ShaderUniformDataType.Vec2);
                oldpos = cam.Position;
                oldtarget = cam.Target;

            }

            camera = cam;

        }

        private void DrawMazeOverlay(HashSet<(int, int)> tiles)
        {
            if (!displayOverlay)
                return;
            var index = TileIndexFromCamera();
            Graphics.DrawText(Time.GetFPS().ToString(), 12, 12, 20, Color.White);
            Graphics.DrawText($"{camera.Position.X:0.000},{camera.Position.Z:0.000}", 60, 12, 20, Color.White);
            Graphics.DrawText($"{index.Item1:0.000},{index.Item2:0.000}", 60, 32, 20, Color.White);
            Graphics.DrawText($"{tiles.Count} {maxdepth}", 220, 12, 20, Color.White);
            var startposx = Window.GetScreenWidth() / 2 - mazeTexture.Texture.Width / 2;
            var startposy = Window.GetScreenHeight() / 2 - mazeTexture.Texture.Height / 2;

            var camx = camera.Position.X;
            var camz = camera.Position.Z;


            var cameradirection = Vector3.Normalize(camera.Target - camera.Position);
            Graphics.DrawTexturePro(mazeTexture.Texture,
                new Rectangle(MathF.Floor(camx * Constants.Blocksize + mazeTexture.Texture.Width / 2f),
                    MathF.Floor(-camz * Constants.Blocksize - mazeTexture.Texture.Height / 2f),
                    mazeTexture.Texture.Width,
                    -mazeTexture.Texture.Height),
                new Rectangle(startposx, startposy, mazeTexture.Texture.Width, mazeTexture.Texture.Height),
                 new Vector2(0, 0),
                0, new Color(255, 255, 255, 128));
            foreach (var tile in tiles)
            {
                var dx = startposx + mazeTexture.Texture.Width / 2f + Tools.DrawOffsetByQuadrantUi(tile.Item1 - camx) * Constants.Blocksize;
                var dy = startposy + mazeTexture.Texture.Height / 2f + Tools.DrawOffsetByQuadrantUi(tile.Item2 - camz) * Constants.Blocksize;

                Graphics.DrawRectangle(
                    (int)dx,
                    (int)dy, Constants.Blocksize, Constants.Blocksize, new Color(0, 0, 255, 64));
            }
            Graphics.DrawRectangle(
                startposx + mazeTexture.Texture.Width / 2,
                startposy + mazeTexture.Texture.Height / 2, Constants.Blocksize - 2, Constants.Blocksize - 2, Color.Red);
            var half = Constants.Blocksize / 2;
            cameradirection = cameradirection * half;
            Graphics.DrawRectangle(
                startposx + mazeTexture.Texture.Width / 2 + (int)cameradirection.X + half / 2,
                startposy + mazeTexture.Texture.Height / 2 + (int)cameradirection.Z + half / 2, half, half, Color.Black);
        }


        private HashSet<(int, int)> DrawLevel()
        {
            var dpos = Tools.DrawOffsetByQuadrant(Constants.DefaultOffset, camera.Position);
            Graphics.DrawModel(transit, dpos, Constants.Scale, Constants.Tint);
           
            dpos = Tools.DrawOffsetByQuadrant(
                    new Vector3(Constants.Exitpos, 1, Constants.Exitpos) + Constants.DefaultOffset, camera.Position);

            Graphics.DrawModel(transit, dpos, Constants.Scale, Constants.Tint);

            var drawList = new HashSet<(int, int)>();
            Checkvisibility(camera.Position.X, camera.Position.Z, ref drawList);
            if (!render3d) return drawList;

            foreach (var tup in drawList.Distinct())
            {
                DrawTile(tup.Item1, tup.Item2);
            }

            //DrawTiles(drawList);
            
            RlGl.DisableDepthMask();
            Graphics.BeginBlendMode(BlendMode.AddColors);
            foreach (var tup in drawList.Where(elem => TileCondition(elem, 10)))
            {
                dpos = Tools.DrawOffsetByQuadrant(new Vector3(tup.Item1, 0, tup.Item2) + Constants.DefaultOffset,
                    camera.Position);
                Graphics.DrawModel(spiderweb, dpos, Constants.Scale, Constants.Tint);
            }
            

            Graphics.EndBlendMode();
            RlGl.EnableDepthMask();
            if (displayOverlay)
                Graphics.DrawSphere(camera.Position + Vector3.Normalize(camera.Target - camera.Position) * .1f,
                    .001f, new Color(255, 255, 255, 64));

            return drawList;
        }

        //private void DrawTiles(HashSet<(int, int)> drawList)
        //{
        //    var groups = drawList.GroupBy(tile => maze[tile.Item1, tile.Item2]).ToDictionary(l => l.Key, l => l.Select(o =>o).ToArray());


        //    //Matrix matTransform = MatrixMultiply(MatrixMultiply(matScale, matRotation), matTranslation);
        //    foreach (var key in groups.Keys)
        //    {
        //        var matrixes = groups[key].Select(o =>
        //            Raymath.MatrixMultiply(Raymath.MatrixScale(Constants.Scale, Constants.Scale, Constants.Scale),
        //            Raymath.MatrixMultiply(parts[key].transform,
        //                Tools.TranslateMatrix(Tools.DrawOffsetByQuadrant(new Vector3(o.Item1, 0, o.Item2) + Constants.DefaultOffset, camera.position))))
        //            )


        //            .ToArray();

        //        if (wireframe)
        //            Rlgl.rlEnableWireMode();
        //        DrawModelInstanced(parts[key], matrixes);
        //        DrawModelInstanced(mud, matrixes);

        //        if (wireframe)
        //            Rlgl.rlDisableWireMode();

        //    }

        //}

        //private static unsafe void DrawModelInstanced(Model model, Matrix4x4[] transforms)
        //{
        //    for (var i = 0; i < model.meshCount; i++)
        //    {

        //        Raylib.DrawMeshInstanced(model.meshes[i], model.materials[0], transforms, transforms.Length);
        //    }
        //}

        private bool TileCondition((int, int) elem, int thresholdup, int tresholdlow = 0)
        {
            return randoms[elem.Item1, elem.Item2].Map(e => tresholdlow < e && e < thresholdup) && maze[elem.Item1, elem.Item2] < Blocks.Room;
        }

        private IEnumerable<Directions> DirectionsFromCamera()
        {
            var floatDirection = Vector3.Normalize(camera.Target - camera.Position);
            foreach (var dir in MazeGenerator.Dirx(floatDirection.X))
                yield return dir;
            foreach (var dir in MazeGenerator.Diry(floatDirection.Z))
                yield return dir;

        }

        private (int, int) TileIndexFromCamera()
        {
            return ((int)Math.Floor(camera.Position.X), (int)Math.Floor(camera.Position.Z));
        }

        private void Checkvisibility(float camx, float camz, ref HashSet<(int, int)> drawList)
        {
            var x = (int)Math.Floor(camx);
            var z = (int)Math.Floor(camz);

            var floatDirection = Vector3.Normalize(camera.Target - camera.Position);
            if (CheckvisibilityLoop(x, z, Directions.Undefined, DirectionsFromCamera().ToArray(), 0, ref drawList))
                Tools.Drawtrangle(floatDirection, x, z, maxdepth, maze, ref drawList);

            ////safety zone

            for (var tx = -1; tx <= 1; tx++)
            {
                for (var tz = -1; tz <= 1; tz++)
                {
                    var cx = Tools.Clamp(x + tx, Constants.Mazesize);
                    var cz = Tools.Clamp(z + tz, Constants.Mazesize);
                    drawList.Add((cx, cz));
                }
            }
        }

        private bool CheckvisibilityLoop(int x, int z, Directions old, Directions[] cd, int depth, ref HashSet<(int, int)> drawList)
        {
            var drawtrangle = false;
            Stack<(int, int, Directions, int)> stack = new Stack<(int, int, Directions, int)>();
            stack.Push((x, z, old, depth));

            while (stack.Count > 0)
            {
                var (currentX, currentZ, currentOld, currentDepth) = stack.Pop();

                if (maze[currentX, currentZ] < Blocks.Room)
                    drawList.Add((currentX, currentZ));
                else
                    drawtrangle = true;

                currentDepth++;
                if (currentDepth > maxdepth)
                    continue;

                var directions = MazeGenerator.DirectionsFromblock(maze[currentX, currentZ])
                    .Except(new[] { MazeGenerator.Opposite(currentOld), Directions.Undefined });

                foreach (var direction in directions)
                {
                    var percievedDepth = cd.Contains(direction) ? currentDepth : maxdepth;

                    var cx = Tools.Clamp(currentX + MazeGenerator.Dx(direction), Constants.Mazesize);
                    var cz = Tools.Clamp(currentZ + MazeGenerator.Dy(direction), Constants.Mazesize);

                    if(!drawList.Contains((cx,cz)))
                        stack.Push((cx, cz, direction, percievedDepth));
                }
            }

            return drawtrangle;
        }

        private void AudioProcessEffectLPF(float[] buffer, uint frames)
        {

            // Converts the buffer data before using it
            for (uint i = 0; i < frames * 2; i += 2)
            {
                int delay = 4410; // 0.1 second delay at 44.1kHz
                buffer[i] = buffer[i] + buffer[Math.Max(0,i-delay)];
            }
        }



        private void DrawTile(int x, int z)
        {
            var dpos = Tools.DrawOffsetByQuadrant(new Vector3(x, 0, z) + Constants.DefaultOffset, camera.Position);
            var tile = maze[x, z];
            var scale= Constants.Scale;
            //var scale = 1.0f;
            if (wireframe)
            {
                {
                    Graphics.DrawModelWires(parts[tile], dpos, scale, Constants.Tint);

                    if (tile < Blocks.Room)
                    {
                        Graphics.DrawModelWires(moss, dpos, scale, Constants.Tint);
                        Graphics.DrawModelWires(mud, dpos, scale, Constants.Tint);
                    }
                }
            }
            else
            {
                {
                    Graphics.DrawModel(parts[tile], dpos, scale, Constants.Tint);
                    Graphics.DrawModel(mud, dpos, scale, Constants.Tint);

                    if (tile < Blocks.Room)
                    {
                        Graphics.DrawModel(moss, dpos, scale, Constants.Tint);
                    }
                }
            }
        }

        public void Dispose()
        {

            foreach (var model in models)
                model.Unload();

            foreach (var texture in textures.SelectMany(t => t.Value.Values))
                texture.Unload();

            foreach (var image in images.Values)
                image.Unload();

            mazeblocks.Unload();
            mazeTexture.Unload();
            backroundNoise.UnloadStream();
            footsteps.UnloadStream();
        }
    }
}
