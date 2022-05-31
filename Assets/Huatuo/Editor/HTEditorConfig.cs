using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace Huatuo.Editor
{
    /// <summary>
    /// 这个类存放各种常量信息
    /// </summary>
    public static class HTEditorConfig
    {
        public static string UnityFullVersion = "";
        public static string UnityVersionDigits = "";
        public static string HEAD = "head";

        //public static readonly string libil2cppPrefixGitee = "https://gitee.com/juvenior/il2cpp_huatuo/repository/archive";
        public static readonly string libil2cppPrefixGithub = "https://github.com/pirunxi/il2cpp_huatuo/archive/refs/heads";
        //public static readonly string huatuoPrefixGitee = "https://gitee.com/focus-creative-games/huatuo/repository/archive";
        public static readonly string huatuoPrefixGithub = "https://github.com/focus-creative-games/huatuo/archive/refs/heads/";
        public static readonly string libil2cppTagPrefixGithub = "https://github.com/pirunxi/il2cpp_huatuo/archive/refs/tags";
        public static readonly string huatuoTagPrefixGithub = "https://github.com/focus-creative-games/huatuo/archive/refs/tags";
        public static readonly string urlVersionConfig = "https://focus-creative-games.github.io/huatuo_upm/Doc/version.json";
        public static readonly string urlHuatuoCommits = "https://api.github.com/repos/focus-creative-games/huatuo/commits";
        public static readonly string urlHuatuoTags = "https://api.github.com/repos/focus-creative-games/huatuo/tags";
        public static readonly string urlIl2cppBranchs = "https://api.github.com/repos/pirunxi/il2cpp_huatuo/branches";

        private static readonly string WebSiteBase = "https://github.com/focus-creative-games/huatuo";
        public static readonly string WebSite = WebSiteBase;
        public static readonly string Document = "https://focus-creative-games.github.io/huatuo/";
        public static readonly string Changelog = WebSiteBase;
        public static readonly string SupportedVersion = WebSiteBase + "/wiki/support_versions";

        private static readonly string EditorBasePath = EditorApplication.applicationContentsPath;
        public static readonly string HuatuoIL2CPPPath = EditorBasePath + "/il2cpp/libil2cpp";
        public static readonly string HuatuoIL2CPPBackPath = EditorBasePath + "/il2cpp/libil2cpp_huatuo";
        public static readonly string Il2cppPath = Path.Combine(EditorBasePath, "il2cpp");
        public static readonly string MonoBleedingEdgePath = Path.Combine(EditorBasePath, "MonoBleedingEdge");
        public static readonly string Libil2cppPath = Path.Combine(Il2cppPath, "libil2cpp");
        public static readonly string Libil2cppOritinalPath = Path.Combine(Il2cppPath, "libil2cpp_original_unity");
        public static readonly string HuatuoPath = Path.Combine(HuatuoIL2CPPPath, "huatuo");

        public static string DownloadCache = "";

        public static string HuatuoVersionPath =
         Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, ".huatuo");

        public static string HuatuoHelperPath = "";

        public static string GetIl2cppBranchName()
        {
            switch (UnityVersionDigits)
            {
                case "2020.3.33":
                    return "2020.3.33";
                case "2020.3.7":
                case "2020.3.9":
                    return "2020.3.7";
                case "2021.3.0":
                case "2021.3.1":
                    return "2021.3.1";
            }
            return "";
        }

        public static void Init()
        {
            UnityFullVersion = InternalEditorUtility.GetFullUnityVersion();
            UnityVersionDigits = InternalEditorUtility.GetUnityVersionDigits();

            HuatuoHelperPath = Path
                .GetFullPath(Path.Combine(HTEditorUtility.GetAppDataPath(), ".huatuo", UnityVersionDigits))
                .Replace('\\', '/');
            HTEditorUtility.EnsureFilePath(HuatuoHelperPath);
            //HuatuoVersionPath =Path.Combine(HuatuoHelperPath, "version.json");
        }
    }
}
