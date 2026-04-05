using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    public static class RendererState
    {
        private static int usedShader = -1;
        private static int boundVertexObjectArray = -1;
        private static readonly Dictionary<TextureUnit, int> boundTextures = new(32);

        static RendererState()
        {
            TextureUnit[] units = Enum.GetValues<TextureUnit>();
            for (int i = 0; i < units.Length; ++i)
            {
                boundTextures.Add(units[i], -1);
            }
        }

        public static void UseShaderProgram(int shaderHandle)
        {
            if (shaderHandle != usedShader)
            {
                GL.UseProgram(shaderHandle);
                usedShader = shaderHandle;
            }
        }

        public static void BindVertexObjectArray(int vaoHandle)
        {
            if (vaoHandle != boundVertexObjectArray)
            {
                GL.BindVertexArray(vaoHandle);
                boundVertexObjectArray = vaoHandle;
            }
        }

        public static void BindTextureToSlot(int textureHandle, TextureUnit slot = TextureUnit.Texture0)
        {
            if (textureHandle != boundTextures[slot])
            {
                GL.ActiveTexture(slot);
                GL.BindTexture(TextureTarget.Texture2D, textureHandle);
                boundTextures[slot] = textureHandle;
            }
        }
    }
}
