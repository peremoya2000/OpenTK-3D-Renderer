using OpenTK.Mathematics;

namespace OpenTK_3D_Renderer
{
    public abstract class Light
    {
        public Vector3 Color;
        public float Intensity;
        public Vector4 InternalVector { get; protected set; }
    }
}
