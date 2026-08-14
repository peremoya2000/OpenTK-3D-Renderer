using OpenTK.Mathematics;

namespace OpenTK_3D_Renderer
{
    public enum MaterialType { Opaque, Cutout, Transparent }

    public class Material
    {
        public Texture MainTexture;
        public Vector3 AmbientTint;
        public Vector4 DiffuseTint;
        public float Shininess;
        public float Opacity;
        public MaterialType Type;

        public Material()
        {
            MainTexture = GLResourceCache.AddOrGetTexture(Project.Resources + Project.DefaultTex);
            Type = MainTexture.MaterialType;
            AmbientTint = Vector3.One;
            DiffuseTint = Vector4.One;
            Shininess = 32;
            SetOpacityByMaterialType();
        }
        public Material(string texturePath, float shininess = 32)
        {
            MainTexture = GLResourceCache.AddOrGetTexture(texturePath);
            Type = MainTexture.MaterialType;
            AmbientTint = Vector3.One;
            DiffuseTint = Vector4.One;
            Shininess = shininess;
            SetOpacityByMaterialType();
        }
        public Material(string texturePath, Vector3 ambientTint, Vector4 diffuseTint, float shininess = 32)
        {
            MainTexture = GLResourceCache.AddOrGetTexture(texturePath);
            Type = MainTexture.MaterialType;
            AmbientTint = ambientTint;
            DiffuseTint = diffuseTint;
            Shininess = shininess;
            SetOpacityByMaterialType();
        }
        public Material(Texture texture, Vector3 ambientTint, Vector4 diffuseTint, float shininess = 32)
        {
            MainTexture = texture;
            Type = MainTexture.MaterialType;
            AmbientTint = ambientTint;
            DiffuseTint = diffuseTint;
            Shininess = shininess;
            SetOpacityByMaterialType();
        }

        public Material GetCopy()
        {
            return new Material(MainTexture, AmbientTint, DiffuseTint, Shininess);
        }

        private void SetOpacityByMaterialType()
        {
            Opacity = Type switch
            {
                MaterialType.Transparent => MainTexture.CalculateAverageOpacity(),
                _ => 1f,
            };
        }
    }
}
