using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace OpenTK_3D_Renderer
{
    public static class ModelImporter
    {
        private static List<float> uncompressedVertexBuffer;
        private static List<float> tVertexPositions;
        private static List<float> tNormals;
        private static List<float> tTextureCoords;
        private static string importedMesh = "";

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

        private enum LineType { None, VertexPosition, Normal, TextureCoords, Indices };

        public static float[] Import(string path)
        {
            if (importedMesh.Equals(path) && uncompressedVertexBuffer.Count>0)
            {
                return uncompressedVertexBuffer.ToArray();
            }

            uncompressedVertexBuffer = new List<float>();
            tVertexPositions = new List<float>();
            tNormals = new List<float>();
            tTextureCoords = new List<float>();

            using var fileStream = File.OpenRead(path);
            using (var streamReader = new StreamReader(fileStream, true))
            {
                String line;
                String[] segments;
                while ((line = streamReader.ReadLine()) != null)
                {
                    segments = line.Split(" ");
                    LineType type = GetLineType(segments[0]);
                    float[] tempFloats;
                    switch (type)
                    {
                        default:
                        case LineType.None:
                            break;
                        case LineType.VertexPosition:
                            tempFloats = StringsToFloats(segments[1..4]);
                            tVertexPositions.AddRange(tempFloats);
                            break;
                        case LineType.Normal:
                            tempFloats = StringsToFloats(segments[1..4]);
                            tNormals.AddRange(tempFloats);
                            break;
                        case LineType.TextureCoords:
                            tempFloats = StringsToFloats(segments[1..3]);
                            tTextureCoords.AddRange(tempFloats);
                            break;
                        case LineType.Indices:
                            if (segments.Length > 4)
                            {
                                throw new IOException("Quad based objects are not supported currently");
                            }
                            AddNewTriangle(segments[1..4]);
                            break;
                    }
                }
            }

            importedMesh = path;
            return uncompressedVertexBuffer.ToArray();
        }


        public static float[] GetDefaultCube()
        {
            return defaultCubeVerts;
        }

        private static LineType GetLineType(String indicator)
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
                default:
                    return LineType.None;
            }
        }

        private static float[] StringsToFloats(string[] source)
        {
            int l = source.Length;
            float[] floats = new float[l];
            for (int i = 0; i < l; ++i)
            {
                floats[i] = float.Parse(source[i], CultureInfo.InvariantCulture);
            }
            return floats;
        }

        private static void AddNewTriangle(string[] indices)
        {
            List<int> data = new List<int>();
            foreach (var subIndex in indices)
            {
                data.AddRange(StringsToInts(subIndex.Split("/")));
            }

            for (int i = 0; i < 9; i += 3)
            {
                var vPos = tVertexPositions.GetRange(data[i] * 3, 3);
                var texCoord = tTextureCoords.GetRange(data[i + 1] * 2, 2);
                var vNor = tNormals.GetRange(data[i + 2] * 3, 3);
                uncompressedVertexBuffer.AddRange(vPos);
                uncompressedVertexBuffer.AddRange(vNor);
                uncompressedVertexBuffer.AddRange(texCoord);
            }
        }

        private static int[] StringsToInts(string[] source)
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
