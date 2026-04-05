using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using System;
using System.IO;

namespace OpenTK_3D_Renderer
{
    public class Texture : IEquatable<Texture>, IDisposable
    {
        private readonly int handle;
        private readonly ImageResult image;
        private readonly string pathToFile;
        public Texture(string projectFilePath)
        {
            pathToFile = projectFilePath;
            handle = GL.GenTexture();
            Use();
            image = LoadImage();
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            GL.GenerateTextureMipmap(handle);
        }
        public Texture(string projectFilePath, ImageResult image)
        {
            pathToFile = projectFilePath;
            handle = GL.GenTexture();
            Use();
            this.image = image;
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            GL.GenerateTextureMipmap(handle);
        }

        public void Use()
        {
            RendererState.BindTextureToSlot(handle);
        }

        public Texture GetCopy()
        {
            return new Texture(pathToFile, image);
        }

        private ImageResult LoadImage()
        {
            // stb_image loads from the top-left pixel, whereas OpenGL loads from the bottom-left, causing the texture to be flipped vertically.
            // This will correct that, making the texture display properly.
            StbImage.stbi_set_flip_vertically_on_load(1);

            // Load the image.
            using FileStream stream = File.OpenRead(pathToFile);
            return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }

        public void Dispose()
        {
            GL.DeleteTexture(handle);
        }

        public bool Equals(Texture other)
        {
            return pathToFile == other.pathToFile && image.Data.Length == other.image.Data.Length;
        }
    }
}
