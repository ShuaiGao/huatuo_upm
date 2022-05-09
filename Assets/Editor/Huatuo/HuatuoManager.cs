using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine; 
    
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
        private static readonly Vector2 WinSize = new Vector2(620f, 650f);
        private static string UnityFullVersion = "";
        private static string UnityVersion = "";

        private static readonly string EditorBasePath = EditorApplication.applicationContentsPath;
        private static readonly string HuatuoIL2CPPPath = Path.Combine(EditorBasePath, "il2cpp/libil2cpp/");
        private static readonly string HuatuoPath = Path.Combine(HuatuoIL2CPPPath, "huatuo/");
        private static readonly string HuatuoIL2CPPBackPath = Path.Combine(EditorBasePath, "il2cpp/libil2cpp_huatuo/");
        private static readonly string HuatuoBackPath = Path.Combine(HuatuoIL2CPPBackPath, "huatuo/");
        private static readonly string LibIl2cppPath = Path.Combine(EditorBasePath, "il2cpp/libil2cpp/");
        private static readonly string LibIl2cppBackPath = Path.Combine(EditorBasePath, "il2cpp/libil2cpp_back/");

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

        private GUIStyle m_styleNormalFont = null;
        private GUIStyle m_styleWarningFont = null;
        private GUIStyle m_styleNormalBtn = null;
        private GUIStyle m_styleFooterBtn = null;


        [MenuItem("HuaTuo/Manager...", false, 3)]
        public static void ShowManager()
        {
            var win = GetWindow<HuatuoManager>(true);
            win.titleContent = new GUIContent("Huatuo Manager");
            win.minSize = win.maxSize = WinSize;
            win.ShowUtility();
        }

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
            m_bHasHuatuo = Directory.Exists(HuatuoIL2CPPPath) && Directory.Exists(HuatuoPath);
            m_bHasHuatoBack = Directory.Exists(HuatuoIL2CPPBackPath) && Directory.Exists(HuatuoBackPath);
            m_bHasIl2cpp = Directory.Exists(LibIl2cppPath);
            m_bHasIl2cppBack = Directory.Exists(LibIl2cppBackPath);

            m_verHuatuo = null;
            m_verHuatuo_il2cpp = null;
            if (m_bHasHuatuo)
            {
                m_verHuatuo_il2cpp = CheckVersion(HuatuoIL2CPPPath);
                m_verHuatuo = CheckVersion(HuatuoPath);
                m_bHasHuatuo = m_verHuatuo != null && m_verHuatuo_il2cpp != null;
                if (!m_bHasHuatuo)
                {
                    m_verHuatuo_il2cpp = null;
                    m_verHuatuo = null;
                }
            }

            if (m_bHasHuatoBack)
            {
                m_verHuatuoBack_il2cpp = CheckVersion(HuatuoIL2CPPBackPath);
                m_verHuatuoBack = CheckVersion(HuatuoBackPath);
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

            UnityFullVersion = InternalEditorUtility.GetFullUnityVersion();
            UnityVersion = InternalEditorUtility.GetUnityVersionDigits();

            if (m_texHeaderImg == null)
            {
                m_texHeaderImg = Logo.LogoImage;
                var tmpHeight = WinSize.x / m_texHeaderImg.width * m_texHeaderImg.height;
                m_rtHeader = m_texHeaderImg ? new Rect(0, 0, WinSize.x, tmpHeight) : Rect.zero;
            }

            ReloadVersion();

            m_bInitialized = true;
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

            if (!string.IsNullOrEmpty(strMsg))
            {
                strMsg = $"{strColor}已安装:\t{strMsg}</color>";
                GUILayout.Space(8f);
                GUILayout.BeginHorizontal();
                GUILayout.Label(strMsg, m_styleNormalFont);
                if (GUILayout.Button(m_bHasHuatuo ? "禁用" : "启用", m_styleNormalBtn))
                {
                    EnableOrDisableHuatuo(!m_bHasHuatuo);
                }
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 通用按钮的展示
        /// </summary>
        private void FooterGui()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Website", m_styleFooterBtn))
            {
                Application.OpenURL("https://github.com/focus-creative-games/huatuo");
            }
            
            if (GUILayout.Button("Document", m_styleFooterBtn))
            {
                Application.OpenURL("https://github.com/focus-creative-games/huatuo");
            }
            
            if (GUILayout.Button("Changelog", m_styleFooterBtn))
            {
                Application.OpenURL("https://github.com/focus-creative-games/huatuo");
            }
            
            if (GUILayout.Button("Check Updates", m_styleFooterBtn))
            {
                Application.OpenURL("https://github.com/focus-creative-games/huatuo");
            }
            GUILayout.EndHorizontal();
        }

        private void OnGUI()
        {
            CheckStyle();
            
            GUI.DrawTexture(m_rtHeader, m_texHeaderImg, ScaleMode.StretchToFill, true);
            GUILayout.Space(m_rtHeader.height + 8f);
            GUILayout.Label($"<color=white>Unity3D:\t{UnityFullVersion}</color>", m_styleNormalFont);

            InstallOrUpgradeGui();

            FooterGui();
        }
    }
}
