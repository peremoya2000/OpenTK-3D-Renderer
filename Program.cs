using System;

namespace OpenTK_3D_Renderer
{
    class Program
    {
        static void Main()
        {
            using (Renderer renderer = new Renderer(1920, 1080, "OpenTK-Renderer"))
            {
                renderer.Run();
            }
        }
    }
}
