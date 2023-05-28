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

namespace MazeGame.Common
{

    public static class Tools
    {
        public static T2 Map<T1, T2>(this T1 input, Func<T1, T2> method) => method(input);

        public static Color ColorFromFloat(float r, float g, float b, float a)
        {
            return new Color((int)(r * 255f), (int)(g * 255f), (int)(b * 255f), (int)(a * 255f));
        }

        public const float Pi = (float)Math.PI;

        public static Dictionary<Blocks, Rectangle[]> boxes = MazeGenerator.PrepareMazeCollosion();
        public static (Vector3, Vector3) Collision(Vector3 oldPos, Vector3 newPos, Vector3 oldTgt, Vector3 newTgt, Blocks[,] maze)
        {
            var (newtile,x,z) = GetIile(newPos.X, newPos.Z, maze);
            var (oldxtile, oldx, dz) = GetIile(oldPos.X, newPos.Z, maze);
            var (oldztile, dx, oldz) = GetIile(newPos.X, oldPos.Z, maze);

            var cxz = boxes[newtile].Aggregate(false, (current, box) => current || CheckCollition(new Vector2(x, z), box));
            var coldx = boxes[oldxtile].Aggregate(false, (current, box) => current || CheckCollition(new Vector2(oldx, z), box));
            var coldz = boxes[oldztile].Aggregate(false, (current, box) => current || CheckCollition(new Vector2(x, oldz), box));

            var canMoveX = (!cxz || !coldz);
            var canMoveZ = (!cxz || !coldx);

            return (new Vector3(canMoveX ? newPos.X : oldPos.X,
                        newPos.Y,
                        canMoveZ ? newPos.Z : oldPos.Z),
                new Vector3(canMoveX ? newTgt.X : oldTgt.X,
                    newTgt.Y,
                    canMoveZ ? newTgt.Z : oldTgt.Z));

        }

        private static bool CheckCollition(Vector2 point, Rectangle box)
        {
            return box.x <= point.X && point.X <= box.x + box.width
                && box.y <= point.Y && point.Y <= box.y + box.height;
        }

        private static (Blocks,float,float) GetIile(float xIn, float zIn, Blocks[,] maze)
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



        public static Model PrepareModel(string modelName, string textureName, Shader shader, Matrix4x4 transform, ref Dictionary<string, Dictionary<string, Texture2D>> textures, ref List<Model> models)
        {
            unsafe
            {
                const string diff = nameof(diff);
                const string normal = nameof(normal);
                const string spec = nameof(spec);
                const string disp = nameof(disp);

                var model = Raylib.LoadModel($"resources/models/{modelName}.obj");
                models.Add(model);

                Texture2D d, n, s, h;

                var usedName = File.Exists(getTexturePath(Path.Combine(textureName, textureName), diff))
                    ? Path.Combine(textureName, textureName)
                    : File.Exists(getTexturePath(modelName, diff))
                        ? modelName
                        : textureName;

                if (!textures.ContainsKey(usedName))
                {
                    var texture = new Dictionary<string, Texture2D>();
                    d = MountTexture(usedName, diff, ref texture);
                    n = MountTexture(usedName, normal, ref texture);
                    s = MountTexture(usedName, spec, ref texture);
                    h = MountTexture(usedName, disp, ref texture);
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
        }

        private static Texture2D MountTexture(string textureName, string type, ref Dictionary<string, Texture2D> textures)
        {
            var texture = Raylib.LoadTexture(getTexturePath(textureName, type));
            Raylib.SetTextureFilter(texture, TextureFilter.TEXTURE_FILTER_TRILINEAR);
            Raylib.GenTextureMipmaps(ref texture);
            textures.Add(type, texture);
            return texture;
        }

        private static string getTexturePath(string textureName, string type)
        {
            const string path = "resources/textures/{0}_{1}.{2}";
            return string.Format(path, textureName, type, "dds");
        }

        public static Dictionary<string, Shader> PrepareShader()
        {
            var result = new Dictionary<string, Shader>();
            LoadIfExists(result, "normal_mapping");
            LoadIfExists(result, "geom");
            // result.Add("geom");

            return result;
        }

        private static void LoadIfExists(Dictionary<string, Shader> result, string name)
        {
            const string path = "resources/shaders/";
            if (File.Exists($"{path}{name}.vs.glsl"))
                result.Add(name, Raylib.LoadShader($"{path}{name}.vs.glsl", $"{path}{name}.fs.glsl"));
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

        public static void Drawtrangle(Vector3 direction, int originX, int originZ, int maxdepth, Blocks[,] maze, ref HashSet<(int, int)> drawList)
        {
            var sinPhi = -direction.X;
            var cosPhi = -direction.Z;

            originX = (int)MathF.Round(originX - direction.X * 2, MidpointRounding.AwayFromZero);
            originZ = (int)MathF.Round(originZ - direction.Z * 2, MidpointRounding.AwayFromZero);

            var dirx = (int)(direction.X * maxdepth * 0.5);
            var dirz = (int)(direction.Z * maxdepth * 0.5);
            var tlength = maxdepth;

            var pLeftX = dirx + (int)MathF.Round((-cosPhi - sinPhi) * tlength + originX, MidpointRounding.AwayFromZero);
            var pLeftZ = dirz + (int)MathF.Round((sinPhi - cosPhi) * tlength + originZ, MidpointRounding.AwayFromZero);
            var pRightX = dirx + (int)MathF.Round((cosPhi - sinPhi) * tlength + originX, MidpointRounding.AwayFromZero);
            var pRightZ = dirz + (int)MathF.Round((-sinPhi - cosPhi) * tlength + originZ, MidpointRounding.AwayFromZero);

            var maxX = (int)MathF.Max(originX, Math.Max(pLeftX, pRightX));
            var minX = (int)MathF.Min(originX, Math.Min(pLeftX, pRightX));
            var maxY = (int)MathF.Max(originZ, Math.Max(pLeftZ, pRightZ));
            var minY = (int)MathF.Min(originZ, Math.Min(pLeftZ, pRightZ));

            /* spanning vectors of edge (v1,v2) and (v1,v3) */
            var vs1 = new Vector2(pLeftX - originX, pLeftZ - originZ);
            var vs2 = new Vector2(pRightX - originX, pRightZ - originZ);

            for (var x = minX; x <= maxX; x++)
            {
                for (var z = minY; z <= maxY; z++)
                {
                    var q = new Vector2(x - originX, z - originZ);

                    var s = CrossProduct(q, vs2) / CrossProduct(vs1, vs2);
                    var t = CrossProduct(vs1, q) / CrossProduct(vs1, vs2);

                    var cx = Clamp(x, Constants.Mazesize);
                    var cz = Clamp(z, Constants.Mazesize);
                    if (!(s >= 0) || !(t >= 0) || !(s + t <= 1))
                    {
                        drawList.Remove((cx, cz));
                        continue;
                    }

                    if (IsNearRoom(cx, cz, maze))
                        drawList.Add((cx, cz));
                }
            }
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
