using System.Numerics;
using MazeGame.Algorithms;
using MazeGame.Common;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static MazeGame.Common.Tools;

namespace HelloWorld
{
    static class Program
    {
        private static int sizex = 1600;
        private static int sizey = 900;

        public static void Main()
        {

            unsafe
            {
                var textures = new Dictionary<string, Dictionary<string, Texture2D>>();
                var maze = MazeGenerator.GenerateMaze(10, 10);

                MazeGenerator.PrintMaze(maze);

                SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT);
                InitWindow(sizex, sizey, "Maze Game");


                SetTargetFPS(60);
                var shader = PrepareShader();
                var start = PrepareModel("start", "exit", shader, Matrix4x4.Identity, ref textures);
                var exit = PrepareModel("exit", "exit", shader, Matrix4x4.Identity, ref textures);
                var mud = PrepareModel("floor", "mud", shader, Matrix4x4.Identity, ref textures);

                var parts = MazeGenerator.PrepareMazeParts(shader, ref textures);

                var angle = 0.0f;
                var camera = CameraSetup();



                // Diffuse light
                var lightPosLoc = GetShaderLocation(shader, "lightPos");
                //specular light
                var specularPosLoc = GetShaderLocation(shader, "viewPos");
                var scale = 1.0f / 30;
                var tint = Color.WHITE;
                var speed = 0.2f;

                Vector3 oldpos = camera.position;

                DisableCursor();
                while (!WindowShouldClose())
                {
                    UpdateCamera(ref camera, CameraMode.CAMERA_FIRST_PERSON);
                    camera.position = Vector3.Lerp(oldpos, camera.position, speed);

                    camera.position = Collision(oldpos, camera.position,maze);
                    oldpos = camera.position;

                    SetShaderValue(shader, lightPosLoc, camera.position + Vector3.Normalize(camera.target - camera.position) * .1f, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
                    SetShaderValue(shader, specularPosLoc, camera.position, ShaderUniformDataType.SHADER_UNIFORM_VEC3);

                    BeginDrawing();
                    ClearBackground(Color.DARKGRAY);
                    BeginMode3D(camera);

                    DrawModel(start, new Vector3(0,0,0), scale, tint);
                    DrawModel(exit, new Vector3(-9, 0, -9), scale, tint);
                    for (var z = 0; z < maze.GetLength(0); z++)
                        for (var x = 0; x < maze.GetLength(1); x++)
                        {
                            var dpos = new Vector3(-x, 0, -z);
                            DrawModel(parts[maze[x, z]], dpos, scale, tint);
                            DrawModel(mud, dpos, scale, tint);
                            //DrawModelWires(parts[maze[x, z]], dpos, scale, tint);
                            //DrawModelWires(mud, dpos, scale, tint);
                        }



                    //DrawGrid(10, .5f);
                     DrawSphere(camera.position + Vector3.Normalize( camera.target - camera.position) *.1f  , .001f, Color.WHITE);
                    EndMode3D();



                    DrawText(GetFPS().ToString(), 12, 12, 20, Color.WHITE);
                    //DrawText(camera.position.ToString(), 12, 32, 20, Color.WHITE);
                    //DrawText($"{(int)(camera.position.X - 0.5f)},{(int)(camera.position.Z - 0.5f)}", 12, 52, 20, Color.WHITE);
                    const float high = short.MaxValue - .5f;
                    DrawText($"{(-camera.position.X + high) % 1.0f:0.000},{ (-camera.position.Z + high) % 1.0f:0.000}", 12, 72, 20, Color.WHITE);
                    //DrawText($"{boundarys.Item1},{boundarys.Item2},{boundarys.Item3},{boundarys.Item4}", 12, 92, 20, Color.WHITE);


                    EndDrawing();
                }

                UnloadModel(exit);
                foreach (var part in parts.Values)
                    UnloadModel(part);
                UnloadModel(mud);
                UnloadShader(shader);

                foreach (var texture in textures.SelectMany(t => t.Value.Values))
                {
                    UnloadTexture(texture);
                }

                CloseWindow();
            }
        }
    }
}