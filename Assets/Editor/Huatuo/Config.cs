using System.IO;
using UnityEditor;
using UnityEditorInternal;

// Config.cs
//
// Author:
//   ldr123 (ldr12@163.com)
//

namespace Assets.Editor.Huatuo
{
    /// <summary>
    /// 这个类存放各种常量信息
    /// </summary>
    public static class Config
    {
        public static string UnityFullVersion = "";
        
        public static string ManifestBaseURL = "https://ldr123.github.io/manifest/";
        private static readonly string WebSiteBase = "https://github.com/focus-creative-games/huatuo";
        public static readonly string WebSite = WebSiteBase;
        public static readonly string Document = WebSiteBase;
        public static readonly string Changelog = WebSiteBase;
        public static readonly string SupportedVersion = WebSiteBase + "/wiki/support_versions";

        private static readonly string EditorBasePath = EditorApplication.applicationContentsPath;
        public static readonly string HuatuoIL2CPPPath = Path.Combine(EditorBasePath, "il2cpp/libil2cpp/");
        public static readonly string HuatuoPath = Path.Combine(HuatuoIL2CPPPath, "huatuo/");
        public static readonly string HuatuoIL2CPPBackPath = Path.Combine(EditorBasePath, "il2cpp/libil2cpp_huatuo/");
        public static readonly string HuatuoBackPath = Path.Combine(HuatuoIL2CPPBackPath, "huatuo/");
        public static readonly string LibIl2cppPath = Path.Combine(EditorBasePath, "il2cpp/libil2cpp/");
        public static readonly string LibIl2cppBackPath = Path.Combine(EditorBasePath, "il2cpp/libil2cpp_back/");

        public static void Init()
        {
            ManifestBaseURL = ManifestBaseURL + InternalEditorUtility.GetUnityVersionDigits() + ".json";
            
            UnityFullVersion = InternalEditorUtility.GetFullUnityVersion();
        }
    }
}
