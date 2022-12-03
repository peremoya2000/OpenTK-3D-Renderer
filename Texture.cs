using System.Collections.Generic;
using System.Text;
using System;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using System.IO;

namespace OpenTK_3D_Renderer
{
    class Texture
    {
        private int Handle;
        private ImageResult image;
        private string pathToFile;
        public Texture(string projectFilePath)
        {
            pathToFile = projectFilePath;
            Handle = GL.GenTexture();
            Bind();
            image = LoadImage();
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            GL.GenerateTextureMipmap(Handle);
        }

        private void Bind()
        {
            GL.BindTexture(TextureTarget.Texture2D, Handle);
        }

        private ImageResult LoadImage()
        {
            // stb_image loads from the top-left pixel, whereas OpenGL loads from the bottom-left, causing the texture to be flipped vertically.
            // This will correct that, making the texture display properly.
            StbImage.stbi_set_flip_vertically_on_load(1);

            // Load the image.
            return ImageResult.FromStream(File.OpenRead(pathToFile), ColorComponents.RedGreenBlueAlpha);
        }

    }
}
