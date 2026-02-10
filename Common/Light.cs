using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mazeGame.Common;

namespace MazeGame.Common
{
    /*******************************************************************************************
    *
    *   raylib [shaders] example - basic pbr
    *
    *   Example complexity rating: [★★★★] 4/4
    *
    *   Example originally created with raylib 5.0, last time updated with raylib 5.5
    *
    *   Example contributed by Afan OLOVCIC (@_DevDad) and reviewed by Ramon Santamaria (@raysan5)
    *
    *   Example licensed under an unmodified zlib/libpng license, which is an OSI-certified,
    *   BSD-like license that allows static linking with closed source software
    *
    *   Copyright (c) 2023-2025 Afan OLOVCIC (@_DevDad)
    ********************************************************************************************/

    using Raylib_CSharp;
    using Raylib_CSharp.Camera.Cam3D;
    using Raylib_CSharp.Colors;
    using Raylib_CSharp.Rendering;
    using Raylib_CSharp.Shaders;
    using Raylib_CSharp.Textures;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using System.Threading.Tasks;

    namespace mazeGame.Common
    {
        public enum LightType
        {
            LIGHT_DIRECTIONAL = 0,
            LIGHT_POINT,
            LIGHT_SPOT
        }
        public unsafe record Light
        {
            private static int lightCount = 0;
            private int LightIndex { get; set; }
            public bool Enabled { get; set; }
            public Color Color { get; set; }
            public Vector3 Position { get; set; }
            private LightType Type { get; set; }
            private Vector3 Target { get; set; }
            private float Intensity { get; set; }
            private int TypeLoc { get; set; }
            private int EnabledLoc { get; set; }
            private int PositionLoc { get; set; }
            private int TargetLoc { get; set; }
            private int ColorLoc { get; set; }
            private int IntensityLoc { get; set; }

            private int LightVPLoc { get; set; }
            private int ShadowMapLoc { get; set; }

            private RenderTexture2D ShadowMap { get; set; }

            private Camera3D LightCamera { get; set; }

            Matrix4x4 LightView { get; set; }
            Matrix4x4 LightProj { get; set; }
            Matrix4x4 LightViewProj { get; set; }

            const int textureActiveSlot = 10; // Can be anything 0 to 15, but 0 will probably be taken up

            public Light(LightType type, Vector3 position, Vector3 target, Color color, float intensity, Shader shader, int resolution)
            {
                Enabled = true;
                Type = type;
                Position = position;
                Target = target;
                Color = color;
                Intensity = intensity;

                // NOTE: Shader parameters names for lights must match the requested ones
                EnabledLoc = shader.GetLocation($"lights[{lightCount}].enabled");
                TypeLoc = shader.GetLocation($"lights[{lightCount}].type");
                PositionLoc = shader.GetLocation($"lights[{lightCount}].position");
                TargetLoc = shader.GetLocation($"lights[{lightCount}].target");
                ColorLoc = shader.GetLocation($"lights[{lightCount}].color");
                IntensityLoc = shader.GetLocation($"lights[{lightCount}].intensity");

                LightVPLoc = shader.GetLocation($"lights[{lightCount}].lightVP");
                ShadowMapLoc = shader.GetLocation($"lights[{lightCount}].shadowMap");

                ShadowMap = Shadowmap.LoadShadowmapRenderTexture(resolution, resolution);

                LightCamera = new Camera3D(Position, Target, Vector3.UnitY, 90f, CameraProjection.Perspective);

                Update(shader);
                LightIndex = lightCount;
                lightCount++;
            }

            public void Draw(Action drawScene)
            {
                LightCamera = LightCamera with { Position = Position };
                Graphics.BeginTextureMode(ShadowMap);
                Graphics.ClearBackground(Color.White);
                Graphics.BeginMode3D(LightCamera);
                LightView = RlGl.GetMatrixModelView();
                LightProj = RlGl.GetMatrixProjection();
                drawScene();
                Graphics.EndMode3D();
                Graphics.EndTextureMode();
                LightViewProj = RayMath.MatrixMultiply(LightView, LightProj);
            }

            public void Update(Shader shader)
            {
                var enabled = Enabled;
                var activeslot = textureActiveSlot + LightIndex;
                shader.SetValue(EnabledLoc, enabled, ShaderUniformDataType.Int);
                shader.SetValue(TypeLoc, (int)Type, ShaderUniformDataType.Int);
                shader.SetValue(PositionLoc, Position, ShaderUniformDataType.Vec3);
                shader.SetValue(TargetLoc, Target, ShaderUniformDataType.Vec3);
                shader.SetValue(ColorLoc, new Vector4(Color.R / 255.0f, Color.G / 255.0f, Color.B / 255.0f, Color.A / 255.0f), ShaderUniformDataType.Vec4);
                shader.SetValue(IntensityLoc, Intensity, ShaderUniformDataType.Float);
                shader.SetValueMatrix(LightVPLoc, LightViewProj);
                RlGl.ActiveTextureSlot(activeslot);
                RlGl.EnableTexture(ShadowMap.Depth.Id);
                RlGl.SetUniform(ShadowMapLoc, (nint)(&activeslot), ShaderUniformDataType.Int, 1);
            }

            public void Unload()
            {
                Shadowmap.UnloadShadowmapRenderTexture(ShadowMap);
            }
        }
    }

}
