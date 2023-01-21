using OpenTK.Mathematics;
using System;

namespace OpenTK_3D_Renderer
{

    public class Camera
    {
        private float movementSpeed = 0;
        private const float movementAcceleration = .04f;
        private const float maxMovementSpeed = 2.5f;
        private const float lookSpeed = 0.2f;

        private Vector3 front = -Vector3.UnitZ;
        private Vector3 up = Vector3.UnitY;
        private Vector3 right = Vector3.UnitX;

        // Rotation around the Y axis (radians)
        private float pitchRad = 0;
        // Rotation around the X axis (radians)
        private float yawRad = MathHelper.PiOver2;
        // The field of view of the camera (radians)
        private float fovRad = MathHelper.PiOver2;
        private bool freeLook = true;

        private readonly Input input;

        public Camera(Vector3 position, float aspectRatio, Input inp)
        {
            Position = position;
            AspectRatio = aspectRatio;
            input = inp;
            input.OnResetCamera += ResetCamera;
        }

        public Vector3 Position { get; private set; }

        public float AspectRatio { private get; set; }

        public Vector3 Front => front;
        public Vector3 Up => up;
        public Vector3 Right => right;

        private float pitch
        {
            get => MathHelper.RadiansToDegrees(pitchRad);
            set
            {
                //Clamp to prevent camera from going upside down
                var angle = MathHelper.Clamp(value, -89f, 89f);
                pitchRad = MathHelper.DegreesToRadians(angle);
                UpdateVectors();
            }
        }
        private float yaw
        {
            get => MathHelper.RadiansToDegrees(yawRad);
            set
            {
                yawRad = MathHelper.DegreesToRadians(value);
                UpdateVectors();
            }
        }

        public void Update(float deltaTime)
        {
            Vector3 movement = input.CameraMovement;

            if (movement.Equals(Vector3.Zero))
            {
                movementSpeed = 0;
            }
            else
            {
                movementSpeed += movementAcceleration;
                movementSpeed = MathF.Min(movementSpeed, maxMovementSpeed);
            }
            movement *= movementSpeed;

            freeLook = input.FreeLookActive;
            if (!freeLook)
            {
                movement = ClampSphereMovement(movement);
            }

            Position += front * movement.Z * deltaTime;
            Position += up * movement.Y * deltaTime;
            Position += right * movement.X * deltaTime;

            if (freeLook)
            {
                yaw += input.CameraLookInput.X * lookSpeed;
                pitch += input.CameraLookInput.Y * lookSpeed;
            }
            else
            {
                UpdateVectors();
            }
        }

        private Vector3 ClampSphereMovement(Vector3 movement)
        {
            if (MathF.Abs(front.Y) >= .95f)
            {
                if (Vector3.Dot(movement, front) < 0)
                {
                    movement *= (1, 0, 1);
                }
            }

            return movement;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + front, up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(fovRad, AspectRatio, 0.01f, 100f);
        }

        private void UpdateVectors()
        {
            if (freeLook)
            {
                front.X = MathF.Cos(pitchRad) * MathF.Cos(yawRad);
                front.Y = MathF.Sin(pitchRad);
                front.Z = MathF.Cos(pitchRad) * MathF.Sin(yawRad);
            }
            else
            {
                front = -Position;
            }

            front = Vector3.Normalize(front);
            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }

        private void ResetCamera()
        {
            Position = (0, 0, -3);
            yawRad = MathHelper.PiOver2;
            pitch = 0;
        }
    }
}