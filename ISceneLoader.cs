using System.Collections.Generic;

namespace OpenTK_3D_Renderer
{
    internal interface ISceneLoader
    {
        public void LoadScene(string filePath, out List<MeshedObject> meshes, out List<Light> lights);
    }
}
