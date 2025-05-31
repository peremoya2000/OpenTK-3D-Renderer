using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    public class MeshedObject
    {
        public const int VERTEX_SIZE = 8;

        public Transform MeshTransform;
        public float[] Vertices => vertices;
        public uint[] Indices => indices;
        private float meshMaxRadius = 0;
        private readonly float[] vertices;
        private readonly uint[] indices;
        private int vertexBufferObject, elementBufferObject, vertexArrayObject;
        private readonly float cullingMargin = 3.0f / MathF.Sqrt(3);
        private Shader shader;
        private Material material;

        public MeshedObject(Transform transform, float[] uncompressedVertexBuffer, Material mat)
        {
            MeshTransform = transform;
            material = mat;
            vertices = uncompressedVertexBuffer;
            ModelFormatConverter.SimplifyToIndexFormat(VERTEX_SIZE, ref vertices, out indices);
            UpdateMeshRadius();

            InitializeGlBuffers();
            InitializeShader();
        }

        public MeshedObject(MeshedObject meshToCopy, Transform transformOverride = null, Material materialOverride = null)
        {
            if (transformOverride == null)
            {
                transformOverride = meshToCopy.MeshTransform.GetCopy();
            }
            MeshTransform = transformOverride;

            if (materialOverride == null)
            {
                materialOverride = meshToCopy.GetMaterial().GetCopy();
            }
            material = materialOverride;

            vertices = meshToCopy.Vertices;
            indices = meshToCopy.Indices;
            UpdateMeshRadius();

            InitializeGlBuffers();
            InitializeShader();
        }

        public void SetMaterial(Material mat)
        {
            material = mat;
        }

        public Material GetMaterial()
        {
            return material;
        }

        public float GetMeshRadius()
        {
            return meshMaxRadius * MeshTransform.Scale;
        }

        public void Dispose()
        {
            shader.Dispose();
        }

        //CombinedMethod
        public bool IsInsideCameraFrustum(Camera camera)
        {
            //First pass of culling based on distance & dot product to handle meshes you are inside of or behind you
            Vector3 cameraToMesh = MeshTransform.Position - camera.Position;
            float meshRadius = GetMeshRadius();
            if (cameraToMesh.LengthSquared <= meshRadius * meshRadius)
            {
                return true;
            }
            Vector3 centerPoint = camera.Front * MathF.Abs(Vector3.Dot(cameraToMesh, camera.Front));
            cameraToMesh += (Vector3.NormalizeFast(centerPoint - cameraToMesh) * meshRadius);
            float meshDotValue = Vector3.Dot(Vector3.NormalizeFast(cameraToMesh), camera.Front);
            if (meshDotValue < camera.GetVisibilityLimit())
            {
                return false;
            }

            //Second pass based on space transformations
            Matrix4 viewProjection = camera.GetViewMatrix() * camera.GetProjectionMatrix();
            Vector4 clipSpacePos = new Vector4(MeshTransform.Position, 1) * viewProjection;
            clipSpacePos /= clipSpacePos.W;
            if (clipSpacePos.X > -1 && clipSpacePos.X < 1 && clipSpacePos.Y > -1 && clipSpacePos.Y < 1)
            {
                return true;
            }

            Vector4 worldSpaceFrustumEdge = new Vector4(MathHelper.Clamp(clipSpacePos.X, -1f, 1f),
                                                        MathHelper.Clamp(clipSpacePos.Y, -1f, 1f),
                                                        clipSpacePos.Z, 1);

            worldSpaceFrustumEdge *= viewProjection.Inverted();
            worldSpaceFrustumEdge.Xyz /= worldSpaceFrustumEdge.W;

            return (MeshTransform.Position - worldSpaceFrustumEdge.Xyz).LengthFast <= meshRadius * cullingMargin;
        }

        public void Draw(Camera camera, List<Light> lights)
        {
            UpdateLightsData(lights);

            shader.Use();
            UpdateModelData();
            UpdateCameraData(camera);

            GL.BindVertexArray(vertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);
        }

        public float SqrDistanceToCamera(Vector3 cameraPos)
        {
            return MathF.Max(0, (MeshTransform.Position - cameraPos).LengthSquared - GetMeshRadius() * GetMeshRadius());
        }

        private void InitializeGlBuffers()
        {
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
        }

        private void InitializeShader()
        {
            shader = new Shader(Project.Resources + "shader.vert", Project.Resources + "shader.frag");
            shader.Use();

            shader.SetVector3("material.ambientTint", material.AmbientTint);
            shader.SetVector3("material.diffuseTint", material.DiffuseTint.Xyz);
            shader.SetFloat("material.shininess", material.Shininess);
            var normalLocation = shader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

            int texCoordLocation = shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
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
        private void UpdateModelData()
        {
            Matrix4 model = MeshTransform.GetModelMatrix();
            shader.SetMatrix4("model", model);
            Matrix3 normalRot = new Matrix3(Matrix4.Transpose(model.Inverted()));
            shader.SetMatrix3("normalRot", normalRot);
        }
        private void UpdateCameraData(Camera camera)
        {
            shader.SetMatrix4("view", camera.GetViewMatrix());
            shader.SetMatrix4("projection", camera.GetProjectionMatrix());
            shader.SetVector3("viewPos", camera.Position);
        }

        private void UpdateMeshRadius()
        {
            Vector3 tVert;
            float maxLengthSquared = 0;
            for (int i = 0; i < vertices.Length; i += VERTEX_SIZE)
            {
                tVert = new Vector3(vertices[i], vertices[i + 1], vertices[i + 2]);
                if (tVert.LengthSquared > maxLengthSquared)
                {
                    maxLengthSquared = tVert.LengthSquared;
                }
            }
            meshMaxRadius = MathF.Sqrt(maxLengthSquared);
        }
    }

    public class MeshedObjectDistanceComparer : IComparer<MeshedObject>
    {
        private readonly Camera cam;
        public MeshedObjectDistanceComparer(Camera cam)
        {
            this.cam = cam;
        }
        public int Compare(MeshedObject a, MeshedObject b)
        {
            Vector3 camPos = cam.Position;
            float da = MathF.Max(0, (a.MeshTransform.Position - camPos).LengthSquared - a.GetMeshRadius() * a.GetMeshRadius());
            float db = MathF.Max(0, (b.MeshTransform.Position - camPos).LengthSquared - b.GetMeshRadius() * b.GetMeshRadius());
            int result = da.CompareTo(db);

            if (result != 0)
            {
                return result;
            }
            else
            {
                return a.Vertices.Length.CompareTo(b.Vertices.Length);
            }
        }
    }
}
