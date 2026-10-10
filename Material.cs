using OpenTK.Mathematics;

namespace OpenTK_3D_Renderer
{
    public enum AlphaMode { Opaque, Cutout, Transparent }

    public class Material
    {
        public Texture MainTexture { get; private set; }
        public Vector3 AmbientTint;
        public Vector4 DiffuseTint;
        public float Shininess;
        public float Opacity;
        public AlphaMode AlphaMode => MainTexture.AlphaMode;

        public Material()
        {
            MainTexture = GLResourceCache.AddOrGetTexture(Project.Resources + Project.DefaultTex);
            AmbientTint = Vector3.One;
            DiffuseTint = Vector4.One;
            Shininess = 32;
            SetOpacityByAlphaMode();
        }
        public Material(string texturePath, float shininess = 32)
        {
            MainTexture = GLResourceCache.AddOrGetTexture(texturePath);
            AmbientTint = Vector3.One;
            DiffuseTint = Vector4.One;
            Shininess = shininess;
            SetOpacityByAlphaMode();
        }
        public Material(string texturePath, Vector3 ambientTint, Vector4 diffuseTint, float shininess = 32)
        {
            MainTexture = GLResourceCache.AddOrGetTexture(texturePath);
            AmbientTint = ambientTint;
            DiffuseTint = diffuseTint;
            Shininess = shininess;
            SetOpacityByAlphaMode();
        }
        public Material(Texture texture, Vector3 ambientTint, Vector4 diffuseTint, float shininess = 32)
        {
            MainTexture = texture;
            AmbientTint = ambientTint;
            DiffuseTint = diffuseTint;
            Shininess = shininess;
            SetOpacityByAlphaMode();
        }

        public Material GetCopy()
        {
            return new Material(MainTexture, AmbientTint, DiffuseTint, Shininess);
        }

        private void SetOpacityByAlphaMode()
        {
            Opacity = MainTexture.AlphaMode switch
            {
                AlphaMode.Transparent => MainTexture.CalculateAverageOpacity(),
                _ => 1f,
            };
        }
    }
}
