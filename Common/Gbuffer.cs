using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;

namespace MazeGame.Common
{
    internal class Gbuffer
    {
        public struct MultiRenderTexture
        {
            public uint Id;        // OpenGL framebuffer object id
            public int Width;              // Color buffers width (same all buffers)
            public int Height;             // Color buffers height (same all buffers)
            public Texture2D TexAlbedo;     // Color buffer attachment: color data , alpha
            public Texture2D TexSpecular;    // Color buffer attachment: roughness, metallness, height
            public Texture2D TexNormal;      // Color buffer attachment: normal data, maybe ambient occlusion in alpha 
            public Texture2D TexPosition;    // Color buffer attachment: position data
            public Texture2D TexDepth;       // Depth buffer attachment
        }


        // Load multi render texture (framebuffer)
        // NOTE: Render texture is loaded by default with RGBA color attachment and depth RenderBuffer
        public static unsafe MultiRenderTexture LoadMultiRenderTexture(int width, int height)
        {
            MultiRenderTexture target = new()
            {
                Id = Rlgl.rlLoadFramebuffer(width, height), // Load an empty framebuffer
                Width = width,
                Height = height
            };

            if (target.Id > 0)
            {
                Rlgl.rlEnableFramebuffer(target.Id);

                // Create color texture: color
                target.TexAlbedo.id = Rlgl.rlLoadTexture(null, width, height, PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8, 1);
                target.TexAlbedo.width = width;
                target.TexAlbedo.height = height;
                target.TexAlbedo.format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8;
                target.TexAlbedo.mipmaps = 1;

                target.TexSpecular.id = Rlgl.rlLoadTexture(null, width, height, PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8, 1);
                target.TexSpecular.width = width;
                target.TexSpecular.height = height;
                target.TexSpecular.format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8;
                target.TexSpecular.mipmaps = 1;

                // Create color texture: normal
                target.TexNormal.id = Rlgl.rlLoadTexture(null, width, height, PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8, 1);
                target.TexNormal.width = width;
                target.TexNormal.height = height;
                target.TexNormal.format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8;
                target.TexNormal.mipmaps = 1;

                // Create color texture: position
                target.TexPosition.id = Rlgl.rlLoadTexture(null, width, height, PixelFormat.PIXELFORMAT_UNCOMPRESSED_R32G32B32, 1);
                target.TexPosition.width = width;
                target.TexPosition.height = height;
                target.TexPosition.format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R32G32B32;
                target.TexPosition.mipmaps = 1;

                // Create depth texture
                target.TexDepth.id = Rlgl.rlLoadTextureDepth(width, height, false);
                target.TexDepth.width = width;
                target.TexDepth.height = height;
                target.TexDepth.format = (PixelFormat)19;  // DEPTH_COMPONENT_24BIT
                target.TexDepth.mipmaps = 1;

                // Attach color textures and depth textures to FBO
                Rlgl.rlFramebufferAttach(target.Id, target.TexAlbedo.id, FramebufferAttachType.RL_ATTACHMENT_COLOR_CHANNEL0, FramebufferAttachTextureType.RL_ATTACHMENT_TEXTURE2D, 0);
                Rlgl.rlFramebufferAttach(target.Id, target.TexSpecular.id, FramebufferAttachType.RL_ATTACHMENT_COLOR_CHANNEL1, FramebufferAttachTextureType.RL_ATTACHMENT_TEXTURE2D, 0);
                Rlgl.rlFramebufferAttach(target.Id, target.TexNormal.id, FramebufferAttachType.RL_ATTACHMENT_COLOR_CHANNEL2, FramebufferAttachTextureType.RL_ATTACHMENT_TEXTURE2D, 0);
                Rlgl.rlFramebufferAttach(target.Id, target.TexPosition.id, FramebufferAttachType.RL_ATTACHMENT_COLOR_CHANNEL3, FramebufferAttachTextureType.RL_ATTACHMENT_TEXTURE2D, 0);
                Rlgl.rlFramebufferAttach(target.Id, target.TexDepth.id, FramebufferAttachType.RL_ATTACHMENT_DEPTH, FramebufferAttachTextureType.RL_ATTACHMENT_RENDERBUFFER, 0);

                // Activate required color draw buffers
                Rlgl.rlActiveDrawBuffers(3);

                // Check if fbo is complete with attachments (valid)
                if (Rlgl.rlFramebufferComplete(target.Id)) Console.WriteLine("INFO: FBO: [ID %i] MultiRenderTexture loaded successfully", target.Id);

                Rlgl.rlDisableFramebuffer();
            }
            else Console.WriteLine("Warning: FBO: MultiRenderTexture can not be created");

            return target;
        }

        public void Begin(MultiRenderTexture target)
        {
            Rlgl.rlDrawRenderBatchActive();
            Rlgl.rlEnableFramebuffer(target.Id);

            Rlgl.rlClearColor(127, 127, 127, 255);
            Rlgl.rlClearScreenBuffers();

            Rlgl.rlViewport(0, 0, target.Width, target.Height);

            Rlgl.rlMatrixMode(MatrixMode.PROJECTION);
            Rlgl.rlLoadIdentity();

            Rlgl.rlOrtho(0.0, (double)target.Width, (double)target.Height, 0.0, 0.0, 1.0);

            Rlgl.rlMatrixMode(MatrixMode.MODELVIEW);
            Rlgl.rlLoadIdentity();

            Rlgl.rlDisableColorBlend();
        }

        public void End(MultiRenderTexture target)
        {
            Rlgl.rlDrawRenderBatchActive();
            Rlgl.rlDisableFramebuffer();

            Rlgl.rlViewport(0, 0, target.Width, target.Height);

            Rlgl.rlMatrixMode(MatrixMode.PROJECTION);
            Rlgl.rlLoadIdentity();

            Rlgl.rlOrtho(0.0, (double)target.Width, (double)target.Height, 0.0, 0.0, 1.0);

            Rlgl.rlMatrixMode(MatrixMode.MODELVIEW);
            Rlgl.rlLoadIdentity();

            Rlgl.rlEnableColorBlend();
        }


        // Unload multi render texture from GPU memory (VRAM)
        public static void UnloadRenderTexture(MultiRenderTexture target)
        {
            if (target.Id > 0)
            {
                // Delete color texture attachments
                Rlgl.rlUnloadTexture(target.TexAlbedo.id);
                Rlgl.rlUnloadTexture(target.TexSpecular.id);
                Rlgl.rlUnloadTexture(target.TexNormal.id);
                Rlgl.rlUnloadTexture(target.TexPosition.id);

                // NOTE: Depth texture is automatically queried
                // and deleted before deleting framebuffer
                Rlgl.rlUnloadFramebuffer(target.Id);
            }
        }
    }
}
