using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    public class Renderer : GameWindow
    {
        public const short MaxSimultaneousLights = 16;
        public const short MaxSortableListSize = 1024;
        private readonly Input input;
        private readonly Camera camera;
        private readonly LightManager lightManager;
        private readonly ClosestMeshedObjectComparer meshedObjectDistanceComparer;
        private readonly List<Light> relevantLightsBuffer = new();
        private List<MeshedObject> sceneMeshes;
        private readonly List<MeshedObject> opaqueQueue, transparentQueue;
        private bool loadingScene = true;

        public Renderer(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            loadingScene = true;
            this.RenderFrequency = 120;
            this.UpdateFrequency = 120;
            input = new Input(KeyboardState, MouseState);
            input.OnClose += OnCloseInput;
            CursorState = CursorState.Grabbed;
            camera = new Camera(new Vector3(0, 0, 3), input, (float)width / height);
            lightManager = new LightManager();
            sceneMeshes = new List<MeshedObject>();
            opaqueQueue = new List<MeshedObject>();
            transparentQueue = new List<MeshedObject>();
            meshedObjectDistanceComparer = new ClosestMeshedObjectComparer(camera);
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
            GL.Enable(EnableCap.CullFace);
            GL.DepthMask(true);

            ISceneLoader sceneLoader = new ColladaSceneLoader();
            sceneLoader.LoadScene(Project.Resources + "sample-scene.dae", out sceneMeshes, out List<Light> lights);
            opaqueQueue.Capacity = sceneMeshes.Count / 2;
            transparentQueue.Capacity = sceneMeshes.Count / 4;

            for (int i = 0; i < lights.Count; ++i)
            {
                lightManager.AddLight(lights[i]);
            }

            loadingScene = false;
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            foreach (MeshedObject obj in sceneMeshes)
            {
                obj.Dispose();
            }
            GLResourceCache.DisposeAll();
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

            //TODO: shadowcasting
            //TODO: add normal map support?

            PopulateSortedQueues();

            DrawMeshes(opaqueQueue);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.DepthMask(false);

            DrawMeshes(transparentQueue);

            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);

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

        private void PopulateSortedQueues()
        {
            opaqueQueue.Clear();
            transparentQueue.Clear();

            for (int i = 0; i < sceneMeshes.Count; ++i)
            {
                MeshedObject mesh = sceneMeshes[i];
                if (mesh.IsInsideCameraFrustum(camera))
                {
                    if (mesh.IsTransparent())
                    {
                        transparentQueue.Add(mesh);
                    }
                    else
                    {
                        opaqueQueue.Add(mesh);
                    }
                }
            }

            if (opaqueQueue.Count <= MaxSortableListSize)
            {
                opaqueQueue.Sort(meshedObjectDistanceComparer);
            }
            transparentQueue.Sort((a, b) => meshedObjectDistanceComparer.Compare(b, a));
        }

        private void DrawMeshes(List<MeshedObject> meshedObjects)
        {
            for (short i = 0; i < meshedObjects.Count; ++i)
            {
                MeshedObject mesh = meshedObjects[i];
                lightManager.GetRelevantLightsForObject(mesh, relevantLightsBuffer);
                mesh.Draw(camera, relevantLightsBuffer);
            }
        }
    }
}