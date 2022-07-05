using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace Huatuo.Editor
{
    /// <summary>
    /// 这个类存放各种常量信息
    /// </summary>
    public class HTEditorConfig
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

        public static readonly string EditorBasePath = EditorApplication.applicationContentsPath;
        public static readonly string HuatuoIL2CPPPath = EditorBasePath + "/il2cpp/libil2cpp";
        public static readonly string HuatuoIL2CPPBackPath = EditorBasePath + "/il2cpp/libil2cpp_huatuo";
        public static readonly string Il2cppPath = Path.Combine(EditorBasePath, "il2cpp");
        public static readonly string MonoBleedingEdgePath = Path.Combine(EditorBasePath, "MonoBleedingEdge");
        public static readonly string Libil2cppPath = Path.Combine(Il2cppPath, "libil2cpp");
        public static readonly string Libil2cppOritinalPath = Path.Combine(Il2cppPath, "libil2cpp_original_unity");
        //public static readonly string HuatuoPath = Path.Combine(HuatuoIL2CPPPath, "huatuo");

        public static readonly string HuatuoCachDirName = "huatuo";
        public static readonly string CacheDirName = "cache";

        public string DownloadCache;
        public string HuatuoHelperPath;
        public string HuatuoLockerFilePath;
        public string HuatuoPath;
        public string HuatuoCacheData;

        public static string GetIl2cppBranchName()
        {
            switch (UnityVersionDigits)
            {
                case "2020.3.33":
                case "2020.3.35":
                    return "2020.3.33";
                case "2020.3.7":
                case "2020.3.9":
                    return "2020.3.7";
                case "2021.3.0":
                case "2021.3.1":
                case "2021.3.2":
                case "2021.3.3":
                case "2021.3.4":
                    return "2021.3.1";
            }
            return "";
        }
        private static HTEditorConfig instance = null;

        public static HTEditorConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HTEditorConfig();
                }

                return instance;
            }
        }
        public string GetVersionPath()
        {
            return Path.Combine(instance.HuatuoPath, HTEditorConfig.UnityVersionDigits, "version.json");
        }
        public string GetCachePath()
        {
            return Path.Combine(instance.HuatuoPath, HTEditorConfig.CacheDirName);
        }
        public string GetUnityDigitsPath()
        {
            return Path.Combine(instance.HuatuoPath, UnityVersionDigits);
        }
        public string GetUnityIl2cppPath()
        {
            return Path.Combine(instance.HuatuoPath, UnityVersionDigits, "il2cpp");
        }
        public string GetUnityLibil2cppPath()
        {
            return Path.Combine(instance.HuatuoPath, UnityVersionDigits, "il2cpp", "libil2cpp");
        }
        public string GetUnityHuatuoPath()
        {
            return Path.Combine(instance.HuatuoPath, UnityVersionDigits, "il2cpp", "libil2cpp", "huatuo");
        }

        public bool SetHuatuoDirectory(string path)
        {
            if (path == null || path.Length == 0)
            {
                return false;
            }

            try
            {
                instance.HuatuoPath = path;
                Directory.CreateDirectory(Path.Combine(HuatuoPath, CacheDirName));
                HTEditorInstaller.Instance.SaveCacheDir();
                HTEditorInstaller.Instance.Init();
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.LogError("缓存设置失败，请不要使用C盘路径做缓存");
                Debug.LogException(ex);
                return false;
            }
            Debug.Log($"HuatuoPath set = {HuatuoPath}");
            return true;
        }
        HTEditorConfig()
        {
            UnityFullVersion = InternalEditorUtility.GetFullUnityVersion();
            UnityVersionDigits = InternalEditorUtility.GetUnityVersionDigits();
            HuatuoPath = Path.Combine(HTEditorUtility.GetAppDataPath(), HuatuoCachDirName);
            HuatuoCacheData = Path.Combine(HuatuoPath, "cache.json");

            try
            {
                var data = File.ReadAllText(HuatuoCacheData, Encoding.UTF8);
                var d = JsonUtility.FromJson<HuatuoVersionDict>(data);
                if (!string.IsNullOrEmpty(d.UnityIl2cppDir))
                {
                    HuatuoPath = d.UnityIl2cppDir;
                }
            }
            catch (FileNotFoundException)
            {
                var tmp = new HuatuoVersionDict();
                tmp.UnityIl2cppDir = HuatuoPath;
                File.WriteAllText(HuatuoCacheData, EditorJsonUtility.ToJson(tmp, true), Encoding.UTF8);
            }

            HuatuoHelperPath = Path
                .GetFullPath(Path.Combine(HuatuoPath, UnityVersionDigits));
            HuatuoLockerFilePath = Path.Combine(HuatuoPath, ".locker");
            Directory.CreateDirectory(Path.Combine(HuatuoPath, UnityVersionDigits));
            Directory.CreateDirectory(Path.Combine(HuatuoPath, CacheDirName));
        }
    }
}
