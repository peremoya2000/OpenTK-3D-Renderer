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
        private struct LightUniformNames
        {
            public string Vector, Color, Intensity, Radius;
        }
        private static readonly LightUniformNames[] lightUniformsCache = new LightUniformNames[Renderer.MaxSimultaneousLights];

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

        public bool IsTransparent()
        {
            return material != null && material.Type == MaterialType.Transparent;
        }

        public float GetMeshRadius()
        {
            return meshMaxRadius * MeshTransform.Scale;
        }

        public void Dispose()
        {
            GL.DeleteBuffer(vertexBufferObject);
            GL.DeleteBuffer(elementBufferObject);
            GL.DeleteVertexArray(vertexArrayObject);
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
            shader.SetInt("material.mainTex", 0);
            material.MainTexture.Use();
            UpdateModelData();
            UpdateCameraData(camera);

            //TODO: either reuse resources when meshes are identical or use DrawElementsInstanced
            RendererState.BindVertexObjectArray(vertexArrayObject);
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
            string fragmentShaderName = (material.Type == MaterialType.Transparent) ? "shader_transparent.frag" : "shader_opaque.frag";
            shader = GLResourceCache.AddOrGetShader(Project.Resources + "shader.vert", Project.Resources + fragmentShaderName);
            shader.Use();

            CacheLightUniformNames();

            shader.SetVector3("material.ambientTint", material.AmbientTint);
            shader.SetVector3("material.diffuseTint", material.DiffuseTint.Xyz);
            shader.SetFloat("material.shininess", material.Shininess);
            if(material.Type == MaterialType.Transparent)
            {
                shader.SetFloat("material.opacity", material.Opacity);
            }

            var normalLocation = shader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

            int texCoordLocation = shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));
        }

        private static void CacheLightUniformNames()
        {
            if (lightUniformsCache != null && !string.IsNullOrEmpty(lightUniformsCache[0].Vector))
            {
                return;
            }
            for (int i = 0; i < Renderer.MaxSimultaneousLights; ++i)
            {
                string prefix = "lights[" + i + "]";
                lightUniformsCache[i] = new LightUniformNames
                {
                    Vector = prefix + ".vector",
                    Color = prefix + ".color",
                    Intensity = prefix + ".intensity",
                    Radius = prefix + ".radius"
                };
            }
        }

        private void UpdateLightsData(List<Light> lights)
        {
            for (byte i = 0; i < lights.Count; ++i)
            {
                ref LightUniformNames names = ref lightUniformsCache[i];

                switch (lights[i])
                {
                    case DirectionalLight directional:
                        shader.SetVector4(names.Vector, directional.InternalVector);
                        shader.SetVector3(names.Color, directional.Color);
                        shader.SetFloat(names.Intensity, directional.Intensity);
                        break;

                    case PointLight point:
                        shader.SetVector4(names.Vector, point.InternalVector);
                        shader.SetVector3(names.Color, point.Color);
                        shader.SetFloat(names.Intensity, point.Intensity);
                        shader.SetFloat(names.Radius, point.Radius);
                        break;
                }
            }
            shader.SetFloat("lightCount", lights.Count);
        }
        private void UpdateModelData()
        {
            Matrix4 model = MeshTransform.GetModelMatrix();
            shader.SetMatrix4("model", model);
            Matrix3 normalRot = Matrix3.Transpose(new Matrix3(model).Inverted());
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

    public class ClosestMeshedObjectComparer : IComparer<MeshedObject>
    {
        private readonly Camera cam;
        public ClosestMeshedObjectComparer(Camera cam)
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
