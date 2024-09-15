using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OpenTK_3D_Renderer
{
    public class ObjFileLoader : ISceneLoader
    {
        private struct MeshedObjectDataBuffers
        {
            public MeshedObjectDataBuffers()
            {
                ResultVertexBuffer = new();
                VertexPositions = new();
                Normals = new();
                TextureCoords = new();
            }

            public List<float> ResultVertexBuffer;
            public List<float> VertexPositions;
            public List<float> Normals;
            public List<float> TextureCoords;
        }
        private MeshedObjectDataBuffers currentMeshData;
        private string lastImportedObjectFile = "";

        private static readonly float[] defaultCubeVerts =
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

        private enum LineType { None, NewMesh, VertexPosition, Normal, TextureCoords, Indices };

        public void LoadScene(string filePath, out List<MeshedObject> meshes, out List<Light> lights)
        {
            meshes = new List<MeshedObject>();

            using FileStream fileStream = File.OpenRead(filePath);
            using (var streamReader = new StreamReader(fileStream, true))
            {
                string line;
                string[] segments;
                while ((line = streamReader.ReadLine()) != null)
                {
                    segments = line.Split(" ");
                    LineType type = GetLineType(segments[0]);
                    if (type == LineType.NewMesh)
                    {
                        AddMeshedObjectToScene(meshes, currentMeshData);
                        currentMeshData = new MeshedObjectDataBuffers();
                    }
                    else
                    {
                        ProcessObjMeshDataLine(segments, type);
                    }
                }

                AddMeshedObjectToScene(meshes, currentMeshData);
                currentMeshData = default;
            }


            lights = new List<Light>();
            var defaultLight = new DirectionalLight(-Vector3.UnitZ);
            lights.Add(defaultLight);
        }

        public MeshedObject LoadMeshedObject(string path)
        {
            if (lastImportedObjectFile.Equals(path) && currentMeshData.ResultVertexBuffer != null && currentMeshData.ResultVertexBuffer.Count > 0)
            {
                return new MeshedObject(new Transform(), currentMeshData.ResultVertexBuffer.ToArray(), new Material());
            }

            using FileStream fileStream = File.OpenRead(path);
            using (var streamReader = new StreamReader(fileStream, true))
            {
                string line;
                string[] segments;
                while ((line = streamReader.ReadLine()) != null)
                {
                    segments = line.Split(" ");
                    LineType type = GetLineType(segments[0]);
                    if (type == LineType.NewMesh)
                    {
                        if (currentMeshData.ResultVertexBuffer != null)
                        {
                            break;
                        }
                        currentMeshData = new MeshedObjectDataBuffers();
                    }
                    else
                    {
                        ProcessObjMeshDataLine(segments, type);
                    }
                }
            }

            lastImportedObjectFile = path;
            MeshedObjectDataBuffers resultBuffers = currentMeshData;
            currentMeshData = default;
            return new MeshedObject(new Transform(), resultBuffers.ResultVertexBuffer.ToArray(), new Material());
        }


        public static float[] GetDefaultCube()
        {
            return defaultCubeVerts;
        }

        private void ProcessObjMeshDataLine(string[] segments, LineType lineType)
        {
            float[] tempFloats;
            switch (lineType)
            {
                default:
                case LineType.None:
                    break;
                case LineType.VertexPosition:
                    tempFloats = StringsToFloats(segments[1..4]);
                    currentMeshData.VertexPositions.AddRange(tempFloats);
                    break;
                case LineType.Normal:
                    tempFloats = StringsToFloats(segments[1..4]);
                    currentMeshData.Normals.AddRange(tempFloats);
                    break;
                case LineType.TextureCoords:
                    tempFloats = StringsToFloats(segments[1..3]);
                    currentMeshData.TextureCoords.AddRange(tempFloats);
                    break;
                case LineType.Indices:
                    if (segments.Length > 4)
                    {
                        throw new IOException("Quad based objects are not supported");
                    }
                    AddNewTriangle(segments[1..4]);
                    break;
            }
        }

        private LineType GetLineType(string indicator)
        {
            switch (indicator)
            {
                case "v":
                    return LineType.VertexPosition;
                case "vn":
                    return LineType.Normal;
                case "vt":
                    return LineType.TextureCoords;
                case "f":
                    return LineType.Indices;
                case "o":
                    return LineType.NewMesh;
                default:
                    return LineType.None;
            }
        }

        private void AddMeshedObjectToScene(List<MeshedObject> list, MeshedObjectDataBuffers data)
        {
            if (data.ResultVertexBuffer != null && list != null)
            {
                list.Add(new MeshedObject(new Transform(), data.ResultVertexBuffer.ToArray(), new Material()));
            }
        }

        private float[] StringsToFloats(string[] source)
        {
            int l = source.Length;
            float[] floats = new float[l];
            for (int i = 0; i < l; ++i)
            {
                floats[i] = float.Parse(source[i], CultureInfo.InvariantCulture);
            }
            return floats;
        }

        private void StartNewMeshData()
        {

        }

        private void AddNewTriangle(string[] indices)
        {
            List<int> data = new List<int>();
            foreach (var subIndex in indices)
            {
                data.AddRange(StringsToInts(subIndex.Split("/")));
            }

            for (int i = 0; i < 9; i += 3)
            {
                var vPos = currentMeshData.VertexPositions.GetRange(data[i] * 3, 3);
                var texCoord = currentMeshData.TextureCoords.GetRange(data[i + 1] * 2, 2);
                var vNor = currentMeshData.Normals.GetRange(data[i + 2] * 3, 3);
                currentMeshData.ResultVertexBuffer.AddRange(vPos);
                currentMeshData.ResultVertexBuffer.AddRange(vNor);
                currentMeshData.ResultVertexBuffer.AddRange(texCoord);
            }
        }

        private int[] StringsToInts(string[] source)
        {
            List<int> ints = new List<int>();
            foreach (var item in source)
            {
                ints.Add(int.Parse(item) - 1);
            }
            return ints.ToArray();
        }
    }
}
