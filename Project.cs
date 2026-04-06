using System;
using System.IO;

namespace OpenTK_3D_Renderer
{
    static class Project
    {
        public static readonly string Root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../"));
        public static readonly string Resources = Path.Combine(Root, "Resources/");
        public const string DefaultTex = "defaultTex.png";
    }
}
