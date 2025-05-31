using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace OpenTK_3D_Renderer
{
    public class ColladaSceneLoader : ISceneLoader
    {
        private struct MaterialData
        {
            public Vector4 DiffuseTint;
            public Vector4 Emissive;
        }

        private const float FLOAT_ROUNDING_THRESHOLD = 0.0001f;
        private XElement loadedDocument, sceneTransformsParent;
        private Dictionary<string, MaterialData> materialDataCache;

        public void LoadScene(string filePath, out List<MeshedObject> meshes, out List<Light> lights)
        {
            lights = new();

            loadedDocument = XElement.Load(filePath);
            var lightsData = RecursiveGetChildElementWithTag(loadedDocument, "library_lights");
            if (lightsData != null)
            {
                lights.AddRange(CreateLights(lightsData));
            }

            meshes = CreateMeshes();

            loadedDocument = null;
            sceneTransformsParent = null;
        }

        private List<Light> CreateLights(XElement lightsElement)
        {
            List<Light> lights = new();

            foreach (XElement lightElement in lightsElement.Elements())
            {
                var lightNameData = lightElement.Attributes().Where(x => x.Name.LocalName.Contains("name", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (lightNameData == default)
                {
                    continue;
                }
                string lightName = (string)lightNameData;
                var lightData = RecursiveGetChildElementWithTag(lightElement, "point");
                if (lightData != null)
                {
                    XElement colorData = RecursiveGetChildElementWithTag(lightData, "color");
                    Vector3 rawColorVector = GetVector3(colorData);
                    float intensity = MathF.Max(rawColorVector.X, MathF.Max(rawColorVector.Y, rawColorVector.Z));
                    if (intensity > 1)
                    {
                        rawColorVector /= intensity;
                    }
                    Transform lightTransform = GetSceneObjectTransform(lightName, true);
                    Light newLight = new PointLight(lightTransform.Position, rawColorVector, intensity, (intensity + 1) * .5f);
                    lights.Add(newLight);
                }
                else
                {
                    //Try process directional light
                    lightData = RecursiveGetChildElementWithTag(lightElement, "directional");
                    if (lightData == null)
                    {
                        continue;
                    }

                    XElement colorData = RecursiveGetChildElementWithTag(lightData, "color");
                    Vector3 rawColorVector = GetVector3(colorData);
                    float intensity = MathF.Max(rawColorVector.X, MathF.Max(rawColorVector.Y, rawColorVector.Z));
                    if (intensity > 1)
                    {
                        rawColorVector /= intensity;
                    }
                    Transform lightTransform = GetSceneObjectTransform(lightName);
                    Vector3 lightDirection = Vector3.Transform(-Vector3.UnitY, lightTransform.Rotation);
                    Light newLight = new DirectionalLight(lightDirection, rawColorVector, intensity);
                    lights.Add(newLight);
                }
            }

            return lights;
        }

        private List<MeshedObject> CreateMeshes()
        {
            List<MeshedObject> meshedObjects = new();

            LoadSceneMaterials();
            XElement geometryLibrary = RecursiveGetChildElementWithTag(loadedDocument, "library_geometries", 2);
            List<XElement> meshDataElements = RecursiveGetChildrenWithTag(geometryLibrary, "geometry", 1);
            for (int i = 0; i < meshDataElements.Count; ++i)
            {
                string meshName = (string)meshDataElements[i].Attributes().Where(x => x.Name.LocalName.Contains("name", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (meshName == default)
                {
                    continue;
                }

                Transform meshedObjectTransform = GetSceneObjectTransform(meshName);

                XElement trianglesData = RecursiveGetChildElementWithTag(meshDataElements[i], "triangles", 3);
                List<XElement> vertexDataSources = RecursiveGetChildrenWithTag(meshDataElements[i], "source", 3);
                float[] uncompressedVertexBuffer = ReadMeshVertexBuffer(trianglesData, vertexDataSources);

                string materialName = (string)trianglesData.Attributes().FirstOrDefault(x => x.Name.LocalName.Contains("material", StringComparison.OrdinalIgnoreCase));
                Material material;
                if (string.IsNullOrEmpty(materialName))
                {
                    material = new Material();
                }
                else
                {
                    material = new Material(new Texture(Project.Resources + "crateTex.png"), Vector3.One, materialDataCache[materialName].DiffuseTint);
                }

                MeshedObject newMesh = new MeshedObject(meshedObjectTransform, uncompressedVertexBuffer, material);
                meshedObjects.Add(newMesh);
            }

            if (materialDataCache != null)
            {
                materialDataCache.Clear();
                materialDataCache = null;
            }

            return meshedObjects;
        }

        private float[] ReadMeshVertexBuffer(XElement trianglesData, List<XElement> vertexDataSources)
        {
            string vertexPositionsData = (string)vertexDataSources.Find(x => ((string)x.FirstAttribute).Contains("mesh-positions", StringComparison.OrdinalIgnoreCase));
            List<float> vertexPositions = ParseToFloatArray(vertexPositionsData.Split(" ")).ToList();
            string vertexNormalsData = (string)vertexDataSources.Find(x => ((string)x.FirstAttribute).Contains("mesh-normals", StringComparison.OrdinalIgnoreCase));
            List<float> vertexNormals = ParseToFloatArray(vertexNormalsData.Split(" ")).ToList();
            string vertexTexCoordsData = (string)vertexDataSources.Find(x => ((string)x.FirstAttribute).Contains("mesh-map-0", StringComparison.OrdinalIgnoreCase));
            List<float> texCoords = ParseToFloatArray(vertexTexCoordsData.Split(" ")).ToList();

            int[] indexList = ParseToIntArray(((string)RecursiveGetChildElementWithTag(trianglesData, "p", 1, false)).Split(" "));

            List<float> uncompressedVertexBuffer = new(indexList.Length * 9);
            for (int j = 0; j < indexList.Length; j += 9)
            {
                // Extract vertex data for a single triangle
                for (int k = 0; k < 3; k++)
                {
                    int positionIndex = indexList[j + (k * 3) + 0] * 3;
                    int normalIndex = indexList[j + (k * 3) + 1] * 3;
                    int texCoordIndex = indexList[j + (k * 3) + 2] * 2;

                    List<float> position = vertexPositions.GetRange(positionIndex, 3);
                    List<float> normal = vertexNormals.GetRange(normalIndex, 3);
                    List<float> texCoord = texCoords.GetRange(texCoordIndex, 2);
                    uncompressedVertexBuffer.AddRange(position);
                    uncompressedVertexBuffer.AddRange(normal);
                    uncompressedVertexBuffer.AddRange(texCoord);
                }
            }

            return uncompressedVertexBuffer.ToArray();
        }

        private XElement RecursiveGetChildElementWithTag(XElement currentElement, string tagName, int maxDepth = 10, bool acceptPartialMatches = true)
        {
            if (currentElement != null && currentElement.HasElements && --maxDepth >= 0)
            {
                XElement[] descendants = currentElement.Descendants().ToArray();
                foreach (var child in descendants)
                {
                    if (!string.IsNullOrEmpty(child.Name.LocalName) && ElementMatchesString(child, tagName, acceptPartialMatches))
                    {
                        return child;
                    }
                    else
                    {
                        XElement result = RecursiveGetChildElementWithTag(child, tagName, maxDepth);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }
            return null;
        }

        private List<XElement> RecursiveGetChildrenWithTag(XElement currentElement, string tagName, int maxDepth = 8)
        {
            List<XElement> results = new();
            if (currentElement != null && currentElement.HasElements && --maxDepth >= 0)
            {
                XElement[] descendants = currentElement.Descendants().ToArray();
                results.Capacity = descendants.Length / 2;
                foreach (var child in descendants)
                {
                    if (!string.IsNullOrEmpty(child.Name.LocalName) && child.Name.LocalName.Contains(tagName, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(child);
                    }
                    else
                    {
                        List<XElement> nestedResults = RecursiveGetChildrenWithTag(child, tagName, maxDepth);
                        if (nestedResults != null && nestedResults.Count > 0)
                        {
                            results.AddRange(nestedResults);
                        }
                    }
                }
            }
            return results;
        }


        private Vector3 GetVector3(XElement element)
        {
            float[] numbers = ParseToFloatArray(((string)element).Split(" "));
            if (numbers.Length == 3)
            {
                return new Vector3(numbers[0], numbers[1], numbers[2]);
            }

            throw new Exception("could not get right ammount of number strings");
        }

        private Vector4 GetVector4(XElement element)
        {
            float[] numbers = ParseToFloatArray(((string)element).Split(" "));
            if (numbers.Length == 4)
            {
                return new Vector4(numbers[0], numbers[1], numbers[2], numbers[3]);
            }

            throw new Exception("could not get right ammount of number strings");
        }

        private float[] ParseToFloatArray(string[] stringNumbers)
        {
            int l = stringNumbers.Length;
            float[] floats = new float[l];
            for (int i = 0; i < l; ++i)
            {
                floats[i] = float.Parse(stringNumbers[i], CultureInfo.InvariantCulture);
                if (MathF.Abs(MathF.Round(floats[i]) - floats[i]) < FLOAT_ROUNDING_THRESHOLD)
                {
                    floats[i] = MathF.Round(floats[i]);
                }
            }
            return floats;
        }

        private int[] ParseToIntArray(string[] source)
        {
            int length = source.Length;
            int[] ints = new int[length];
            for (int i = 0; i < length; i++)
            {
                ints[i] = int.Parse(source[i]);
            }
            return ints;
        }

        private bool ElementMatchesString(XElement element, string nameToMatch, bool acceptPartialMatch = true)
        {
            return element.Name.LocalName.Equals(nameToMatch, StringComparison.OrdinalIgnoreCase)
                || (acceptPartialMatch && element.Name.LocalName.Contains(nameToMatch, StringComparison.InvariantCultureIgnoreCase));
        }

        private Transform GetSceneObjectTransform(string objectName, bool positionOnly = false)
        {
            XElement sceneObjectData = GetSceneObjectTransformData(objectName);
            if (sceneObjectData == null)
            {
                return null;
            }

            XElement positionData = RecursiveGetChildElementWithTag(sceneObjectData, "translate", 2);

            if (positionData == null)
            {
                return GetTransformFromCombinedMatrix(sceneObjectData, positionOnly);
            }

            Vector3 position = GetVector3(positionData);

            if (positionOnly)
            {
                return new Transform(position);
            }

            List<XElement> rotationDataElements = RecursiveGetChildrenWithTag(sceneObjectData, "rotate", 1);
            Vector3 eulerAngles = ExtractRotationAngles(rotationDataElements);
            Quaternion rotation = new(eulerAngles.X, eulerAngles.Y, eulerAngles.Z);

            XElement scaleData = RecursiveGetChildElementWithTag(sceneObjectData, "scale");
            Vector3 scaleVector = GetVector3(scaleData);
            float uniformScale = (MathF.Abs(scaleVector.X) + MathF.Abs(scaleVector.Y) + MathF.Abs(scaleVector.Z)) / 3;

            return new Transform(position, rotation, uniformScale);
        }

        private Transform GetTransformFromCombinedMatrix(XElement element, bool positionOnly = false)
        {
            XElement matrixData = RecursiveGetChildElementWithTag(element, "matrix", 2);
            if (matrixData == null)
            {
                return null;
            }

            float[] m = ParseToFloatArray(((string)matrixData).Split(" "));
            Matrix4 matrix = new Matrix4(m[0], m[1], m[2], m[3],
                                        m[4], m[5], m[6], m[7],
                                        m[8], m[9], m[10], m[11],
                                        m[12], m[13], m[14], m[15]);

            Vector3 position = matrix.Column3.Xyz;
            if (positionOnly)
            {
                return new Transform(position);
            }

            Quaternion rotation = matrix.ExtractRotation();

            Vector3 scaleVector = matrix.ExtractScale();
            float uniformScale = (MathF.Abs(scaleVector.X) + MathF.Abs(scaleVector.Y) + MathF.Abs(scaleVector.Z)) / 3;

            return new Transform(position, rotation, uniformScale);
        }

        private XElement GetSceneObjectTransformData(string objectName)
        {
            if (sceneTransformsParent == null)
            {
                sceneTransformsParent = RecursiveGetChildElementWithTag(loadedDocument, "visual_scene", 4);
            }
            if (sceneTransformsParent != null)
            {
                List<XElement> nodes = RecursiveGetChildrenWithTag(sceneTransformsParent, "node", 2);
                for (int i = 0; i < nodes.Count; ++i)
                {
                    XAttribute nameAtribute = nodes[i].Attributes().Where(x => x.Name.LocalName.Contains("name", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (nameAtribute != default && ((string)nameAtribute).Equals(objectName))
                    {
                        return nameAtribute.Parent;
                    }
                }
            }

            return null;
        }

        private Vector3 ExtractRotationAngles(List<XElement> elements)
        {
            string GetRotationText(string attributeName)
            {
                return (string)(elements.Where(
                x => x.Attributes().Where(
                y => ((string)y).Contains(attributeName))
                .Any()).FirstOrDefault());
            }

            string[] rotations = new string[3];
            rotations[0] = GetRotationText("rotationX").Split(" ")[3];
            rotations[1] = GetRotationText("rotationY").Split(" ")[3];
            rotations[2] = GetRotationText("rotationZ").Split(" ")[3];

            float[] floats = ParseToFloatArray(rotations);
            return new Vector3(floats[0], floats[1], floats[2]);
        }

        private void LoadSceneMaterials()
        {
            XElement meterialLibrary = RecursiveGetChildElementWithTag(loadedDocument, "library_materials", 2);
            List<XElement> materials = RecursiveGetChildrenWithTag(meterialLibrary, "material");

            XElement effectsLibrary = RecursiveGetChildElementWithTag(loadedDocument, "library_effects", 2);
            List<XElement> effects = RecursiveGetChildrenWithTag(effectsLibrary, "effect");
            materialDataCache = new Dictionary<string, MaterialData>();
            for (int i = 0; i < materials.Count; ++i)
            {
                string materialId = (string)materials[i].FirstAttribute;
                string effectId = (string)materials[i].Descendants().First().FirstAttribute;
                if (string.IsNullOrEmpty(effectId))
                {
                    continue;
                }

                effectId = effectId.Replace("#", "");

                XElement effectData = effects.Find(x => ((string)x.FirstAttribute).Contains(effectId, StringComparison.OrdinalIgnoreCase));

                XElement diffuseTintData = RecursiveGetChildElementWithTag(RecursiveGetChildElementWithTag(effectData, "diffuse"), "color", 2);

                MaterialData matData = new();
                matData.DiffuseTint = GetVector4(diffuseTintData);

                materialDataCache.Add(materialId, matData);
            }

        }


    }
}
