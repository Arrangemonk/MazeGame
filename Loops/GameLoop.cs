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
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

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
        private int specularPosLoc;
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
        //private Gbuffer.MultiRenderTexture mrt;
        public static float Tickscale => Constants.Ticks / Raylib.GetFPS().Map(a => Math.Max(Math.Min(a, 120), 15));
        private int maxdepth = 7;
        public bool Inialized = false;
        private bool moving = false;

        public GameLoop()
        {

            shader = Tools.PrepareShader();

            //mrt = Gbuffer.LoadMultiRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());


            Raylib.DisableCursor();
        }

        public Task StartInit()
        {
            return Task.Run(() =>
            {
                backroundNoise = Raylib.LoadMusicStream("resources/audio/super strange ambient.ogg");
                footsteps = Raylib.LoadMusicStream("resources/audio/footsteps.ogg");
                images = Tools.PrepareImages();
            });
        }

        public void FinishInit(Task task)
        {
            task.Wait();

            var modelshader = shader["normal_mapping"];
            var instanceshader = shader["normal_mapping_instanced"];

            transit = Tools.PrepareModel("stairs", "stairs", modelshader, Matrix4x4.Identity, ref textures, ref images, ref models);
            //wall = Tools.PrepareModel("wall", "pipe", modelshader, Matrix4x4.Identity, ref textures, ref images, ref models);
            spiderweb = Tools.PrepareModel("spiderweb", "spiderweb", modelshader, Matrix4x4.Identity, ref textures, ref images, ref models);
            moss = Tools.PrepareModel("moss", "moss", modelshader, Matrix4x4.Identity, ref textures, ref images, ref models);
            mud = Tools.PrepareModel("floor", "mud", modelshader, Matrix4x4.Identity, ref textures, ref images, ref models);
            mazeblocks = Raylib.LoadTexture($"resources/mazeblocks_{Constants.Blocksize}.png");
            tileset = MazeGenerator.PrepareMazePrint(Constants.Blocksize);
            parts = MazeGenerator.PrepareMazeParts(modelshader, "brick", ref textures, ref images, ref models);
            //_parts = MazeGenerator.PrepareMazeParts(_modelshader, "concrete", ref _textures, ref _models);
            //upipe = MazeGenerator.PrepareUpwardsParts(modelshader, ref textures, ref models);
            //ustairs = MazeGenerator.PrepareStairsParts(modelshader, ref textures, ref models);

            camera = Tools.CameraSetup();
            ResetMaze();
            lightPosLoc = Raylib.GetShaderLocation(modelshader, "lightPos");
            specularPosLoc = Raylib.GetShaderLocation(modelshader, "viewPos");
            instanceLightPosLoc = Raylib.GetShaderLocation(modelshader, "lightPos");
            instanceSpecularPosLoc = Raylib.GetShaderLocation(modelshader, "viewPos");
            instancePosLoc = Raylib.GetShaderLocationAttrib(modelshader, "instanceTransform");
            instanceshader.locs[(int)ShaderLocationIndex.SHADER_LOC_MATRIX_MODEL] = Raylib.GetShaderLocationAttrib(instanceshader, "instanceTransform");
            Raylib.PlayMusicStream(backroundNoise);
            PrepareMazeTexture();
        }

        private void PrepareMazeTexture()
        {
            var size = Constants.Mazesize * Constants.Blocksize;
            mazeTexture = Raylib.LoadRenderTexture(size, size);

            Raylib.BeginTextureMode(mazeTexture);
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
                        Raylib.DrawTextureRec(mazeblocks, rect, dpos, Color.WHITE);
                    }
                    else
                    {
                        Raylib.DrawTextureRec(mazeblocks, tileset[tile], dpos, Color.WHITE);
                    }
                }


            Raylib.EndTextureMode();
            Raylib.GenTextureMipmaps(ref mazeTexture.texture);
            Raylib.SetTextureFilter(mazeTexture.texture, TextureFilter.TEXTURE_FILTER_BILINEAR);
        }

        public void Draw()
        {
            ProcessAudio();
            ProcessInputs();
            UpdateCamera();
            Raylib.BeginDrawing();
            //Gbuffer.Begin(mrt);
            Raylib.ClearBackground(Constants.ClsColor);
            Raylib.BeginMode3D(camera);
            var tiles = DrawLevel();
            Raylib.EndMode3D();
            DrawMazeOverlay(tiles);
            //Gbuffer.End(mrt);

            //Raylib.BeginDrawing();
            //Raylib.ClearBackground(Constants.ClsColor);
            //Raylib.BeginShaderMode(shader["deferred"]);

            //shader.locs[(int)ShaderLocationIndex.SHADER_LOC_COLOR_DIFFUSE] = Raylib.GetShaderLocation(shader, "diffuse");
            //shader.locs[(int)ShaderLocationIndex.SHADER_LOC_COLOR_SPECULAR] = Raylib.GetShaderLocation(shader, "specular");
            //shader.locs[(int)ShaderLocationIndex.SHADER_LOC_MAP_NORMAL] = Raylib.GetShaderLocation(shader, "normalMap");
            //shader.locs[(int)ShaderLocationIndex.SHADER_LOC_MAP_HEIGHT] = Raylib.GetShaderLocation(shader, "heightMap");

            //Raylib.DrawTextureRec(mrt.TexAlbedo, new Rectangle(0, 0, mrt.Width, -mrt.Height), Vector2.Zero, Color.WHITE);

            //Raylib.EndShaderMode();
            Raylib.EndDrawing();
        }

        private void ProcessAudio()
        {
            if (moving)
            {
                if (!Raylib.IsMusicStreamPlaying(footsteps))
                    Raylib.PlayMusicStream(footsteps);
            }
            else
            {
                if (Raylib.IsMusicStreamPlaying(footsteps))
                    Raylib.StopMusicStream(footsteps);
            }

            Raylib.UpdateMusicStream(backroundNoise);
            Raylib.UpdateMusicStream(footsteps);
        }

        private void ProcessInputs()
        {
            var key = (KeyboardKey)Raylib.GetKeyPressed();

            switch (key)
            {
                case KeyboardKey.KEY_F3:
                    {
                        const string screenshots = nameof(screenshots);
                        if (!Directory.Exists(screenshots))
                            Directory.CreateDirectory(screenshots);
                        Raylib.TakeScreenshot($"{screenshots}/{Guid.NewGuid()}.png");
                        break;
                    }
                case KeyboardKey.KEY_R:
                    ResetMaze();
                    break;
                case KeyboardKey.KEY_F1:
                    displayOverlay = !displayOverlay;
                    break;
                case KeyboardKey.KEY_F2:
                    wireframe = !wireframe;
                    break;
                case KeyboardKey.KEY_UP:
                    maxdepth++;
                    break;
                case KeyboardKey.KEY_DOWN:
                    maxdepth--;
                    break;
                case KeyboardKey.KEY_F4:
                    Program.Togglefullscreen();
                    break;
                case KeyboardKey.KEY_F5:
                    render3d = !render3d;
                    break;
                case KeyboardKey.KEY_F6:
                    collision = !collision;
                    break;
            }
        }

        private void ResetMaze()
        {
            oldpos = camera.position = Constants.DefaultOffset;
            maze = MazeGenerator.GenerateMaze(Constants.Mazesize, Constants.Mazesize);
            randoms = MazeGenerator.GenerateRandomIntegers(Constants.Mazesize, Constants.Mazesize);
            PrepareMazeTexture();
            Tools.ResetCamera(ref camera, maze[0, 0]);
        }

        private void UpdateCamera()
        {
            float cameraMoveSpeed = 0.03f * Tickscale;
            float cameraMouseMoveSensitivity = 0.003f * Tickscale;

            var mousePositionDelta = Raylib.GetMouseDelta();
            var cam = camera;
            moving = false;
            unsafe
            {

                if (Raylib.IsKeyDown(KeyboardKey.KEY_W))
                {
                    Raylib.CameraMoveForward(&cam, cameraMoveSpeed, true);
                    moving = true;
                }

                if (Raylib.IsKeyDown(KeyboardKey.KEY_A))
                {
                    Raylib.CameraMoveRight(&cam, -cameraMoveSpeed, true);
                    moving = true;
                }

                if (Raylib.IsKeyDown(KeyboardKey.KEY_S))
                {
                    Raylib.CameraMoveForward(&cam, -cameraMoveSpeed, true);
                    moving = true;
                }

                if (Raylib.IsKeyDown(KeyboardKey.KEY_D))
                {
                    Raylib.CameraMoveRight(&cam, cameraMoveSpeed, true);
                    moving = true;
                }

                if (collision)
                    (cam.position, cam.target) = Tools.Collision(oldpos, cam.position, oldtarget, cam.target, maze);

                var relativetarget = Vector3.Normalize(cam.target - cam.position);

                cam.position = Tools.Clamp(cam.position, Constants.Maxcam);

                cam.target = cam.position + relativetarget;

                Raylib.CameraYaw(&cam, -mousePositionDelta.X * cameraMouseMoveSensitivity, false);
                Raylib.CameraPitch(&cam, -mousePositionDelta.Y * cameraMouseMoveSensitivity, true, false, false);

                var modelshader = shader["normal_mapping"];
                var instanceshader = shader["normal_mapping_instanced"];

                Raylib.SetShaderValue(modelshader, lightPosLoc, Tools.DrawOffsetByQuadrant(Tools.Clamp(cam.position + relativetarget * 0.2f, Constants.Maxcam), cam.position), ShaderUniformDataType.SHADER_UNIFORM_VEC3);
                Raylib.SetShaderValue(modelshader, specularPosLoc, cam.position, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
                Raylib.SetShaderValue(instanceshader, instanceLightPosLoc, Tools.DrawOffsetByQuadrant(Tools.Clamp(cam.position + relativetarget * 0.2f, Constants.Maxcam), cam.position), ShaderUniformDataType.SHADER_UNIFORM_VEC3);
                Raylib.SetShaderValue(instanceshader, instanceSpecularPosLoc, cam.position, ShaderUniformDataType.SHADER_UNIFORM_VEC3);

                oldpos = cam.position;
                oldtarget = cam.target;

            }

            camera = cam;

        }

        private void DrawMazeOverlay(HashSet<(int, int)> tiles)
        {
            if (!displayOverlay)
                return;
            var index = TileIndexFromCamera();
            Raylib.DrawText(Raylib.GetFPS().ToString(), 12, 12, 20, Color.WHITE);
            Raylib.DrawText($"{camera.position.X:0.000},{camera.position.Z:0.000}", 60, 12, 20, Color.WHITE);
            Raylib.DrawText($"{index.Item1:0.000},{index.Item2:0.000}", 60, 32, 20, Color.WHITE);
            Raylib.DrawText($"{tiles.Count} {maxdepth}", 220, 12, 20, Color.WHITE);
            var startposx = Raylib.GetScreenWidth() / 2 - mazeTexture.texture.width / 2;
            var startposy = Raylib.GetScreenHeight() / 2 - mazeTexture.texture.height / 2;

            var camx = camera.position.X;
            var camz = camera.position.Z;


            var cameradirection = Vector3.Normalize(camera.target - camera.position);
            Raylib.DrawTexturePro(mazeTexture.texture,
                new Rectangle(MathF.Floor(camx * Constants.Blocksize + mazeTexture.texture.width / 2f),
                    MathF.Floor(-camz * Constants.Blocksize - mazeTexture.texture.height / 2f),
                    mazeTexture.texture.width,
                    -mazeTexture.texture.height),
                new Rectangle(startposx, startposy, mazeTexture.texture.width, mazeTexture.texture.height),
                 new Vector2(0, 0),
                0, new Color(255, 255, 255, 128));
            foreach (var tile in tiles)
            {
                var dx = startposx + mazeTexture.texture.width / 2f + Tools.DrawOffsetByQuadrantUi(tile.Item1 - camx) * Constants.Blocksize;
                var dy = startposy + mazeTexture.texture.height / 2f + Tools.DrawOffsetByQuadrantUi(tile.Item2 - camz) * Constants.Blocksize;

                Raylib.DrawRectangle(
                    (int)dx,
                    (int)dy, Constants.Blocksize, Constants.Blocksize, new Color(0, 0, 255, 64));
            }
            Raylib.DrawRectangle(
                startposx + mazeTexture.texture.width / 2,
                startposy + mazeTexture.texture.height / 2, Constants.Blocksize - 2, Constants.Blocksize - 2, Color.RED);
            var half = Constants.Blocksize / 2;
            cameradirection = cameradirection * half;
            Raylib.DrawRectangle(
                startposx + mazeTexture.texture.width / 2 + (int)cameradirection.X + half / 2,
                startposy + mazeTexture.texture.height / 2 + (int)cameradirection.Z + half / 2, half, half, Color.BLACK);
        }


        private HashSet<(int, int)> DrawLevel()
        {
            var dpos = Tools.DrawOffsetByQuadrant(Constants.DefaultOffset, camera.position);
            Raylib.DrawModel(transit, dpos, Constants.Scale, Constants.Tint);
           
            dpos = Tools.DrawOffsetByQuadrant(
                    new Vector3(Constants.Exitpos, 1, Constants.Exitpos) + Constants.DefaultOffset, camera.position);

            Raylib.DrawModel(transit, dpos, Constants.Scale, Constants.Tint);

            var drawList = new HashSet<(int, int)>();
            Checkvisibility(camera.position.X, camera.position.Z, ref drawList);
            if (!render3d) return drawList;

            foreach (var tup in drawList)
            {
                DrawTile(tup.Item1, tup.Item2);

            }

            //DrawTiles(drawList);

            Rlgl.rlDisableDepthMask();
            Raylib.BeginBlendMode(BlendMode.BLEND_ADD_COLORS);
            foreach (var tup in drawList.Where(elem => TileCondition(elem, 10)))
            {
                dpos = Tools.DrawOffsetByQuadrant(new Vector3(tup.Item1, 0, tup.Item2) + Constants.DefaultOffset,
                    camera.position);
                Raylib.DrawModel(spiderweb, dpos, Constants.Scale, Constants.Tint);
            }

            Raylib.EndBlendMode();
            Rlgl.rlEnableDepthMask();
            if (displayOverlay)
                Raylib.DrawSphere(camera.position + Vector3.Normalize(camera.target - camera.position) * .1f,
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
            var floatDirection = Vector3.Normalize(camera.target - camera.position);
            foreach (var dir in MazeGenerator.Dirx(floatDirection.X))
                yield return dir;
            foreach (var dir in MazeGenerator.Diry(floatDirection.Z))
                yield return dir;

        }

        private (int, int) TileIndexFromCamera()
        {
            return ((int)Math.Floor(camera.position.X), (int)Math.Floor(camera.position.Z));
        }

        private void Checkvisibility(float camx, float camz, ref HashSet<(int, int)> drawList)
        {
            var x = (int)Math.Floor(camx);
            var z = (int)Math.Floor(camz);

            var floatDirection = Vector3.Normalize(camera.target - camera.position);
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



        private void DrawTile(int x, int z)
        {
            var dpos = Tools.DrawOffsetByQuadrant(new Vector3(x, 0, z) + Constants.DefaultOffset, camera.position);
            var tile = maze[x, z];
            if (wireframe)
            {
                {
                    Raylib.DrawModelWires(parts[tile], dpos, Constants.Scale, Constants.Tint);


                    if (tile < Blocks.Room)
                    {
                        Raylib.DrawModelWires(moss, dpos, Constants.Scale, Constants.Tint);
                        Raylib.DrawModelWires(mud, dpos, Constants.Scale, Constants.Tint);
                    }
                }
            }
            else
            {
                {
                    Raylib.DrawModel(parts[tile], dpos, Constants.Scale, Constants.Tint);
                    Raylib.DrawModel(mud, dpos, Constants.Scale, Constants.Tint);

                    if (tile < Blocks.Room)
                    {
                        Raylib.DrawModel(moss, dpos, Constants.Scale, Constants.Tint);
                    }
                }
            }
        }

        public void Dispose()
        {

            foreach (var model in models)
                Raylib.UnloadModel(model);

            foreach (var texture in textures.SelectMany(t => t.Value.Values))
                Raylib.UnloadTexture(texture);

            foreach (var image in images.Values)
                Raylib.UnloadImage(image);

            Raylib.UnloadTexture(mazeblocks);
            Raylib.UnloadRenderTexture(mazeTexture);
            Raylib.UnloadMusicStream(backroundNoise);
            Raylib.UnloadMusicStream(footsteps);
        }
    }
}
