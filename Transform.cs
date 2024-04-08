using OpenTK.Mathematics;

namespace OpenTK_3D_Renderer
{
    class Transform
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Scale;

        public Transform()
        {
            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
            Scale = 1;
        }
        public Transform(Vector3 position, float scale = 1)
        {
            Position = position;
            Rotation = Quaternion.Identity;
            Scale = scale;
        }
        public Transform(Vector3 position, Quaternion rotation, float scale = 1)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public void AddRotation(Quaternion q)
        {
            Rotation *= q;
        }

        public Matrix4 GetModelMatrix()
        { 
            Matrix4 scaling = Matrix4.CreateScale(Scale);
            Matrix4.CreateFromQuaternion(Rotation, out Matrix4 rotation);
            Matrix4.CreateTranslation(Position, out Matrix4 translation);
            return scaling*rotation*translation;
        }

        public Transform GetCopy()
        {
            return new Transform(Position, Rotation, Scale);
        }

    }
}
