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
using Raylib_cs;
using static System.Net.Mime.MediaTypeNames;

namespace MazeGame.Common
{

    public static class Tools
    {
        public static T Map<T>(this T input, Func<T, T> method) => method(input);

        public static Color ColorFromFloat(float r, float g, float b, float a)
        {
            return new Color((int)(r * 255f), (int)(g * 255f), (int)(b * 255f), (int)(a * 255f));
        }

        public const float Pi = (float)Math.PI;
        public static (Vector3, Vector3) Collision(Vector3 oldPos, Vector3 newPos, Vector3 oldTgt, Vector3 newTgt, Blocks[,] maze)
        {
            float width = Constants.Mazesize;
            float height = Constants.Mazesize;


            var tx = Clamp(newPos.X, width);
            var tz = Clamp(newPos.Z, height);

            var tile = GetBoundarysFromMaze((int)(tx), (int)(tz), maze);

            var isRoom = tile >= Blocks.Room;

            var oldx = Clamp(oldPos.X, 1.0f);
            var oldz = Clamp(oldPos.Z, 1.0f);

            var x = Clamp(tx, 1.0f);
            var z = Clamp(tz, 1.0f);

            var minD = 1.0f - Range(isRoom);
            var maxD = Range(isRoom);

            var minX = 1.0f - Range(isRoom || ((int)tile & (int)Directions.East) != 0);
            var maxX = Range(isRoom || ((int)tile & (int)Directions.West) != 0);
            var minZ = 1.0f - Range(isRoom || ((int)tile & (int)Directions.North) != 0);
            var maxZ = Range(isRoom || ((int)tile & (int)Directions.South) != 0);

            var rectc = (
                oldx >= minX &&
                oldx <= maxX &&
                z >= minD &&
                z <= maxD
            );

            var recdb = (
                oldx >= minD &&
                oldx <= maxD &&
                z >= minZ &&
                z <= maxZ
            );

            var recte = (
                x >= minX &&
                x <= maxX &&
                oldz >= minD &&
                oldz <= maxD
            );

            var rectf = (
                x >= minD &&
                x <= maxD &&
                oldz >= minZ &&
                oldz <= maxZ
            );

            var canMoveX = (recte || rectf);
            var canMoveZ = (rectc || recdb);



            return (new Vector3(canMoveX ? newPos.X : oldPos.X,
                        newPos.Y,
                        canMoveZ ? newPos.Z : oldPos.Z),
                new Vector3(canMoveX ? newTgt.X : oldTgt.X,
                    newTgt.Y,
                    canMoveZ ? newTgt.Z : oldTgt.Z));

        }

        public static float Clamp(float input, float max)
        {
            return (((input) % max) + max) % max;
        }

        public static int Clamp(int input, int max)
        {
            return ((input % max) + max) % max;
        }

        public static Vector3 Clamp(Vector3 input, Vector3 max)
        {
            return new Vector3(Clamp(input.X, max.X), input.Y, Clamp(input.Z, max.Z));
        }

        private static float Range(bool condiditon)
        {
            return condiditon ? 3f : 0.8f;
        }

        public static Blocks GetBoundarysFromMaze(int x, int z, Blocks[,] maze)
        {
            x = Clamp(x, Constants.Mazesize);
            z = Clamp(z, Constants.Mazesize);
            return maze[x, z];

        }



        public static Model PrepareModel(string modelName, string textureName, Shader shader, Matrix4x4 transform, ref Dictionary<string, Dictionary<string, Texture2D>> textures, ref List<Model> models)
        {
            unsafe
            {
                const string diff = nameof(diff);
                const string normal = nameof(normal);
                const string spec = nameof(spec);

                var model = Raylib.LoadModel($"resources/models/{modelName}.obj");
                models.Add(model);

                Texture2D d, n, s;

                if (!textures.ContainsKey(textureName))
                {
                    var texture = new Dictionary<string, Texture2D>();
                    d = MountTexture(textureName, diff, ref texture);
                    n = MountTexture(textureName, normal, ref texture);
                    s = MountTexture(textureName, spec, ref texture);
                    textures.Add(textureName, texture);
                }
                else
                {
                    var texture = textures[textureName];
                    d = texture[diff];
                    n = texture[normal];
                    s = texture[spec];
                }

                model.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_DIFFUSE].texture = d;
                model.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_NORMAL].texture = n;
                model.materials[0].maps[(int)MaterialMapIndex.MATERIAL_MAP_SPECULAR].texture = s;

                shader.locs[(int)ShaderLocationIndex.SHADER_LOC_COLOR_DIFFUSE] = Raylib.GetShaderLocation(shader, "diffuse");
                shader.locs[(int)ShaderLocationIndex.SHADER_LOC_COLOR_SPECULAR] = Raylib.GetShaderLocation(shader, "specular");
                shader.locs[(int)ShaderLocationIndex.SHADER_LOC_MAP_NORMAL] = Raylib.GetShaderLocation(shader, "normalMap");
                model.materials[0].shader = shader;
                model.transform = transform;
                Raylib.GenMeshTangents(model.meshes);
                return model;
            }
        }

        private static Texture2D MountTexture(string textureName, string type, ref Dictionary<string, Texture2D> textures)
        {
            const string path = "resources/textures/{0}_{1}.{2}";
            var texture = Raylib.LoadTexture(string.Format(path, textureName, type, "dds"));
            Raylib.GenTextureMipmaps(ref texture);
            Raylib.SetTextureFilter(texture, TextureFilter.TEXTURE_FILTER_TRILINEAR);
            textures.Add(type, texture);
            return texture;
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
                target = new Vector3(Constants.Mazesize/2f + 1.5f, 0, Constants.Mazesize / 2f + 0.5f),
                up = new Vector3(0.0f, 1.0f, 0.0f),
                position = new Vector3(Constants.Mazesize / 2f + 0.5f, 0, Constants.Mazesize / 2f + 0.5f),
                fovy = 45.0f,
                projection = CameraProjection.CAMERA_PERSPECTIVE,
            };
        }

        public static void Drawtrangle(Vector3 direction, int originX, int originZ, int maxdepth, ref HashSet<(int, int)> drawList)
        {
            var sinPhi = -direction.X;
            var cosPhi = -direction.Z;

            originX = (int)MathF.Round(originX - direction.X * 2, MidpointRounding.AwayFromZero);
            originZ = (int)MathF.Round(originZ - direction.Z * 2, MidpointRounding.AwayFromZero);

            var halflength = (maxdepth) / 2f;
            var dirx = (int)(direction.X * halflength);
            var dirz = (int)(direction.Z * halflength);

            var pLeftX = dirx + (int)MathF.Round(((-cosPhi - sinPhi) * (halflength + 3)) + originX, MidpointRounding.AwayFromZero);
            var pLeftZ = dirz + (int)MathF.Round(((sinPhi - cosPhi) * (halflength + 3)) + originZ, MidpointRounding.AwayFromZero);
            var pRightX = dirx + (int)MathF.Round(((cosPhi - sinPhi) * (halflength + 3)) + originX, MidpointRounding.AwayFromZero);
            var pRightZ = dirz + (int)MathF.Round(((-sinPhi - cosPhi) * (halflength + 3)) + originZ, MidpointRounding.AwayFromZero);

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

                    if (!(s >= 0) || !(t >= 0) || !(s + t <= 1))
                        continue;
                    var cx = Tools.Clamp(x, Constants.Mazesize);
                    var cz = Tools.Clamp(z, Constants.Mazesize);
                    drawList.Add(((int)MathF.Round(cx, MidpointRounding.AwayFromZero), (int)MathF.Round(cz, MidpointRounding.AwayFromZero)));
                }
            }
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

            var ccam = Tools.Clamp(campos, Constants.Mazesize) is < Constants.Mazesize * 0.25f or > Constants.Mazesize * 0.75f;

            var result = drawpos + Constants.Mazesize * ((draw && !cam && ccam) ? 1.0f : (!draw && cam && ccam) ? -1.0f : 0.0f);

            return result;
        }

        public static float DrawOffsetByQuadrantUi(float drawpos)
        {
            drawpos = Tools.Clamp(drawpos, Constants.Mazesize);

            return (drawpos >= Constants.Mazesize * 0.5f) ? drawpos - Constants.Mazesize: drawpos;
        }
    }
}
