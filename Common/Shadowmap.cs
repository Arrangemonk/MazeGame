/*******************************************************************************************
*
*   raylib [shaders] example - shadowmap rendering
*
*   Example complexity rating: [★★★★] 4/4
*
*   Example originally created with raylib 5.0, last time updated with raylib 5.0
*
*   Example contributed by TheManTheMythTheGameDev (@TheManTheMythTheGameDev) and reviewed by Ramon Santamaria (@raysan5)
*
*   Example licensed under an unmodified zlib/libpng license, which is an OSI-certified,
*   BSD-like license that allows static linking with closed source software
*
*   Copyright (c) 2023-2025 TheManTheMythTheGameDev (@TheManTheMythTheGameDev)
*
********************************************************************************************/
using Raylib_CSharp.Images;
using Raylib_CSharp.Rendering.Gl.FrameBuffer;
using Raylib_CSharp.Textures;
using Raylib_CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mazeGame.Common
{
    public class Shadowmap
    {
        // Load render texture for shadowmap projection
        // NOTE: Load framebuffer with only a texture depth attachment, 
        // no color attachment required for shadowmap
        public static RenderTexture2D LoadShadowmapRenderTexture(int width, int height)
        {
            RenderTexture2D target = new RenderTexture2D();

            target.Id = RlGl.LoadFramebuffer(); // Load an empty framebuffer
            target.Texture.Width = width;
            target.Texture.Height = height;
            //RlGl.TextureParameters(target.Texture.Id, 0x2803, 0x812D);

            //float borderColor[] = { 1.0f, 1.0f, 1.0f, 1.0f };
            //glTexParameterfv(GL_TEXTURE_2D, GL_TEXTURE_BORDER_COLOR, borderColor);

            if (target.Id > 0)
            {
                RlGl.EnableFramebuffer(target.Id);

                // Create depth texture
                // NOTE: No need a color texture attachment for the shadowmap
                target.Depth.Id = RlGl.LoadTextureDepth(width, height, false);
                target.Depth.Width = width;
                target.Depth.Height = height;
                target.Depth.Format = PixelFormat.UncompressedR32; // DEPTH_COMPONENT_24BIT?
                target.Depth.Mipmaps = 1;
                //RlGl.TextureParameters(target.Depth.Id, 0x2803, 0x812D);

                // Attach depth texture to FBO
                RlGl.FramebufferAttach(target.Id, target.Depth.Id, FramebufferAttachType.Depth, FramebufferAttachTextureType.Texture2D, 0);

                // Check if fbo is complete with attachments (valid)
                if (RlGl.FramebufferComplete(target.Id)) Console.WriteLine("FBO: ID {0} Framebuffer object created successfully", target.Id);

                RlGl.DisableFramebuffer();
            }
            else Console.WriteLine("FBO: Framebuffer object can not be created");

            return target;
        }

        // Unload shadowmap render texture from GPU memory (VRAM)
        public static void UnloadShadowmapRenderTexture(RenderTexture2D target)
        {
            if (target.Id > 0)
            {
                // NOTE: Depth texture/renderbuffer is automatically
                // queried and deleted before deleting framebuffer
                RlGl.UnloadFramebuffer(target.Id);
            }
        }
    }
}
