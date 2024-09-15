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
        public const short MaxSimultaneousLights = 16;
        private readonly Input input;
        private readonly Camera camera;
        private readonly LightManager lightManager;
        private List<MeshedObject> renderedMeshes;
        private bool loadingScene = true;

        public Renderer(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            loadingScene = true;
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
            loadingScene = true;

            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            ISceneLoader sceneLoader = new ColladaSceneLoader();
            sceneLoader.LoadScene(Project.Resources + "sample-scene.dae", out renderedMeshes, out List<Light> lights);

            for (int i = 0; i < lights.Count; ++i)
            {
                lightManager.AddLight(lights[i]);
            }

            loadingScene = false;
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

            if (loadingScene)
            {
                return;
            }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            //TODO: transparent materials
            //TODO: shadowcasting
            //TODO: add normal map support?

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

            input.Update();
            camera.Update(deltaTime);
        }
    }
}