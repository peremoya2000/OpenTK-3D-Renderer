using System;
using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    static class ModelFormatConverter
    {
        private static readonly List<float[]> vertexBuffer = new List<float[]>();
        private static readonly List<uint> indexBuffer = new List<uint>();

        public static void SimplifyToIndexFormat(int vertexSize, ref float[] vertices, out uint[] indices)
        {
            vertexBuffer.Clear();
            indexBuffer.Clear();
            for (int i = 0; i < vertices.Length; i += vertexSize)
            {
                float[] vertex = new float[vertexSize];
                Array.Copy(vertices, i, vertex, 0, vertexSize);
                int index = GetVertIndex(vertex);
                if (index >= 0)
                {
                    indexBuffer.Add((uint)index);
                }
                else
                {
                    indexBuffer.Add((uint)vertexBuffer.Count);
                    vertexBuffer.Add(vertex);
                }

            }
            //PrintVertexBuffer();
            //PrintIndexBuffer();
            Console.WriteLine("Previous vert count: " + vertices.Length / vertexSize + ". Simplified vert count: " + vertexBuffer.Count);
            vertices = GetVertexBuffer(vertexSize);
            indices = GetIndexBuffer();
        }

        public static float[] GetVertexBuffer(int vertSize)
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
        public static uint[] GetIndexBuffer()
        {
            return indexBuffer.ToArray();
        }

        public static float[] CombineToVertexBufferOnly(float[] vertexBuffer, uint[] indexBuffer, byte vertexSize)
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

        private static void PrintVertexBuffer()
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
        private static void PrintIndexBuffer()
        {
            string text = "";
            foreach (int item in indexBuffer)
            {
                text += item;
                text += ", ";
            }
            Console.WriteLine(text);
        }

        private static int GetVertIndex(float[] newVert)
        {
            return vertexBuffer.FindIndex(0, v => AreIdentical(v, newVert));
        }
        private static bool AreIdentical(float[] arr0, float[] arr1)
        {
            int length = Math.Min(arr0.Length, arr1.Length);
            for (int i = 0; i < length; ++i)
            {
                if (arr0[i] != arr1[i])
                {
                    return false;
                }
            }
            return true;
        }

    }
}
