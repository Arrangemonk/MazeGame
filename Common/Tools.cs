using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.XPath;
using MazeGame.Algorithms;
using Microsoft.VisualBasic;
using Raylib_cs;
using static System.Net.Mime.MediaTypeNames;
using Image = Raylib_cs.Image;
using System.Runtime.Intrinsics.Arm;

namespace MazeGame.Common
{

    public static unsafe class Tools
    {
        public static T2 Map<T1, T2>(this T1 input, Func<T1, T2> method) => method(input);

        public static Color ColorFromFloat(float r, float g, float b, float a)
        {
            return new Color((int)(r * 255f), (int)(g * 255f), (int)(b * 255f), (int)(a * 255f));
        }

        public const float Pi = (float)Math.PI;

        public static Dictionary<Blocks, Rectangle[]> Boxes = MazeGenerator.PrepareMazeCollosion();
        public static (Vector3, Vector3) Collision(Vector3 oldPos, Vector3 newPos, Vector3 oldTgt, Vector3 newTgt, Blocks[,] maze)
        {
            var (newtile, x, z) = GetIile(newPos.X, newPos.Z, maze);
            var (oldxtile, oldx, dz) = GetIile(oldPos.X, newPos.Z, maze);
            var (oldztile, dx, oldz) = GetIile(newPos.X, oldPos.Z, maze);

            var cxz = Boxes[newtile].Aggregate(false, (current, box) => current || Raylib.CheckCollisionPointRec(new Vector2(x, z), box));
            var coldx = Boxes[oldxtile].Aggregate(false, (current, box) => current || Raylib.CheckCollisionPointRec(new Vector2(oldx, z), box));
            var coldz = Boxes[oldztile].Aggregate(false, (current, box) => current || Raylib.CheckCollisionPointRec(new Vector2(x, oldz), box));

            var canMoveX = (!cxz || !coldz);
            var canMoveZ = (!cxz || !coldx);

            return (new Vector3(canMoveX ? newPos.X : oldPos.X,
                        newPos.Y,
                        canMoveZ ? newPos.Z : oldPos.Z),
                new Vector3(canMoveX ? newTgt.X : oldTgt.X,
                    newTgt.Y,
                    canMoveZ ? newTgt.Z : oldTgt.Z));

        }

        public static Matrix4x4 TranslateMatrix(Vector3 vector) => Raymath.MatrixTranslate(vector.X, vector.Y, vector.Z);

        public static Dictionary<string, Image> PrepareImages()
        {
            var result = new Dictionary<string, Image>();
            foreach (var file in new DirectoryInfo("resources/textures").GetFiles("*.dds"))
            {
                result.Add(Path.GetFileNameWithoutExtension(file.Name), Raylib.LoadImage(file.FullName));
            }

            foreach (var file in new DirectoryInfo("resources/textures/brick").GetFiles("*.dds"))
            {
                result.Add(Path.Combine("brick", Path.GetFileNameWithoutExtension(file.Name)), Raylib.LoadImage(file.FullName));
            }

            return result;

        }

        public static Color ColorLerp(Color first, Color second, float amount)
        {
            var neg = 1.0 - amount;
            return new Color(
                (int)(first.r * amount + second.r * neg),
                (int)(first.g * amount + second.g * neg),
                (int)(first.b * amount + second.b * neg),
                (int)(first.a * amount + second.a * neg));

        }

        //private static bool CheckCollition(Vector2 point, Rectangle box)
        //{
        //    return box.x <= point.X && point.X <= box.x + box.width
        //        && box.y <= point.Y && point.Y <= box.y + box.height;
        //}

        private static (Blocks, float, float) GetIile(float xIn, float zIn, Blocks[,] maze)
        {
            var tx = Clamp(xIn, Constants.Mazesize);
            var tz = Clamp(zIn, Constants.Mazesize);

            var tile = maze[(int)tx, (int)tz];

            tile = (tile < Blocks.Room) ? tile : (Blocks)((int)Blocks.RoomBlocked + (int)RoomDirections(tx, tz, maze));
            return (tile, Clamp(tx, 1.0f), Clamp(tz, 1.0f));
        }

        public static float Clamp(float input, float max)
        {
            return (((input) % max) + max) % max;
        }

        public static int Clamp(int input, int max)
        {
            return ((input % max) + max) % max;
        }

        public static (int, int) Clamp((int, int) input, int max)
        {
            return (Clamp(input.Item1, max), Clamp(input.Item2, max));
        }

        public static Vector3 Clamp(Vector3 input, Vector3 max)
        {
            return new Vector3(Clamp(input.X, max.X), input.Y, Clamp(input.Z, max.Z));
        }

        private static float Range(bool condiditon)
        {
            return condiditon ? 3f : 0.8f;
        }



        public static Model PrepareModel(string modelName, string textureName, Shader shader, Matrix4x4 transform, ref Dictionary<string, Dictionary<string, Texture2D>> textures, ref Dictionary<string, Image> images, ref List<Model> models)
        {


            const string diff = nameof(diff);
            const string normal = nameof(normal);
            const string spec = nameof(spec);
            const string disp = nameof(disp);

            var model = Raylib.LoadModel($"resources/models/{modelName}.obj");
            models.Add(model);

            Texture2D d, n, s, h;

            var usedName = images.Keys.Contains(GetTexturePath(Path.Combine(textureName, textureName), diff))
                ? Path.Combine(textureName, textureName)
                : images.Keys.Contains(GetTexturePath(modelName, diff))
                    ? modelName
                    : textureName;

            if (!textures.ContainsKey(usedName))
            {
                var texture = new Dictionary<string, Texture2D>();
                d = MountTexture(usedName, diff, ref texture, ref images);
                n = MountTexture(usedName, normal, ref texture, ref images);
                s = MountTexture(usedName, spec, ref texture, ref images);
                h = MountTexture(usedName, disp, ref texture, ref images);
                textures.Add(usedName, texture);
            }
            else
            {
                var texture = textures[usedName];
                d = texture[diff];
                n = texture[normal];
                s = texture[spec];
                h = texture[disp];
            }

            model.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_DIFFUSE].texture = d;
            model.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_NORMAL].texture = n;
            model.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_SPECULAR].texture = s;
            model.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_HEIGHT].texture = h;

            shader.locs[(int)ShaderLocationIndex.SHADER_LOC_COLOR_DIFFUSE] = Raylib.GetShaderLocation(shader, "diffuse");
            shader.locs[(int)ShaderLocationIndex.SHADER_LOC_COLOR_SPECULAR] = Raylib.GetShaderLocation(shader, "specular");
            shader.locs[(int)ShaderLocationIndex.SHADER_LOC_MAP_NORMAL] = Raylib.GetShaderLocation(shader, "normalMap");
            shader.locs[(int)ShaderLocationIndex.SHADER_LOC_MAP_HEIGHT] = Raylib.GetShaderLocation(shader, "heightMap");
            model.materials[0].shader = shader;
            model.transform = transform;
            Raylib.GenMeshTangents(model.meshes);
            return model;

        }

        private static Texture2D MountTexture(string textureName, string type, ref Dictionary<string, Texture2D> textures, ref Dictionary<string, Image> images)
        {
            var texture = Raylib.LoadTextureFromImage(images[GetTexturePath(textureName, type)]);
            Raylib.SetTextureFilter(texture, TextureFilter.TEXTURE_FILTER_TRILINEAR);
            Raylib.GenTextureMipmaps(ref texture);
            textures.Add(type, texture);
            return texture;
        }

        private static string GetTexturePath(string textureName, string type)
        {
            const string path = "{0}_{1}";
            return string.Format(path, textureName, type);
        }

        public static Dictionary<string, Shader> PrepareShader()
        {
            var result = new Dictionary<string, Shader>();
            LoadIfExists(result, "normal_mapping");
            LoadIfExists(result, "normal_mapping_instanced");
            //LoadIfExists(result, "deferred");
            //LoadIfExists(result, "gbuffer");

            return result;
        }

        private static void LoadIfExists(Dictionary<string, Shader> result, string name)
        {
            const string path = "resources/shaders/";
            if (File.Exists($"{path}{name}.vs.glsl"))
            {
                var shader = Raylib.LoadShader($"{path}{name}.vs.glsl", $"{path}{name}.fs.glsl");
                result.Add(name, shader);

            }

        }


        public static Camera3D CameraSetup()
        {
            return new Camera3D
            {
                target = new Vector3(1.5f, 0, +0.5f),
                up = new Vector3(0.0f, 1.0f, 0.0f),
                position = Constants.DefaultOffset,
                fovy = 45.0f,
                projection = CameraProjection.CAMERA_PERSPECTIVE,
            };
        }

        public static void ResetCamera(ref Camera3D camera, Blocks block)
        {
            var dirs = MazeGenerator.DirectionsFromblock(block).ToArray();
            var dx = MazeGenerator.Dx(dirs.FirstOrDefault(dir => ((int)dir & (int)Blocks.Horizontal) != 0, Directions.North));
            var dz = dx != 0 ? 0 : MazeGenerator.Dy(dirs.FirstOrDefault(dir => ((int)dir & (int)Blocks.Vertical) != 0, Directions.East));

            camera.position = Constants.DefaultOffset;
            camera.target = new Vector3(dx + 0.5f, 0, dz + 0.5f);
            camera.up = new Vector3(0.0f, 1.0f, 0.0f);
        }

        public static void Drawtrangle(Vector3 direction, float originX, float originZ, float maxdepth, Blocks[,] maze, ref HashSet<(int, int)> drawList)
        {
            var sinPhi = -direction.X;
            var cosPhi = -direction.Z;

            originX = originX - direction.X * 2f;
            originZ = originZ - direction.Z * 2f;

            var dirx = (direction.X * maxdepth * 0.5f);
            var dirz = (direction.Z * maxdepth * 0.5f);
            var tlength = maxdepth;

            var pLeftX = dirx + (-cosPhi - sinPhi) * tlength + originX;
            var pLeftZ = dirz + (sinPhi - cosPhi) * tlength + originZ;
            var pRightX = dirx + (cosPhi - sinPhi) * tlength + originX;
            var pRightZ = dirz + (-sinPhi - cosPhi) * tlength + originZ;


            var tmpdrawlist = new List<(int, int)>();

            Trangle.DrawTriangle(new Vector2(originX, originZ), new Vector2(pLeftX, pLeftZ), new Vector2(pRightX, pRightZ),
                (x1, x2, z) =>
                {
                    if (x1 > x2)
                        (x1, x2) = (x2, x1);
                    for (var x = x1; x <= x2; x++)
                    {
                        if (IsNearRoom(Clamp(x,Constants.Mazesize), z, maze))
                            tmpdrawlist.Add(Clamp((x, z),Constants.Mazesize));
                    }

                });

            foreach (var e in tmpdrawlist)
                drawList.Add(e);
        }




        private static bool IsNearRoom(int x, int z, Blocks[,] maze)
        {
            for (var tx = -1; tx <= 1; tx++)
            {
                for (var tz = -1; tz <= 1; tz++)
                {
                    var cx = Clamp(x + tx, Constants.Mazesize);
                    var cz = Clamp(z + tz, Constants.Mazesize);
                    if (Blocks.Room <= maze[cx, cz])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        //private static Blocks RoomDirections(int x, int z, Blocks[,] maze)
        //{
        //    int result = 0;

        //    if (maze[Clamp(x - 1, Constants.Mazesize), z].Map(block => Blocks.Room <= block || ((int)block & (int)Directions.West) != 0))
        //        result += (int)Directions.East;

        //    if (maze[Clamp(x + 1, Constants.Mazesize), z].Map(block => Blocks.Room <= block || ((int)block & (int)Directions.East) != 0))
        //        result += (int)Directions.West;

        //    if (maze[x, Clamp(z - 1, Constants.Mazesize)].Map(block => Blocks.Room <= block || ((int)block & (int)Directions.South) != 0))
        //        result += (int)Directions.North;

        //    if (maze[x, Clamp(z + 1, Constants.Mazesize)].Map(block => Blocks.Room <= block || ((int)block & (int)Directions.North) != 0))
        //        result += (int)Directions.South;

        //    return (Blocks)result;
        //}

        private static readonly (int, int, Directions, Directions)[] Combinations = new[]
        {
            (-1, 0, Directions.East, Directions.West),
            (1, 0, Directions.West, Directions.East),
            (0, -1, Directions.North, Directions.South),
            (0, 1, Directions.South, Directions.North),
        };

        private static Blocks RoomDirections(float x, float z, Blocks[,] maze)
        {
            int result = 0;
            foreach (var combination in Combinations)
            {
                if (maze[(int)Clamp(x + combination.Item1, Constants.Mazesize),
                        (int)Clamp(z + combination.Item2, Constants.Mazesize)]
                    .Map(block => Blocks.Room <= block || ((int)block & (int)combination.Item4) != 0))
                    result += (int)combination.Item3;
            }
            return (Blocks)result;
        }


        private static float CrossProduct(Vector2 v1, Vector2 v2)
            => (v1.X * v2.Y) - (v1.Y * v2.X);

        public static Vector3 DrawOffsetByQuadrant(Vector3 drawpos, Vector3 campos)
        {
            return new Vector3(DrawOffsetByQuadrant(drawpos.X, campos.X), drawpos.Y, DrawOffsetByQuadrant(drawpos.Z, campos.Z));
        }

        public static float DrawOffsetByQuadrant(float drawpos, float campos)
        {
            var draw = drawpos < Constants.Mazesize * 0.5f;

            var cam = campos < Constants.Mazesize * 0.5f;

            var ccam = Clamp(campos, Constants.Mazesize) is < Constants.Mazesize * 0.25f or > Constants.Mazesize * 0.75f;

            var result = drawpos + Constants.Mazesize * ((draw && !cam && ccam) ? 1.0f : (!draw && cam && ccam) ? -1.0f : 0.0f);

            return result;
        }

        public static float DrawOffsetByQuadrantUi(float drawpos)
        {
            drawpos = Clamp(drawpos, Constants.Mazesize);

            return (drawpos >= Constants.Mazesize * 0.5f) ? drawpos - Constants.Mazesize : drawpos;
        }
    }
}
