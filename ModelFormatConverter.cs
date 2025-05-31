using System;
using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    static class ModelFormatConverter
    {
        private class FloatArrayComparer : IEqualityComparer<float[]>
        {
            public bool Equals(float[] a, float[] b)
            {
                if (ReferenceEquals(a, b)) return true;
                if (a == null || b == null) return false;
                if (a.Length != b.Length) return false;
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i]) return false;
                }
                return true;
            }

            public int GetHashCode(float[] arr)
            {
                if (arr == null) return 0;
                unchecked
                {
                    int hash = 17;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        int bits = BitConverter.SingleToInt32Bits(arr[i]);
                        hash = hash * 31 + bits;
                    }
                    return hash;
                }
            }
        }

        private static readonly FloatArrayComparer vertexComparer = new();

        public static void SimplifyToIndexFormat(int vertexSize, ref float[] vertices, out uint[] indices)
        {
            List<float[]> vertexBuffer = new(vertices.Length / vertexSize);
            List<uint> indexBuffer = new(vertices.Length / vertexSize);
            var vertToIndexCache = new Dictionary<float[], uint>(vertexComparer);

            for (int i = 0; i < vertices.Length; i += vertexSize)
            {
                float[] vertex = new float[vertexSize];
                Array.Copy(vertices, i, vertex, 0, vertexSize);
                if (vertToIndexCache.TryGetValue(vertex, out uint matchingIndex))
                {
                    indexBuffer.Add(matchingIndex);
                }
                else
                {
                    uint newIndex = (uint)vertexBuffer.Count;
                    indexBuffer.Add(newIndex);
                    vertToIndexCache[vertex] = newIndex;
                    vertexBuffer.Add(vertex);
                }

            }
            //PrintVertexBuffer();
            //PrintIndexBuffer(indexBuffer);
            Console.WriteLine("Previous vert count: " + vertices.Length / vertexSize + ". Simplified vert count: " + vertexBuffer.Count);
            vertices = GetVertexBuffer(vertexBuffer, vertexSize);
            indices = indexBuffer.ToArray();
        }

        public static float[] GetVertexBuffer(List<float[]> vertexBuffer, int vertSize)
        {
            float[] unifiedVertexBuffer = new float[vertexBuffer.Count * vertSize];
            for (int i = 0; i < vertexBuffer.Count; ++i)
            {
                float[] vert = vertexBuffer[i];
                for (int j = 0; j < vertSize; ++j)
                {
                    unifiedVertexBuffer[vertSize * i + j] = vert[j];
                }
            }
            return unifiedVertexBuffer;
        }

        public static float[] CombineToVertexBufferOnly(float[] vertexBuffer, uint[] indexBuffer, int vertexSize)
        {
            List<float> result = new List<float>();
            foreach (int index in indexBuffer)
            {
                var tVertex = vertexBuffer[index..(index + vertexSize)];
                result.AddRange(tVertex);
            }
            Console.WriteLine("VB Length is: " + result.Count);
            return result.ToArray();
        }

        public static uint[] GetGenericIndexBuffer(float[] vertexBuffer, int vertexSize)
        {
            int size = (int)(vertexBuffer.Length / vertexSize);
            uint[] result = new uint[size];

            for (uint i = 0; i < size; ++i)
            {
                result[i] = i;
            }

            return result;
        }

        private static void PrintVertexBuffer(List<float[]> vertexBuffer)
        {
            foreach (float[] vert in vertexBuffer)
            {
                string text = "";
                foreach (float item in vert)
                {
                    text += item;
                    text += ", ";
                }
                Console.WriteLine(text);
            }
        }
        private static void PrintIndexBuffer(List<uint> indexBuffer)
        {
            string text = "";
            foreach (int item in indexBuffer)
            {
                text += item;
                text += ", ";
            }
            Console.WriteLine(text);
        }
    }
}
