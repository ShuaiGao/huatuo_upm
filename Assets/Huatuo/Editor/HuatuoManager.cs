using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using Huatuo.Editor.ThirdPart;
using System.Text;

// HuatuoManager.cs
//
// Author:
//   ldr123 (ldr12@163.com)
//

namespace Huatuo.Editor
{
    /// <summary>
    /// 这个类是Huatuo管理器，用于对Huatuo进行开关和更新等相关操作
    /// </summary>
    public class HuatuoManager : EditorWindow
    {
        private static readonly Vector2 WinSize = new Vector2(620f, 400);

        private bool m_bInitialized = false;

        private Logo m_logo = null;

        private HuatuoRemoteConfig m_verHuatuo_il2cpp = null;
        private HuatuoRemoteConfig m_verHuatuo = null;

        private HuatuoRemoteConfig m_verHuatuoBack_il2cpp = null;
        private HuatuoRemoteConfig m_verHuatuoBack = null;

        private bool m_bHasHuatuo = false;
        private bool m_bHasHuatoBack = false;

        private bool m_bHasIl2cpp = false;
        private bool m_bHasIl2cppBack = false;

        private bool m_bNeedUpgrade = false;


        private GUIStyle m_styleNormalFont = null;
        private GUIStyle m_styleWarningFont = null;
        private GUIStyle m_styleNormalBtn = null;
        private GUIStyle m_styleFooterBtn = null;
        private GUIStyle m_styleUninstallBtn = null;

        private EditorCoroutines.EditorCoroutine m_corFetchManifest = null;
        private EditorCoroutines.EditorCoroutine m_corUpgrade = null;

        private HuatuoRemoteConfig m_remoteConfig = null;

        private bool m_bVersionUnsported = false;

        private bool busying => m_corUpgrade != null || m_corFetchManifest != null;

        [MenuItem("HuaTuo/Manager...", false, 3)]
        public static void ShowManager()
        {
            var win = GetWindow<HuatuoManager>(true);
            win.titleContent = new GUIContent("Huatuo Manager");
            win.minSize = win.maxSize = WinSize;
            win.ShowUtility();
        }
        /*[MenuItem("HuaTuo/Install with github", false, 3)]
       public static void InstallByGithub()
       {
           var version = new HuatuoVersion();
           version.huatuoTag = "0.0.1";
           version.libil2cppTag = "2020.3.33-0.0.1";
           new Installer(true, version).Install();
       }
       //[MenuItem("HuaTuo/Install/gitee", false, 3)]
       //public static void InstallByGitee()
       //{
       //    var version = new HuatuoVersion();
       //    version.huatuoTag = "0.0.1";
       //    new Installer(false, version).Install();
       //}
*/

        private void OnEnable()
        {
            Init();
        }

        /// <summary>
        /// 检查给定目录下是否有Huatuo的版本文件
        /// </summary>
        /// <param name="basePath">待检查的根目录</param>
        /// <returns>如果存在版本文件，则返回相应的版本信息，否则返回空</returns>
        private static HuatuoRemoteConfig CheckVersion(string basePath)
        {
            var verFile = basePath + "/version.json";
            if (!File.Exists(verFile))
            {
                return null;
            }

            var txt = File.ReadAllText(verFile);
            if (string.IsNullOrEmpty(txt))
            {
                return null;
            }

            return JsonUtility.FromJson<HuatuoRemoteConfig>(txt);
        }

        /// <summary>
        /// 当版本发生变化后，重新加载版本信息
        /// </summary>
        private void ReloadVersion()
        {
            m_bNeedUpgrade = false;
            m_bHasHuatuo = Directory.Exists(Config.HuatuoIL2CPPPath) && Directory.Exists(Config.HuatuoPath);
            m_bHasHuatoBack = Directory.Exists(Config.HuatuoIL2CPPBackPath) && Directory.Exists(Config.HuatuoBackPath);
            m_bHasIl2cpp = Directory.Exists(Config.Il2cppPath);
            m_bHasIl2cppBack = Directory.Exists(Config.LibIl2cppBackPath);

            m_verHuatuo = null;
            m_verHuatuo_il2cpp = null;
            if (m_bHasHuatuo)
            {
                m_verHuatuo_il2cpp = CheckVersion(Config.HuatuoIL2CPPPath);
                m_verHuatuo = CheckVersion(Config.HuatuoPath);
                m_bHasHuatuo = m_verHuatuo != null && m_verHuatuo_il2cpp != null;
                if (!m_bHasHuatuo)
                {
                    m_verHuatuo_il2cpp = null;
                    m_verHuatuo = null;
                }
            }

            if (m_bHasHuatoBack)
            {
                m_verHuatuoBack_il2cpp = CheckVersion(Config.HuatuoIL2CPPBackPath);
                m_verHuatuoBack = CheckVersion(Config.HuatuoBackPath);
                m_bHasHuatoBack = m_verHuatuoBack != null && m_verHuatuoBack_il2cpp != null;
                if (!m_bHasHuatoBack)
                {
                    m_verHuatuoBack_il2cpp = null;
                    m_verHuatuoBack = null;
                }
            }

            Installer.Instance.Init();

            //if (m_remoteVerHuatuo != null && m_remoteVerHuatuoIl2Cpp != null && m_corUpgrade == null)
            //{
            //    Func<string, string, int> comp = (a, b) => Utility.CompareVersions(a, b);
            //    if (m_verHuatuo != null && m_verHuatuo_il2cpp != null)
            //    {
            //        m_bNeedUpgrade = comp(m_remoteVerHuatuoIl2Cpp.ver, m_verHuatuo_il2cpp.ver) > 0 ||
            //                         comp(m_remoteVerHuatuo.ver, m_verHuatuo.ver) > 0;
            //    }
            //    else if (m_verHuatuoBack != null && m_verHuatuoBack_il2cpp != null)
            //    {
            //        m_bNeedUpgrade = comp(m_remoteVerHuatuoIl2Cpp.ver, m_verHuatuoBack_il2cpp.ver) > 0 ||
            //                         comp(m_remoteVerHuatuo.ver, m_verHuatuoBack.ver) > 0;
            //    }
            //}
        }

        /// <summary>
        /// 初始化基础环境
        /// </summary>
        private void Init()
        {
            if (m_bInitialized)
            {
                return;
            }

            Config.Init();

            m_logo = new Logo();
            m_logo.Init(WinSize);

            ReloadVersion();

            if (m_corFetchManifest != null)
            {
                this.StopCoroutine(m_corFetchManifest.routine);
            }

            m_corFetchManifest = this.StartCoroutine(GetSdkVersions(true, () => { ReloadVersion(); }));

            m_bInitialized = true;
        }

        private void OnDestroy()
        {
            this.StopAllCoroutines();
        }

        /// <summary>
        /// 初始化各组件显示的样式信息
        /// </summary>
        private void CheckStyle()
        {
            if (m_styleNormalFont != null)
            {
                return;
            }

            m_styleNormalFont = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                richText = true,
                fontSize = 14,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(4, 0, 0, 0)
            };

            m_styleWarningFont = new GUIStyle(m_styleNormalFont)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };

            m_styleNormalBtn = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(0, 0, 5, 5),
                wordWrap = true,
                richText = true
            };

            m_styleFooterBtn = new GUIStyle(m_styleNormalBtn)
            {
                padding = new RectOffset(0, 0, 10, 10)
            };
            m_styleUninstallBtn = new GUIStyle(m_styleFooterBtn)
            {
                padding = new RectOffset(0, 0, 10, 10)
            };
        }

        /// <summary>
        /// 启用/禁用Huatuo插件
        /// </summary>
        /// <param name="enable">启用还是禁用</param>
        private void EnableOrDisable(bool enable)
        {
            Action<string> endFunc = ret =>
            {
                if (!string.IsNullOrEmpty(ret))
                {
                    Debug.LogError(ret);
                    EditorUtility.DisplayDialog("错误", ret, "ok");
                    return;
                }

                ReloadVersion();
            };
            if (enable)
            {
                Installer.Enable(ret => { endFunc?.Invoke(ret); });
            }
            else
            {
                Installer.Disable(ret => { endFunc?.Invoke(ret); });
            }
        }

        /// <summary>
        /// 升级中...
        /// </summary>
        private void Upgrade()
        {
            //m_corUpgrade = this.StartCoroutine(install());
            install();
        }
        private void install()
        {
            var version = new InstallVersion();
            version.huatuoTag = m_remoteConfig.huatuo_recommend_version;
            version.il2cppTag = m_remoteConfig.GetIl2cppRecommendVersion();
            Installer.Instance.Install(true, version);
        }


        private IEnumerator Upgrading()
        {
            var itor = GetSdkVersions(true, null);
            while (itor.MoveNext())
            {
                yield return itor.Current;
            }

            ReloadVersion();

            var haserr = false;
            do
            {
                if (!m_bNeedUpgrade)
                {
                    break;
                }

                if (m_bHasHuatuo)
                {
                    EnableOrDisable(false);
                }

                //local url temp hash unzippath, versionstr
                var needDownload =
                    new Dictionary<string, (string url, string tmpfile, string hash, string unzipPath, string verstr)
                    >();
                //if (!m_remoteConfig.Compare(m_verHuatuo_il2cpp))
                //{
                //    var il2cpp_version = m_remoteConfig.GetIl2cppRecommendVersion();
                //    needDownload.Add(Config.HuatuoIL2CPPBackPath,
                //        ($"{Config.IL2CPPManifestUrl}/{il2cpp_version}.zip",
                //            $"{Config.DownloadCache}/{il2cpp_version}.zip", m_remoteVerHuatuoIl2Cpp.hash,
                //            $"{Config.DownloadCache}/{il2cpp_version}_dir",
                //            JsonUtility.ToJson(m_remoteVerHuatuoIl2Cpp)));
                //}
                //if (!m_remoteVerHuatuo.Compare(m_verHuatuo))
                //{
                //    needDownload.Add(Config.HuatuoBackPath,
                //        ($"{Config.HuatuoManifestUrl}/{m_remoteVerHuatuo.ver}.zip",
                //            $"{Config.DownloadCache}/huatuo_{m_remoteVerHuatuo.ver}.zip", m_remoteVerHuatuo.hash,
                //            $"{Config.DownloadCache}/huatuo_{m_remoteVerHuatuo.ver}_dir",
                //            JsonUtility.ToJson(m_remoteVerHuatuo)));
                //}

                var downloading = 0;
                foreach (var kv in needDownload)
                {
                    downloading++;
                    itor = Utility.DownloadFile(kv.Value.url, kv.Value.tmpfile,
                        p => { EditorUtility.DisplayProgressBar("下载中...", $"{downloading}/{needDownload.Count}", p); },
                        ret =>
                        {
                            if (!string.IsNullOrEmpty(ret))
                            {
                                haserr = true;
                                EditorUtility.DisplayDialog("错误", $"下载{kv.Value.Item1}出错.\n{ret}", "ok");
                            }
                        }, false);
                    while (itor.MoveNext())
                    {
                        yield return itor.Current;
                    }

                    if (haserr)
                    {
                        break;
                    }
                }

                if (haserr)
                {
                    break;
                }

                var unzipCnt = 0;
                //check files
                foreach (var kv in needDownload)
                {
                    unzipCnt++;
                    if (!File.Exists(kv.Value.tmpfile))
                    {
                        EditorUtility.DisplayDialog("错误", $"下载的文件{kv.Value.tmpfile}不存在", "ok");
                    }
                    else if (MD5.ComputeFileMD5(kv.Value.tmpfile).ToLower() != kv.Value.hash)
                    {
                        EditorUtility.DisplayDialog("错误", $"下载的文件{kv.Value.tmpfile} hash不匹配，请重新下载", "ok");
                    }
                    else
                    {
                        var cnt = 0;
                        itor = Utility.UnzipAsync(kv.Value.tmpfile, kv.Value.unzipPath, b => { cnt = b; }, p =>
                        {
                            EditorUtility.DisplayProgressBar($"解压中...{unzipCnt}/{needDownload.Count}", $"{p}/{cnt}",
                                (float)p / cnt);
                        }, () => { }, () => { haserr = true; });
                        while (itor.MoveNext())
                        {
                            yield return itor.Current;
                        }
                    }
                }

                if (haserr)
                {
                    break;
                }

                foreach (var kv in needDownload)
                {
                    if (Directory.Exists(kv.Key))
                    {
                        Directory.Delete(kv.Key, true);
                    }

                    var err = Utility.Mv(kv.Value.unzipPath, kv.Key);
                    if (!string.IsNullOrEmpty(err))
                    {
                        Debug.LogError(err);
                        haserr = true;
                    }

                    File.WriteAllText(kv.Key + "/version.json", kv.Value.verstr);
                }

                if (haserr)
                {
                    break;
                }

                foreach (var val in needDownload.Values)
                {
                    if (File.Exists(val.tmpfile))
                    {
                        File.Delete(val.tmpfile);
                    }
                }

                itor = GetSdkVersions(true, null);
                while (itor.MoveNext())
                {
                    yield return itor.Current;
                }

                EnableOrDisable(true);
            } while (false);

            EditorUtility.ClearProgressBar();

            if (haserr)
            {
                EditorUtility.DisplayDialog("错误", "发生了一些错误，请看log!", "ok");
            }

            m_corUpgrade = null;
        }

        /// <summary>
        /// 安装版本
        /// </summary>
        /// <param name="strHuatuoFile">打包后的huatuo文件</param>
        /// <param name="strIl2cppFile">打包后的il2cpp文件</param>
        private void Install(string strHuatuoFile, string strIl2cppFile)
        {

        }

        /// <summary>
        /// 对Huatuo环境进行检查
        /// </summary>
        private void InstallOrUpgradeGui()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color=white>Unity3D:\t{Config.UnityFullVersion}</color>", m_styleNormalFont);
            if (GUILayout.Button("修改缓存路径", m_styleNormalBtn))
            {
                ChangeCacheDir();
            }
            GUILayout.EndHorizontal();

            if (!m_bHasIl2cpp)
            {
                GUILayout.Label("<color=red>Scripting Backend(IL2CPP) is not installed!</color>", m_styleWarningFont);
                return;
            }

            if (m_bVersionUnsported)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<color=red>你的Unity版本不被支持，请使用受支持的版本!</color>", m_styleWarningFont);
                if (GUILayout.Button("查看受支持的版本", m_styleNormalBtn))
                {
                    Application.OpenURL(Config.SupportedVersion);
                }

                GUILayout.EndHorizontal();
                return;
            }


            var strMsg = "";
            var strColor = "<color=green>";
            if (m_bHasHuatuo)
            {
                strMsg = $"Huatuo:{m_verHuatuo.ver}\tIL2CPP:{m_verHuatuo_il2cpp.ver}";
            }
            else if (m_bHasHuatoBack)
            {
                strColor = "<color=grey>";
                strMsg = $"Huatuo:{m_verHuatuoBack.ver}\tIL2CPP:{m_verHuatuoBack_il2cpp.ver}";
            }

            var installVersion = Installer.Instance.huatuoVersion;
            if (installVersion.HuatuoTag.Length > 0)
            {
                strMsg = $"Huatuo: {installVersion.HuatuoTag}\tIL2CPP: {installVersion.Il2cppTag}";
            }


            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(strMsg))
            {
                strMsg = $"{strColor}已安装:\t{strMsg}</color>";
                GUILayout.Label(strMsg, m_styleNormalFont);
                EditorGUI.BeginDisabledGroup(true);
                if (GUILayout.Button(m_bHasHuatuo ? "禁用" : "启用", m_styleNormalBtn))
                {
                    EnableOrDisable(!m_bHasHuatuo);
                }

                if (GUILayout.Button("卸载", m_styleNormalBtn))
                {
                    if (m_bHasHuatoBack)
                    {
                        EnableOrDisable(true);
                    }

                    Installer.Uninstall(ret =>
                    {
                        EditorUtility.DisplayDialog("提示", $"卸载完毕。{(string.IsNullOrEmpty(ret) ? " " : ret)}", "ok");

                        ReloadVersion();
                    });
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                m_bNeedUpgrade = true;
                GUILayout.Label("未安装", m_styleNormalFont);
            }


            //EditorGUI.BeginDisabledGroup(true);
            //if (GUILayout.Button(string.IsNullOrEmpty(strMsg) ? "手动安装" : "手动更新", m_styleNormalBtn))
            //{
            //    Manual();
            //}
            //EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            if (m_corFetchManifest != null)
            {
                GUILayout.Label("正在获取配置文件...", m_styleNormalFont);
            }

            if (m_remoteConfig != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"推荐版本:\tHuatuo: {m_remoteConfig.huatuo_recommend_version}\tIL2CPP: {m_remoteConfig.GetIl2cppRecommendVersion()}",
                    m_styleNormalFont);

                if (m_bNeedUpgrade && m_corUpgrade == null)
                {
                    if (GUILayout.Button(string.IsNullOrEmpty(strMsg) ? "安装" : "更新", m_styleNormalBtn))
                    {
                        Upgrade();
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        private void ChangeCacheDir()
        {
            var cachePath = EditorUtility.OpenFolderPanel("请选择缓存路径", PackageManager.Instance.CacheBasePath, "");
            if (cachePath.Length == 0)
            {
                return;
            }
            if (!Directory.Exists(cachePath))
            {
                EditorUtility.DisplayDialog("错误", "路径不存在!", "ok");
                return;
            }
            PackageManager.Instance.SetCacheDirectory(cachePath);
        }
        /// <summary>
        /// 手动安装
        /// </summary>
        private void Manual()
        {
            var strhuatuoFile = EditorUtility.OpenFilePanel("请选择Huatuo.zip", "", "*.zip");
            if (string.IsNullOrEmpty(strhuatuoFile) || !File.Exists(strhuatuoFile))
            {
                EditorUtility.DisplayDialog("错误", "请选择正确的文件!", "ok");
                return;
            }

            var stril2CppFile = EditorUtility.OpenFilePanel("请选择Il2cpp.zip", "", "*.zip");
            if (string.IsNullOrEmpty(stril2CppFile) || !File.Exists(stril2CppFile))
            {
                EditorUtility.DisplayDialog("错误", "请选择正确的文件!", "ok");
                return;
            }

            Install(strhuatuoFile, stril2CppFile);
        }

        private IEnumerator HttpRequest(string url, bool silent, Action<HuatuoRemoteConfig> callback)
        {
            Debug.Log($"Fetching {url}");
            using var www = new UnityWebRequest(url)

            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 10, // seconds
            };
            yield return www.SendWebRequest();

            HuatuoRemoteConfig ret = null;
            do
            {
                if (!string.IsNullOrEmpty(www.error))
                {
                    Debug.LogError(www.error);
                    if (!silent)
                    {
                        EditorUtility.DisplayDialog("错误", $"【1】获取远程版本信息错误。\n[{www.error}]", "ok");
                    }

                    //m_bVersionUnsported = www.error.Contains("404") && www.error.Contains("Found");

                    break;
                }

                var json = www.downloadHandler.text;
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError("Unable to retrieve SDK version manifest.  Showing installed SDKs only.");
                    if (!silent)
                    {
                        EditorUtility.DisplayDialog("错误", $"【2】获取远程版本信息错误。", "ok");
                    }

                    break;
                }

                ret = JsonUtility.FromJson<HuatuoRemoteConfig>(json);
                if (ret == null)
                {
                    Debug.LogError("Unable to retrieve SDK version manifest.  Showing installed SDKs only.");
                    if (!silent)
                    {
                        EditorUtility.DisplayDialog("错误", $"【2】获取远程版本信息错误。", "ok");
                    }

                    break;
                }
            } while (false);

            callback?.Invoke(ret);
        }

        /// <summary>
        /// 获取远程的版本信息
        /// </summary>
        /// <param name="silent">静默获取</param>
        /// <param name="callback">获取后的回调</param>
        /// <returns>迭代器</returns>
        private IEnumerator GetSdkVersions(bool silent, Action callback)
        {
            m_bVersionUnsported = false;

            // Wait one frame so that we don't try to show the progress bar in the middle of OnGUI().
            yield return null;

            var itor = HttpRequest(Config.urlVersionConfig, silent, r1 =>
            {
                m_remoteConfig = r1;
                var unityVersion = InternalEditorUtility.GetUnityVersionDigits();
                if (!m_remoteConfig.unity_version.Contains(unityVersion))
                {
                    m_bVersionUnsported = true;
                }
            });
            while (itor.MoveNext())
            {
                yield return itor.Current;
            }
            m_corFetchManifest = null;
            callback?.Invoke();
        }

        /// <summary>
        /// 通用按钮的展示
        /// </summary>
        private void FooterGui()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Website", m_styleFooterBtn))
            {
                Application.OpenURL(Config.WebSite);
            }

            if (GUILayout.Button("Document", m_styleFooterBtn))
            {
                Application.OpenURL(Config.Document);
            }

            //if (GUILayout.Button("Changelog", m_styleFooterBtn))
            //{
            //    Application.OpenURL(Config.Changelog);
            //}

            if (GUILayout.Button("Check Updates", m_styleFooterBtn))
            {
                this.CheckUpdate();
            }
            if (GUILayout.Button("Install", m_styleFooterBtn))
            {
                // TODO 选择安装版本， 可以单独安装huatuo或il2cpp
                // 使用推荐版本安装
                var version = new InstallVersion();
                version.huatuoTag = m_remoteConfig.huatuo_recommend_version;
                version.il2cppTag = m_remoteConfig.GetIl2cppRecommendVersion();
                Installer.Instance.Install(true, version);
            }

            EditorGUI.BeginDisabledGroup(busying);
            //if (GUILayout.Button("强制修复", m_styleFooterBtn))
            //{
            //    if (EditorUtility.DisplayDialog("警告", "只有更新失败的时候才会使用强制修复功能，确定强制修复吗？", "是", "不用了"))
            //    {
            //        EditorUtility.DisplayDialog("------", "暂未实现~~", "ok");
            //    }
            //}

            if (GUILayout.Button("Uninstall", m_styleFooterBtn))
            {
                Uninstaller.DoUninstall();
            }


            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }
        private void CheckUpdate()
        {
            if (m_corUpgrade != null)
            {
                return;
            }

            if (m_corFetchManifest != null)
            {
                this.StopCoroutine(m_corFetchManifest.routine);
                m_corFetchManifest = null;
            }

            m_corFetchManifest = this.StartCoroutine(GetSdkVersions(false, () =>
            {
                ReloadVersion();
                var s = JsonUtility.ToJson(m_remoteConfig, true);
                Debug.Log($"remote version config: {s}");
                Debug.Log($"huatuo version: {m_remoteConfig.huatuo_recommend_version}");
                Debug.Log($"il2cpp version: {m_remoteConfig.il2cpp_recommend_version}");
                EditorUtility.DisplayDialog("", m_bNeedUpgrade ? "有新版本了" : "已经是最新版本了", "OK");
            }));
        }

        private void OnGUI()
        {
            CheckStyle();

            if (m_logo != null)
            {
                m_logo.OnGUI();

                GUILayout.Space(m_logo.ImgHeight + 16f);
            }

            EditorGUI.BeginDisabledGroup(busying);

            InstallOrUpgradeGui();

            GUILayout.Space(80);

            EditorGUI.EndDisabledGroup();

            FooterGui();
        }
    }
}
