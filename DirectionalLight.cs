using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    public class DirectionalLight : Light
    {
        public DirectionalLight(Vector3 direction, Vector3 col, float intensity = 1)
        {
            Type = LightType.DirectionalLight;
            InternalVector = new Vector4(direction, 0);
            Color = col;
            Intensity = intensity;
        }

        public DirectionalLight(Vector3 direction, float intensity = 1)
        {
            Type = LightType.DirectionalLight;
            InternalVector = new Vector4(direction, 0);
            Color = Vector3.One;
            Intensity = intensity;
        }

    }
}
