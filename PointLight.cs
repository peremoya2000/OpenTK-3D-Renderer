using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    public class PointLight : Light
    {
        public float Radius;
        public PointLight(Vector3 position, Vector3 col, float radius, float intensity = 1)
        {
            Type = LightType.PointLight;
            InternalVector = new Vector4(position, 1);
            Color = col;
            Intensity = intensity;
            Radius = radius;
        }
        public PointLight(Vector3 position, float radius = 50, float intensity = 1)
        {
            Type = LightType.PointLight;
            InternalVector = new Vector4(position, 1);
            Color = Vector3.One;
            Intensity = intensity;
            Radius = radius;
        }

    }
}
