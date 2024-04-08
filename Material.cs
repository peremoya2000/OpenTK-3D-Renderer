using OpenTK.Mathematics;

namespace OpenTK_3D_Renderer
{
    public class Material
    {
        public Texture MainTexture;
        public Vector3 AmbientTint;
        public Vector3 DiffuseTint;
        public float Shininess;

        public Material()
        {
            MainTexture = new Texture(Project.Resources + "crateTex.png");
            AmbientTint = Vector3.One;
            DiffuseTint = Vector3.One;
            Shininess = 32;
        }
        public Material(string texturePath, float shininess = 32)
        {
            MainTexture = new Texture(texturePath);
            AmbientTint = Vector3.One;
            DiffuseTint = Vector3.One;
            Shininess = shininess;
        }
        public Material(string texturePath, Vector3 ambientTint, Vector3 diffuseTint, float shininess = 32)
        {
            MainTexture = new Texture(texturePath);
            AmbientTint = ambientTint;
            DiffuseTint = diffuseTint;
            Shininess = shininess;
        }
        public Material(Texture texture, Vector3 ambientTint, Vector3 diffuseTint, float shininess = 32)
        {
            MainTexture = texture;
            AmbientTint = ambientTint;
            DiffuseTint = diffuseTint;
            Shininess = shininess;
        }

        public Material GetCopy()
        {
            return new Material(MainTexture, AmbientTint, DiffuseTint, Shininess);
        }
    }
}
