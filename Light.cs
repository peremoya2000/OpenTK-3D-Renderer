using OpenTK.Mathematics;

namespace OpenTK_3D_Renderer
{
    public enum LightType
    {
        DirectionalLight,
        PointLight
    }

    public abstract class Light
    {
        public LightType Type { get; protected set; }
        public Vector3 Color;
        public float Intensity;
        public Vector4 InternalVector { get; protected set; }
    }
}
