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

namespace MazeGame.Common
{

    public class Tools
    {
        public static Color ColorFromFloat(float r, float g, float b, float a)
        {
            return new Color((int)(r * 255f), (int)(g * 255f), (int)(b * 255f), (int)(a * 255f));
        }

        public const float Pi = (float)Math.PI;
        public static (Vector3, Vector3) Collision(Vector3 oldpos, Vector3 newpos, Vector3 oldTarget, Vector3 newTarget, Blocks[,] maze)
        {
            const float high = short.MaxValue - .5f;

            var tile = GetBoundarysFromMaze((int)(newpos.X - .5f), (int)(newpos.Z - .5f), maze);

            //clamp to range 0 to 1 but offsett by .5

            var oldx = (-oldpos.X + high) % 1.0f;
            var oldz = (-oldpos.Z + high) % 1.0f;

            var x = (-newpos.X + high) % 1.0f;
            var z = (-newpos.Z + high) % 1.0f;

            var minD = 1.0f - Range(false);
            var maxD = Range(false);

            var minX = 1.0f - Range(tile >= Blocks.Room || ((int)tile & (int)Directions.East) != 0);
            var maxX = Range(tile >= Blocks.Room || ((int)tile & (int)Directions.West) != 0);
            var minZ = 1.0f - Range(tile >= Blocks.Room || ((int)tile & (int)Directions.North) != 0);
            var maxZ = Range(tile >= Blocks.Room || ((int)tile & (int)Directions.South) != 0);

            var recta = (
                x >= minX &&
                x <= maxX &&
                z >= minD &&
                z <= maxD
            );

            var rectb = (
                x >= minD &&
                x <= maxD &&
                z >= minZ &&
                z <= maxZ
            );


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

            var canmoveboth = (recta || rectb);
            var canmovex = (recte || rectf);
            var canmovez = (rectc || recdb);


            return canmoveboth ? (newpos, newTarget) :
                canmovex ? (new Vector3(newpos.X, newpos.Y, oldpos.Z), new Vector3(newTarget.X, newTarget.Y, oldTarget.Z)) :
                canmovez ? (new Vector3(oldpos.X, newpos.Y, newpos.Z), new Vector3(oldTarget.X, newTarget.Y, newTarget.Z)) :
                (oldpos, oldTarget);

        }

        private static float Range(bool condiditon)
        {
            return condiditon ? 2f : 0.8f;
        }

        public static Blocks GetBoundarysFromMaze(int x, int z, Blocks[,] maze)
        {
            x = -x;
            if (x < 0)
                x = 0;
            z = -z;
            if (z < 0)
                z = 0;
            return maze[x, z];

        }



        public static Model PrepareModel(string modelName, string textureName, Shader shader, Matrix4x4 transform, ref Dictionary<string, Dictionary<string, Texture2D>> textures,ref List<Model> models)
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

        public static Dictionary<string,Shader> PrepareShader()
        {
            var result = new Dictionary<string,Shader>();
            LoadIfExists(result, "normal_mapping");
            LoadIfExists(result,"geom");
            // result.Add("geom");

            return result;
        }

        private static void LoadIfExists(Dictionary<string, Shader> result,string name)
        {
            const string path = "resources/shaders/";
            if (File.Exists($"{path}{name}.vs"))
                result.Add(name, Raylib.LoadShader($"{path}{name}.vs", $"{path}{name}.fs"));
        }

        public static Camera3D CameraSetup()
        {
            return new Camera3D
            {
                target = new Vector3(-1, 0, 0),
                up = new Vector3(0.0f, 1.0f, 0.0f),
                position = new Vector3(0, 0, 0),
                fovy = 45.0f,
                projection = CameraProjection.CAMERA_PERSPECTIVE,
            };
        }
    }
}
