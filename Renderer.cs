using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    public class Renderer : GameWindow
    {
        private float[] vertices =
        {
            // Positions          Normals              Texture coords
            -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  0.0f, 0.0f,

            -0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  0.0f, 0.0f,

            -0.5f,  0.5f,  0.5f, -1.0f,  0.0f,  0.0f,  1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f, -1.0f,  0.0f,  0.0f,  1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f, -1.0f,  0.0f,  0.0f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f, -1.0f,  0.0f,  0.0f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f, -1.0f,  0.0f,  0.0f,  0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f, -1.0f,  0.0f,  0.0f,  1.0f, 0.0f,

             0.5f,  0.5f,  0.5f,  1.0f,  0.0f,  0.0f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  1.0f,  0.0f,  0.0f,  1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  1.0f,  0.0f,  0.0f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  1.0f,  0.0f,  0.0f,  0.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  1.0f,  0.0f,  0.0f,  0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f,  0.0f,  0.0f,  1.0f, 0.0f,

            -0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,  1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,  1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,  1.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,  0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,  0.0f, 1.0f,

            -0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,  0.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,  1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,  0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,  0.0f, 1.0f
        };
        private List<Light> lights = new List<Light>();

        //TODO: Go back to using elementBufferObjects
        private int vertexBufferObject, elementBufferObject;
        private int vertexArrayObject;
        private Shader mainShader;
        private Texture mainTex;
        private Input input;
        private Camera camera;

        public Renderer(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title })
        {
            this.RenderFrequency = 120;
            this.UpdateFrequency = 120;
            input = new Input(KeyboardState, MouseState);
            input.OnClose += OnCloseInput;
            CursorState = CursorState.Grabbed;
            camera = new Camera(new Vector3(0,0,-3), width/height, input);
        }

        private void OnCloseInput()
        {
            Close();
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);


            vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);


            vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(vertexArrayObject);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            mainShader = new Shader(Project.Resources + "shader.vert", Project.Resources + "shader.frag");
            mainShader.Use();

            AddLight(new DirectionalLight(-Vector3.UnitZ, Vector3.UnitX));
            AddLight(new PointLight(new Vector3(0, -5, 0), Vector3.UnitZ, 50));
   
            mainShader.SetVector3("material.ambientTint", Vector3.One);
            mainShader.SetVector3("material.diffuseTint", Vector3.One);
            mainShader.SetFloat("material.shininess", 32.0f);
            var normalLocation = mainShader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

            int texCoordLocation = mainShader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
            mainTex = new Texture(Project.Resources + "crateTex.png");
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            mainShader.Dispose();
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
            camera.AspectRatio = Size.X / (float)Size.Y;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            mainShader.Use();
            var now = DateTime.UtcNow;
            float time = 60*now.Minute+now.Second+(float)(now.Millisecond) /1000;
            Matrix4 model = Matrix4.CreateRotationX((float)MathHelper.DegreesToRadians(time*30));
            mainShader.SetMatrix4("model", model);

            Matrix3 normalRot = new Matrix3(Matrix4.Transpose(model.Inverted()));
            mainShader.SetMatrix3("normalRot", normalRot);
            mainShader.SetMatrix4("view", camera.GetViewMatrix());
            mainShader.SetMatrix4("projection", camera.GetProjectionMatrix());
            mainShader.SetVector3("viewPos", camera.Position);
            GL.BindVertexArray(vertexArrayObject);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

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

        private void AddLight(Light l)
        {
            lights.Add(l);
            UpdateLights();
        }

        private void RemoveLight(Light l)
        {
            lights.Remove(l);
            UpdateLights();
        }

        private void ClearLights()
        {
            lights.Clear();
            UpdateLights();
        }

        private void UpdateLights()
        {
            for (byte i = 0; i < lights.Count; ++i)
            {
                string lightUniform = "lights[" + i + "]";

                switch (lights[i])
                {
                    case DirectionalLight directional:
                        mainShader.SetVector4(lightUniform + ".vector", directional.InternalVector);
                        mainShader.SetVector3(lightUniform + ".color", directional.Color);
                        mainShader.SetFloat(lightUniform + ".intensity", directional.Intensity);
                        break;

                    case PointLight point:
                        mainShader.SetVector4(lightUniform + ".vector", point.InternalVector);
                        mainShader.SetVector3(lightUniform + ".color", point.Color);
                        mainShader.SetFloat(lightUniform + ".intensity", point.Intensity);
                        mainShader.SetFloat(lightUniform + ".radius", point.Radius);
                        break;
                }
            }
            mainShader.SetFloat("lightCount", lights.Count);
        }
    }
}