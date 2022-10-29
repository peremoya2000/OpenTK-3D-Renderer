using System;

namespace OpenTK_3D_Renderer
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Hello World!");

            using (Renderer renderer = new Renderer(800, 600, "OpenTK-Renderer"))
            {
                renderer.Run();
            }
        }
    }
}
