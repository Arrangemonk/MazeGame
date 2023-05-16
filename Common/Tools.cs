using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using MazeGame.Algorithms;
using Raylib_cs;
using static System.Net.Mime.MediaTypeNames;

namespace MazeGame.Common
{

    public class Tools
    {
        public static Color ColorFromFloat(float r, float g, float b, float a)
        {
            return new Color((int)(r * 255f), (int)(g * 255f), (int)(b * 255f), (int)(a * 255f));
        }

        public const float Pi = (float)Math.PI;
        public static (Vector3, Vector3) Collision(Vector3 oldPos, Vector3 newPos, Vector3 oldTgt, Vector3 newTgt, Blocks[,] maze)
        {
            float width = GameLoop.Mazesize;
            float height = GameLoop.Mazesize;


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
            x = Clamp(x, GameLoop.Mazesize);
            z = Clamp(z, GameLoop.Mazesize);
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
                target = new Vector3(1, 0, 0),
                up = new Vector3(0.0f, 1.0f, 0.0f),
                position = new Vector3(0.5f, 0, 0.5f),
                fovy = 45.0f,
                projection = CameraProjection.CAMERA_PERSPECTIVE,
            };
        }
    }
}
