using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    class LightManager
    {
        private readonly List<Light> lights = new List<Light>();

        public LightManager()
        {
        }

        public void AddLight(Light light)
        {
            if (light.Type == LightType.DirectionalLight)
            {
                lights.Insert(0, light);
            }
            else
            {
                lights.Add(light);
            }
        }

        public void RemoveLight(Light light)
        {
            lights.Remove(light);
        }

        public void ClearLights()
        {
            lights.Clear();
        }

        //TODO: reduce time complexity to lower than O(N)?
        public List<Light> GetRelevantLightsForObject(MeshedObject obj)
        {
            List<Light> result = new List<Light>();
            for (short i = 0; i < lights.Count; ++i)
            {
                Light light = lights[i];
                if (light.Type == LightType.PointLight)
                {
                    PointLight pointLight = (PointLight)light;
                    float combinedRadius = pointLight.Radius + obj.GetMeshRadius();
                    if (ApproximatedDistance(pointLight.InternalVector.Xyz, obj.Transform.Position) < combinedRadius)
                    {
                        result.Add(pointLight);
                    }
                }
                else
                {
                    result.Add(light);
                }
                if (result.Count >= Renderer.MaxSimultaneousLights)
                {
                    Console.WriteLine("Too many lights together, some might be culled");
                    return result;
                }
            }
            return result;
        }

        private readonly float manhattanWorstCaseScaleCorrection = MathF.Sqrt(2) / 2.0f;
        private float ApproximatedDistance(Vector3 p1, Vector3 p2)
        {
            float manhattanDist = MathF.Abs(p1.X - p2.X) + MathF.Abs(p1.Y - p2.Y) + MathF.Abs(p1.Z - p2.Z);
            //Multiply by this factor to make it only return values equal or smaller to the actual dist
            return manhattanDist * manhattanWorstCaseScaleCorrection;
        }

    }
}
