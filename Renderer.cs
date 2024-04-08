using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    public class Renderer : GameWindow
    {
        public readonly static short MaxSimultaneousLights = 16;
        private readonly Input input;
        private readonly Camera camera;
        private readonly LightManager lightManager;
        private readonly List<MeshedObject> renderedMeshes;

        public Renderer(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            this.RenderFrequency = 120;
            this.UpdateFrequency = 120;
            input = new Input(KeyboardState, MouseState);
            input.OnClose += OnCloseInput;
            CursorState = CursorState.Grabbed;
            camera = new Camera(new Vector3(0, 0, 3), input, width / height);
            lightManager = new LightManager();
            renderedMeshes = new List<MeshedObject>();
        }

        private void OnCloseInput()
        {
            if (input != null)
            {
                input.OnClose -= OnCloseInput;
            }
            Close();
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            lightManager.AddLight(new PointLight(new Vector3(0, -5, 0), Vector3.UnitZ, 30, 5));
            lightManager.AddLight(new DirectionalLight(Vector3.UnitZ, Vector3.UnitX));

            Transform t;
            MeshedObject mesh = null;
            for (int i = -10; i <= 10; ++i)
            {
                for (int j = -10; j <= 10; ++j)
                {
                    for (int k = 0; k < 5; ++k)
                    {
                        t = new Transform
                        {
                            Position = new Vector3(i * 5f, j * 5f, -k * 5f)
                        };
                        if (mesh == null)
                        {
                            mesh = new MeshedObject(Project.Resources + "monkey.obj", t);
                            renderedMeshes.Add(mesh);
                        }
                        else
                        {
                            renderedMeshes.Add(new MeshedObject(mesh, t));
                        }
                        
                    }
                }
            }
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            foreach (MeshedObject obj in renderedMeshes)
            {
                obj.Dispose();
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
            camera.SetAspectRatio(Size.X / (float)Size.Y);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            //TODO: add normal map support
            //TODO: shadowcasting?
            //TODO: transparent materials?

            for (short i = 0; i < renderedMeshes.Count; ++i)
            {
                MeshedObject mesh = renderedMeshes[i];
                if (mesh.IsInsideCameraFrustum(camera))
                {
                    var lights = lightManager.GetRelevantLightsForObject(mesh);
                    mesh.Draw(camera, lights);
                }
            }

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (!IsFocused)
            {
                return;
            }

            float deltaTime = (float)e.Time;
            foreach (MeshedObject mesh in renderedMeshes)
            {
                mesh.Transform.AddRotation(Quaternion.FromAxisAngle(Vector3.UnitY, deltaTime));
            }
            input.Update();
            camera.Update(deltaTime);
        }
    }
}