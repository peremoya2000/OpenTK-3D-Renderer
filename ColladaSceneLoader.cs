using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            public string LocalPathToMainTexture;
        }

        private const float FLOAT_ROUNDING_THRESHOLD = 0.0001f;
        private XElement loadedDocument, sceneTransformsParent;
        private Dictionary<string, MaterialData> materialDataCache;

        public void LoadScene(string filePath, out List<MeshedObject> meshes, out List<Light> lights)
        {
            lights = new();

            loadedDocument = XElement.Load(filePath);
            var lightsData = GetChildElementWithTag(loadedDocument, "library_lights");
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
                var lightIdData = lightElement.Attributes().Where(x => x.Name.LocalName.Contains("id", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (lightIdData == default)
                {
                    continue;
                }
                string lightId = (string)lightIdData;
                var lightData = GetChildElementWithTag(lightElement, "point");
                if (lightData != null)
                {
                    XElement colorData = GetChildElementWithTag(lightData, "color");
                    Vector3 rawColorVector = GetVector3(colorData);
                    float intensity = MathF.Max(rawColorVector.X, MathF.Max(rawColorVector.Y, rawColorVector.Z));
                    if (intensity > 1)
                    {
                        rawColorVector /= intensity;
                    }
                    Transform lightTransform = GetSceneObjectTransform(lightId, true);
                    Light newLight = new PointLight(lightTransform.Position, rawColorVector, intensity, (intensity + 1) * .5f);
                    lights.Add(newLight);
                }
                else
                {
                    //Try process directional light
                    lightData = GetChildElementWithTag(lightElement, "directional");
                    if (lightData == null)
                    {
                        continue;
                    }

                    XElement colorData = GetChildElementWithTag(lightData, "color");
                    Vector3 rawColorVector = GetVector3(colorData);
                    float intensity = MathF.Max(rawColorVector.X, MathF.Max(rawColorVector.Y, rawColorVector.Z));
                    if (intensity > 1)
                    {
                        rawColorVector /= intensity;
                    }
                    Transform lightTransform = GetSceneObjectTransform(lightId);
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
            XElement geometryLibrary = GetChildElementWithTag(loadedDocument, "library_geometries", 2);
            List<XElement> meshDataElements = GetChildrenWithTag(geometryLibrary, "geometry");
            for (int i = 0; i < meshDataElements.Count; ++i)
            {
                string meshId = (string)meshDataElements[i].Attributes().Where(x => x.Name.LocalName.Contains("id", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (meshId == default)
                {
                    continue;
                }

                Transform meshedObjectTransform = GetSceneObjectTransform(meshId);

                XElement trianglesData = GetChildElementWithTag(meshDataElements[i], "triangles", 3);
                List<XElement> vertexDataSources = GetChildrenWithTag(meshDataElements[i], "source");
                float[] uncompressedVertexBuffer = ReadMeshVertexBuffer(trianglesData, vertexDataSources);

                string materialName = (string)trianglesData.Attributes().FirstOrDefault(x => x.Name.LocalName.Contains("material", StringComparison.OrdinalIgnoreCase));
                Material material;
                if (string.IsNullOrEmpty(materialName))
                {
                    material = new Material();
                }
                else
                {
                    var matData = materialDataCache[materialName];
                    string localTexturePath = string.IsNullOrEmpty(matData.LocalPathToMainTexture) ? Project.DefaultTex : matData.LocalPathToMainTexture;
                    material = new Material(GLResourceCache.AddOrGetTexture(Project.Resources + localTexturePath), Vector3.One, matData.DiffuseTint);
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
            vertexNormals = NormalizeVector3List(vertexNormals);
            string vertexTexCoordsData = (string)vertexDataSources.Find(x => ((string)x.FirstAttribute).Contains("mesh-map-0", StringComparison.OrdinalIgnoreCase)
                                                                            || ((string)x.FirstAttribute).Contains("mesh-map", StringComparison.OrdinalIgnoreCase));
            List<float> texCoords = ParseToFloatArray(vertexTexCoordsData.Split(" ")).ToList();

            int[] indexList = ParseToIntArray(((string)GetChildElementWithTag(trianglesData, "p", 1, false)).Split(" "));

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

        private XElement GetChildElementWithTag(XElement currentElement, string tagName, int maxDepth = 10, bool acceptPartialMatches = true)
        {
            if (currentElement == null || !currentElement.HasElements)
                return null;

            foreach (var descendant in currentElement.Descendants())
            {
                if (!string.IsNullOrEmpty(descendant.Name.LocalName) && ElementMatchesString(descendant, tagName, acceptPartialMatches))
                {
                    return descendant;
                }
            }
            return null;
        }

        private List<XElement> GetChildrenWithTag(XElement currentElement, string tagName)
        {
            List<XElement> results = new();
            if (currentElement == null || !currentElement.HasElements)
                return results;

            foreach (var descendant in currentElement.Descendants())
            {
                if (!string.IsNullOrEmpty(descendant.Name.LocalName)
                    && descendant.Name.LocalName.Contains(tagName, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(descendant);
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

        private List<float> NormalizeVector3List(List<float> floats)
        {
            int l = floats.Count;
            Debug.Assert(l % 3 == 0, "The list cannot be grouped into Vector3s");
            for (int i = 0; i < l; i += 3)
            {
                Vector3 v = new(floats[i], floats[i + 1], floats[i + 2]);
                v.Normalize();
                floats[i] = v.X;
                floats[i + 1] = v.Y;
                floats[i + 2] = v.Z;
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

        private Transform GetSceneObjectTransform(string objectId, bool positionOnly = false)
        {
            XElement sceneObjectData = GetSceneObjectTransformData(objectId);
            if (sceneObjectData == null)
            {
                return null;
            }

            XElement positionData = GetChildElementWithTag(sceneObjectData, "translate", 2);

            if (positionData == null)
            {
                return GetTransformFromCombinedMatrix(sceneObjectData, positionOnly);
            }

            Vector3 position = GetVector3(positionData);

            if (positionOnly)
            {
                return new Transform(position);
            }

            List<XElement> rotationDataElements = GetChildrenWithTag(sceneObjectData, "rotate");
            Vector3 eulerAngles = ExtractRotationAngles(rotationDataElements);
            Quaternion rotation = Quaternion.FromEulerAngles(
                MathHelper.DegreesToRadians(eulerAngles.X),
                MathHelper.DegreesToRadians(eulerAngles.Y),
                MathHelper.DegreesToRadians(eulerAngles.Z)
            );

            XElement scaleData = GetChildElementWithTag(sceneObjectData, "scale");
            Vector3 scaleVector = GetVector3(scaleData);
            float uniformScale = (MathF.Abs(scaleVector.X) + MathF.Abs(scaleVector.Y) + MathF.Abs(scaleVector.Z)) / 3;

            return new Transform(position, rotation, uniformScale);
        }

        private Transform GetTransformFromCombinedMatrix(XElement element, bool positionOnly = false)
        {
            XElement matrixData = GetChildElementWithTag(element, "matrix", 2);
            if (matrixData == null)
            {
                return null;
            }

            float[] m = ParseToFloatArray(((string)matrixData).Split(" "));
            // Transpose on construction: Map Collada's columns directly into OpenTK's rows.
            Matrix4 matrix = new Matrix4(
                m[0], m[4], m[8], m[12],
                m[1], m[5], m[9], m[13],
                m[2], m[6], m[10], m[14],
                m[3], m[7], m[11], m[15]
            );

            Vector3 position = matrix.ExtractTranslation();

            if (positionOnly)
            {
                return new Transform(position);
            }

            Quaternion rotation = matrix.ExtractRotation();

            Vector3 scaleVector = matrix.ExtractScale();
            float uniformScale = (MathF.Abs(scaleVector.X) + MathF.Abs(scaleVector.Y) + MathF.Abs(scaleVector.Z)) / 3;

            return new Transform(position, rotation, uniformScale);
        }

        private XElement GetSceneObjectTransformData(string objectId)
        {
            if (sceneTransformsParent == null)
            {
                sceneTransformsParent = GetChildElementWithTag(loadedDocument, "visual_scene", 4);
            }
            if (sceneTransformsParent != null)
            {
                List<XElement> nodes = GetChildrenWithTag(sceneTransformsParent, "node");
                for (int i = 0; i < nodes.Count; ++i)
                {
                    if (TransformNodeContainsId(nodes[i], objectId))
                    {
                        return nodes[i];
                    }
                }
            }

            return null;
        }

        private bool TransformNodeContainsId(XElement transformNode, string id)
        {
            var meshInstanceData = GetChildElementWithTag(transformNode, "instance_geometry", 1);
            if (meshInstanceData != null)
            {
                XAttribute urlAttribute = meshInstanceData.Attributes().FirstOrDefault(x => x.Name.LocalName.Contains("url", StringComparison.OrdinalIgnoreCase));
                return urlAttribute != null && ((string)urlAttribute).Contains(id, StringComparison.OrdinalIgnoreCase);
            }

            var lightInstanceData = GetChildElementWithTag(transformNode, "instance_light", 1);
            if (lightInstanceData != null)
            {
                XAttribute urlAttribute = lightInstanceData.Attributes().FirstOrDefault(x => x.Name.LocalName.Contains("url", StringComparison.OrdinalIgnoreCase));
                return urlAttribute != null && ((string)urlAttribute).Contains(id, StringComparison.OrdinalIgnoreCase);
            }

            XAttribute nameAtribute = transformNode.Attributes().Where(x => x.Name.LocalName.Contains("id", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            return nameAtribute != default && ((string)nameAtribute).Equals(id);
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
            XElement meterialLibrary = GetChildElementWithTag(loadedDocument, "library_materials", 2);
            List<XElement> materials = GetChildrenWithTag(meterialLibrary, "material");

            XElement effectsLibrary = GetChildElementWithTag(loadedDocument, "library_effects", 2);
            List<XElement> effects = GetChildrenWithTag(effectsLibrary, "effect");
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

                XElement diffuseData = GetChildElementWithTag(effectData, "diffuse");
                XElement diffuseTintData = GetChildElementWithTag(diffuseData, "color", 2);
                XElement emisionData = GetChildElementWithTag(GetChildElementWithTag(effectData, "emission"), "color", 2);

                MaterialData matData = new();
                if (diffuseTintData != null)
                {
                    matData.DiffuseTint = GetVector4(diffuseTintData);
                }
                else
                {
                    matData.DiffuseTint = Vector4.One;
                }
                if (emisionData != null)
                {
                    matData.Emissive = GetVector4(emisionData);
                }

                XElement diffuseTextureData = GetChildElementWithTag(diffuseData, "texture", 2);
                if (diffuseTextureData != null)
                {
                    matData.LocalPathToMainTexture = GetMainTexLocalPath(effectData, diffuseTextureData);
                }

                materialDataCache.Add(materialId, matData);
            }

        }

        private string GetMainTexLocalPath(XElement effectData, XElement diffuseTextureData)
        {
            string sampler = (string)diffuseTextureData.FirstAttribute;

            List<XElement> effectParams = GetChildrenWithTag(effectData, "newparam").Where(x => x.FirstAttribute != null).ToList();
            XElement samplerParent = effectParams.FirstOrDefault(x => (string)(x.FirstAttribute) == sampler);
            if (samplerParent == null)
            {
                return null;
            }

            string surfaceId = (string)GetChildElementWithTag(samplerParent, "source");
            XElement surfaceParent = effectParams.FirstOrDefault(x => (string)(x.FirstAttribute) == surfaceId);
            string imageId = (string)GetChildElementWithTag(surfaceParent, "init_from");

            List<XElement> images = GetChildrenWithTag(loadedDocument, "image");
            XElement image = images.FirstOrDefault(x => x.FirstAttribute != null && (string)(x.FirstAttribute) == imageId);
            return (image != null) ? (string)image.Descendants().First() : null;
        }
    }
}
