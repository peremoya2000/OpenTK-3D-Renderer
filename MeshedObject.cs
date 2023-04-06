using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    class MeshedObject
    {
        public Transform Transform;
        private float meshMaxRadius = 0;
        private float[] vertices;
        private uint[] indices;
        private readonly int vertexBufferObject, elementBufferObject, vertexArrayObject;
        private Shader shader;
        private Material material;

        public MeshedObject(string meshPath) : this(meshPath, new Transform(), new Material())
        {
        }
        public MeshedObject(string meshPath, Transform transform) : this(meshPath, transform, new Material())
        {
        }
        public MeshedObject(string meshPath, Material material) : this(meshPath, new Transform(), material)
        {
        }
        public MeshedObject(string meshPath, Transform transform, Material mat)
        {
            Transform = transform;
            material = mat;

            vertices = ModelImporter.Import(meshPath);
            ModelFormatConverter.SimplifyToIndexFormat(8, ref vertices, out indices);
            UpdateMeshRadius();

            vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(vertexArrayObject);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            elementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            shader = new Shader(Project.Resources + "shader.vert", Project.Resources + "shader.frag");
            shader.Use();

            shader.SetVector3("material.ambientTint", material.AmbientTint);
            shader.SetVector3("material.diffuseTint", material.DiffuseTint);
            shader.SetFloat("material.shininess", material.Shininess);
            var normalLocation = shader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

            int texCoordLocation = shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
        }

        public void SetMaterial(Material mat)
        {
            material = mat;
        }

        public float GetMeshRadius()
        {
            return meshMaxRadius * Transform.Scale;
        }

        public void Dispose()
        {
            shader.Dispose();
        }

        public void Draw(Camera camera, List<Light> lights)
        {
            UpdateLightsData(lights);

            shader.Use();

            Matrix4 model = Transform.GetModelMatrix();
            shader.SetMatrix4("model", model);
            Matrix3 normalRot = new Matrix3(Matrix4.Transpose(model.Inverted()));
            shader.SetMatrix3("normalRot", normalRot);

            shader.SetMatrix4("view", camera.GetViewMatrix());
            shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            shader.SetVector3("viewPos", camera.Position);
            GL.BindVertexArray(vertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
        }

        private void UpdateLightsData(List<Light> lights)
        {
            for (byte i = 0; i < lights.Count; ++i)
            {
                string lightUniform = "lights[" + i + "]";

                switch (lights[i])
                {
                    case DirectionalLight directional:
                        shader.SetVector4(lightUniform + ".vector", directional.InternalVector);
                        shader.SetVector3(lightUniform + ".color", directional.Color);
                        shader.SetFloat(lightUniform + ".intensity", directional.Intensity);
                        break;

                    case PointLight point:
                        shader.SetVector4(lightUniform + ".vector", point.InternalVector);
                        shader.SetVector3(lightUniform + ".color", point.Color);
                        shader.SetFloat(lightUniform + ".intensity", point.Intensity);
                        shader.SetFloat(lightUniform + ".radius", point.Radius);
                        break;
                }
            }
            shader.SetFloat("lightCount", lights.Count);
        }

        private void UpdateMeshRadius()
        {
            Vector3 maxVert = Vector3.Zero;
            Vector3 tVert;
            float maxLengthSquared = 0;
            for (int i = 0; i < vertices.Length; i += 3)
            {
                tVert = new Vector3(vertices[i], vertices[i + 1], vertices[i + 2]);
                if (tVert.LengthSquared > maxLengthSquared)
                {
                    maxLengthSquared = tVert.LengthSquared;
                    maxVert = tVert;
                }
            }
            meshMaxRadius = maxVert.Length;
        }
    }
}
