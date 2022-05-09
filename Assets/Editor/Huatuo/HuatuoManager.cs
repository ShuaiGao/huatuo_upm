using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using Editor.Huatuo.ThirdPart;

// HuatuoManager.cs
//
// Author:
//   ldr123 (ldr12@163.com)
//

namespace Assets.Editor.Huatuo
{
    /// <summary>
    /// 这个类是Huatuo管理器，用于对Huatuo进行开关和更新等相关操作
    /// </summary>
    public class HuatuoManager : EditorWindow
    {
        private static readonly Vector2 WinSize = new Vector2(620f, 650f);

        private bool m_bInitialized = false;

        private Texture2D m_texHeaderImg = null;
        private Rect m_rtHeader = Rect.zero;

        private HuatuoVersion m_verHuatuo_il2cpp = null;
        private HuatuoVersion m_verHuatuo = null;

        private HuatuoVersion m_verHuatuoBack_il2cpp = null;
        private HuatuoVersion m_verHuatuoBack = null;

        private bool m_bHasHuatuo = false;
        private bool m_bHasHuatoBack = false;

        private bool m_bHasIl2cpp = false;
        private bool m_bHasIl2cppBack = false;

        private bool m_bNeedUpgrade = false;
        private bool m_bUpgrading = false;


        private GUIStyle m_styleNormalFont = null;
        private GUIStyle m_styleWarningFont = null;
        private GUIStyle m_styleNormalBtn = null;
        private GUIStyle m_styleFooterBtn = null;

        private EditorCoroutines.EditorCoroutine m_corFetchManifest = null;
        private RemoteHuatuoVersion m_verRemote = null;
        private bool m_bFetchingManifest = false;
        private bool m_bVersionUnsported = false;

        [MenuItem("HuaTuo/Manager...", false, 3)]
        public static void ShowManager()
        {
            var win = GetWindow<HuatuoManager>(true);
            win.titleContent = new GUIContent("Huatuo Manager");
            win.minSize = win.maxSize = WinSize;
            win.ShowUtility();
        }
        [MenuItem("HuaTuo/Install with github", false, 3)]
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

        private void OnEnable()
        {
            Init();
        }

        /// <summary>
        /// 检查给定目录下是否有Huatuo的版本文件
        /// </summary>
        /// <param name="basePath">待检查的根目录</param>
        /// <returns>如果存在版本文件，则返回相应的版本信息，否则返回空</returns>
        private static HuatuoVersion CheckVersion(string basePath)
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

            return JsonUtility.FromJson<HuatuoVersion>(txt);
        }

        /// <summary>
        /// 当版本发生变化后，重新加载版本信息
        /// </summary>
        private void ReloadVersion()
        {
            m_bHasHuatuo = Directory.Exists(Config.HuatuoIL2CPPPath) && Directory.Exists(Config.HuatuoPath);
            m_bHasHuatoBack = Directory.Exists(Config.HuatuoIL2CPPBackPath) && Directory.Exists(Config.HuatuoBackPath);
            m_bHasIl2cpp = Directory.Exists(Config.LibIl2cppPath);
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

            if (m_texHeaderImg == null)
            {
                m_texHeaderImg = Logo.LogoImage;
                var tmpHeight = WinSize.x / m_texHeaderImg.width * m_texHeaderImg.height;
                m_rtHeader = m_texHeaderImg ? new Rect(0, 0, WinSize.x, tmpHeight) : Rect.zero;
            }

            ReloadVersion();

            if (m_corFetchManifest != null)
            {
                this.StopCoroutine(m_corFetchManifest.routine);
            }

            m_bFetchingManifest = true;
            m_corFetchManifest = this.StartCoroutine(GetSDKVersions(true, () =>
            {
                m_bFetchingManifest = false;
                ReloadVersion();
            }));

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
        }

        /// <summary>
        /// 启用/禁用Huatuo插件
        /// </summary>
        /// <param name="enable">启用还是禁用</param>
        private void EnableOrDisableHuatuo(bool enable)
        {
            ReloadVersion();
        }

        /// <summary>
        /// 升级中...
        /// </summary>
        private void Upgrade()
        {
            m_bUpgrading = true;

            m_bUpgrading = false;
        }

        /// <summary>
        /// 对Huatuo环境进行检查
        /// </summary>
        private void InstallOrUpgradeGui()
        {
            if (!m_bHasIl2cpp)
            {
                GUILayout.Label("<color=red>Scripting Backend(IL2CPP) is not installed!</color>", m_styleWarningFont);
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


            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(strMsg))
            {
                strMsg = $"{strColor}已安装:\t{strMsg}</color>";
                GUILayout.Label(strMsg, m_styleNormalFont);
                if (GUILayout.Button(m_bHasHuatuo ? "禁用" : "启用", m_styleNormalBtn))
                {
                    EnableOrDisableHuatuo(!m_bHasHuatuo);
                }
            }

            if (GUILayout.Button("手动更新", m_styleNormalBtn))
            {
                Manual();
            }

            GUILayout.EndHorizontal();

            if (m_bFetchingManifest)
            {
                GUILayout.Label("正在获取配置文件...", m_styleNormalFont);
            }

            if (m_verRemote != null)
            {
                GUILayout.Label($"最新版本:\tHuatuo: {m_verRemote.huatuo_recommend_version}\tIL2CPP: {m_verRemote.il2cpp_recommend_version}");

                if (m_bNeedUpgrade && !m_bUpgrading)
                {
                    if (GUILayout.Button("更新", m_styleNormalBtn))
                    {
                        Upgrade();
                    }
                }
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

        /// <summary>
        /// 安装版本
        /// </summary>
        /// <param name="strHuatuoFile">打包后的huatuo文件</param>
        /// <param name="strIl2cppFile">打包后的il2cpp文件</param>
        /// <returns>迭代器</returns>
        private void Install(string strHuatuoFile, string strIl2cppFile)
        {

        }

        /// <summary>
        /// 获取远程的版本信息
        /// </summary>
        /// <param name="silent">静默获取</param>
        /// <param name="callback">获取后的回调</param>
        /// <returns>迭代器</returns>
        private IEnumerator GetSDKVersions(bool silent, Action callback)
        {
            m_bVersionUnsported = false;
            m_bFetchingManifest = true;
            m_verRemote = null;
            // Wait one frame so that we don't try to show the progress bar in the middle of OnGUI().
            yield return null;

        //      //version.json
        //https://focus-creative-games.github.io/focus-creative-games/version.json
            using var www = new UnityWebRequest(Config.ManifestBaseURL)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 10, // seconds
            };
            yield return www.SendWebRequest();

            do
            {
                if (!string.IsNullOrEmpty(www.error))
                {
                    Debug.LogError(www.error);
                    if (!silent)
                    {
                        EditorUtility.DisplayDialog("错误", $"【1】获取远程版本信息错误。\n[{www.error}]", "ok");
                    }

                    m_bVersionUnsported = www.error.Contains("404") && www.error.Contains("Found");

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

                m_verRemote = JsonUtility.FromJson<RemoteHuatuoVersion>(json);
                if (m_verRemote == null)
                {
                    Debug.LogError("Unable to retrieve SDK version manifest.  Showing installed SDKs only.");
                    if (!silent)
                    {
                        EditorUtility.DisplayDialog("错误", $"【2】获取远程版本信息错误。", "ok");
                    }

                    break;
                }
            } while (false);

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
            if (GUILayout.Button("Install", m_styleFooterBtn))
            {
                // TODO 选择安装版本， 可以单独安装huatuo或il2cpp
                // 使用推荐版本安装
                var version = new HuatuoVersion();
                version.huatuoTag = m_verRemote.huatuo_recommend_version;
                version.libil2cppTag = m_verRemote.il2cpp_recommend_version;
                new Installer(true, version).Install();
            }

            if (GUILayout.Button("Check Updates", m_styleFooterBtn))
            {
                if (m_bUpgrading)
                {
                    return;
                }

                if (m_corFetchManifest != null)
                {
                    this.StopCoroutine(m_corFetchManifest.routine);
                    m_corFetchManifest = null;
                }

                m_corFetchManifest = this.StartCoroutine(GetSDKVersions(false, () =>
                {
                    m_bFetchingManifest = false;
                    var tmpVersion = new RemoteHuatuoVersion(m_verRemote);
                    ReloadVersion();

                    if (!tmpVersion.Compare(m_verRemote))
                    {
                        EditorUtility.DisplayDialog("", "已经是最新版本了", "OK");
                        return;
                    }

                    Upgrade();
                }));
            }

            GUILayout.EndHorizontal();
        }

        private void OnGUI()
        {
            CheckStyle();

            GUI.DrawTexture(m_rtHeader, m_texHeaderImg, ScaleMode.StretchToFill, true);
            GUILayout.Space(m_rtHeader.height + 8f);
            GUILayout.Label($"<color=white>Unity3D:\t{Config.UnityFullVersion}</color>", m_styleNormalFont);

            InstallOrUpgradeGui();

            FooterGui();
        }
    }
}
