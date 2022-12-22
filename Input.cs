using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace OpenTK_3D_Renderer
{
    public class Input
    {
        public Vector3 CameraMovement { get; private set; }
        public Vector2 CameraLookInput { get; private set; }

        public delegate void CloseEventHandler();
        public event CloseEventHandler OnClose;

        private KeyboardState keyboard;
        private MouseState mouse;
        private Vector2 lastMousePos;
        public Input(KeyboardState keyboard, MouseState mouse)
        {
            this.keyboard = keyboard;
            this.mouse = mouse;
            lastMousePos = (mouse.X, mouse.Y);
        }

        public void Update()
        {
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                OnClose?.Invoke();
            }

            UpdateMovement();
            UpdateLook();
        }

        private void UpdateMovement()
        {
            CameraMovement = Vector3.Zero;

            if (keyboard.IsKeyDown(Keys.W))
            {
                CameraMovement += Vector3.UnitZ; // Forward
            }
            if (keyboard.IsKeyDown(Keys.S))
            {
                CameraMovement -= Vector3.UnitZ; // Backwards
            }

            if (keyboard.IsKeyDown(Keys.D))
            {
                CameraMovement += Vector3.UnitX; // Right
            }
            if (keyboard.IsKeyDown(Keys.A))
            {
                CameraMovement -= Vector3.UnitX; // Left
            }

            if (keyboard.IsKeyDown(Keys.E))
            {
                CameraMovement += Vector3.UnitY; // Up
            }
            if (keyboard.IsKeyDown(Keys.Q))
            {
                CameraMovement -= Vector3.UnitY; // Down
            }

            CameraMovement.Normalize();
        }

        private void UpdateLook()
        {
            float deltaX = mouse.X - lastMousePos.X;
            float deltaY = mouse.Y - lastMousePos.Y;
            lastMousePos = (mouse.X, mouse.Y);

            CameraLookInput = (CameraLookInput + (deltaX, -deltaY)) / 2;
        }
    }
}
