using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

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
            lights.Add(light);
        }

        public void RemoveLight(Light l)
        {
            lights.Remove(l);
        }

        public void ClearLights()
        {
            lights.Clear();
        }

        //TODO: Return only lights that can affect that mesh
        public List<Light> GetRelevantLightForObject(MeshedObject obj)
        {
            return lights;
        }

        
    }
}
