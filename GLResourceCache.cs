using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    public static class GLResourceCache
    {
        private static readonly Dictionary<(string, string), Shader> shaderCache = new();
        private static readonly Dictionary<string, Texture> textureCache = new();

        public static Texture AddOrGetTexture(string texturePath)
        {
            if (textureCache.TryGetValue(texturePath, out Texture result))
            {
                return result;
            }

            result = new Texture(texturePath);
            textureCache.Add(texturePath, result);

            return result;
        }

        public static Shader AddOrGetShader(string vertexPath, string fragmentPath)
        {
            var key = (vertexPath, fragmentPath);
            if (shaderCache.TryGetValue(key, out Shader result))
            {
                return result;
            }
            result = new Shader(vertexPath, fragmentPath);
            shaderCache.Add(key, result);
            return result;
        }

        public static void DisposeAll()
        {
            foreach (var shader in shaderCache.Values)
            {
                shader.Dispose();
            }
            shaderCache.Clear();

            foreach (var texture in textureCache.Values)
            {
                texture.Dispose();
            }
            textureCache.Clear();
        }
    }
}
