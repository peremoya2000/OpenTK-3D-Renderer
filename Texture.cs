using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenTK_3D_Renderer
{
    public class Texture : IDisposable
    {
        private readonly int handle;
        private readonly ImageResult image;
        private readonly string pathToFile;
        public MaterialType MaterialType { get; private set; }

        public Texture(string projectFilePath)
        {
            pathToFile = projectFilePath;
            handle = GL.GenTexture();
            Use();

            image = LoadImage();
            MaterialType = GetMaterialType();
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                image.Width, image.Height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            GL.GenerateTextureMipmap(handle);
        }

        public void Use()
        {
            RendererState.BindTextureToSlot(handle);
        }

        private ImageResult LoadImage()
        {
            // stb_image loads from the top-left pixel, whereas OpenGL loads from the bottom-left, causing the texture to be flipped vertically.
            // This will correct that, making the texture display properly.
            StbImage.stbi_set_flip_vertically_on_load(1);

            try
            {
                using FileStream stream = File.OpenRead(pathToFile);

                return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            }
            catch (Exception e) when (
                e is FileNotFoundException ||
                e is DirectoryNotFoundException ||
                e is UnauthorizedAccessException ||
                e is IOException)
            {
                throw new IOException($"Failed to load texture file: {pathToFile}", e);
            }
        }

        public MaterialType GetMaterialType()
        {
            const byte opaqueThreshold = byte.MaxValue * 9 / 10;
            const byte transparentThreshold = byte.MaxValue * 1 / 10;
            const float minRatioForOpaque = 0.9f;
            const float stdDevToleranceRatio = 0.25f;

            uint opaquePixels = 0;
            List<byte> semitransparentAlphas = new();

            for (int i = 0; i < image.Data.Length; i += 4)
            {
                byte a = image.Data[i + 3];

                if (a > opaqueThreshold)
                {
                    opaquePixels++;
                }
                else if (a > transparentThreshold)
                { 
                    semitransparentAlphas.Add(a);
                }
            }

            if (opaquePixels >= image.Width * image.Height * minRatioForOpaque)
            {
                return MaterialType.Opaque;
            }

            if (semitransparentAlphas.Count > 0)
            {
                float stdDev = CalculateStdDev(semitransparentAlphas);
                float tolerance = byte.MaxValue * stdDevToleranceRatio;

                if (stdDev < tolerance)
                {
                    return MaterialType.Transparent;
                }
            }

            return MaterialType.Cutout;
        }

        public float CalculateAverageOpacity()
        {
            float opacity = 0;

            for (int i = 0; i < image.Data.Length; i += 4)
            {
                opacity += image.Data[i + 3];
            }

            return opacity / (image.Width * image.Height * byte.MaxValue);
        }

        public void Dispose()
        {
            RendererState.BindTextureToSlot(0);
            GL.DeleteTexture(handle);

            GC.SuppressFinalize(this);
        }

        public bool Equals(Texture other)
        {
            return pathToFile == other.pathToFile && image.Data.Length == other.image.Data.Length;
        }

        private static float CalculateStdDev(List<byte> alphaValues)
        {
            float mean = alphaValues.Aggregate(0f, (acc, v) => acc + v) / alphaValues.Count;
            float sumSquaredDiff = alphaValues.Aggregate(0f, (acc, v) => acc + (v - mean) * (v - mean));
            return MathF.Sqrt(sumSquaredDiff / alphaValues.Count);
        }
    }
}
