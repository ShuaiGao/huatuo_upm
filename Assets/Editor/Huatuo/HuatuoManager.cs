using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Huatuo.Editor
{
    [Serializable]
    internal class VersionInfo
    {
        public string ver;
        public string commitid;
    }

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


        private VersionInfo m_verHuatuo_il2cpp = null;
        private VersionInfo m_verHuatuo = null;

        private VersionInfo m_verHuatuoBack_il2cpp = null;
        private VersionInfo m_verHuatuoBack = null;

        private bool m_bHasHuatuo = false;
        private bool m_bHasHuatoBack = false;

        private bool m_bHasIl2cpp = false;
        private bool m_bHasIl2cppBack = false;

        private GUIStyle m_styleNormalFont = null;
        private GUIStyle m_styleWarningFont = null;
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

        private static VersionInfo CheckVersion(string basePath)
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

            return JsonUtility.FromJson<VersionInfo>(txt);
        }

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

        private void CheckStyle()
        {
            if (m_styleNormalFont != null)
            {
                return;
            }
            
            m_styleNormalFont = new GUIStyle(GUI.skin.label);
            m_styleNormalFont.richText = true;
            m_styleNormalFont.fontSize = 14;
            m_styleNormalFont.fontStyle = FontStyle.Normal;
            m_styleNormalFont.padding = new RectOffset(4, 0, 0, 0);
            
            m_styleWarningFont = new GUIStyle(m_styleNormalFont);
            m_styleWarningFont.fontSize = 18;
            m_styleWarningFont.fontStyle = FontStyle.Bold;
            
            m_styleFooterBtn = new GUIStyle(GUI.skin.button);
            m_styleFooterBtn.padding = new RectOffset(0, 0, 10, 10);
            m_styleFooterBtn.wordWrap = true;
            m_styleFooterBtn.richText = true;
        }

        private void InstallOrUpgradeGUI()
        {
            if (!m_bHasIl2cpp)
            {
                GUILayout.Label("<color=red>Scripting Backend(IL2CPP) is not installed!</color>", m_styleWarningFont);
            }

            var huatuoVer = "";
            if (m_bHasHuatuo)
            {
                huatuoVer = $"Huatuo:{m_verHuatuo.ver}\tIL2CPP:{m_verHuatuo_il2cpp.ver}";
            }
            else if (m_bHasHuatoBack)
            {
                huatuoVer = $"Huatuo:{m_verHuatuoBack.ver}\tIL2CPP:{m_verHuatuoBack_il2cpp.ver}";
            }

            if (!string.IsNullOrEmpty(huatuoVer))
            {
                huatuoVer = "<color=green>已安装:\t" + huatuoVer + "</color>";
                GUILayout.Space(8f);
                GUILayout.Label(huatuoVer, m_styleNormalFont);
            }

        }

        private void FooterGUI()
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
            
            var tmpColor = GUI.color;

            GUI.DrawTexture(m_rtHeader, m_texHeaderImg, ScaleMode.StretchToFill, true);
            GUILayout.Space(m_rtHeader.height + 8f);
            GUILayout.Label($"<color=white>Unity3D:\t{UnityFullVersion}</color>", m_styleNormalFont);

            InstallOrUpgradeGUI();

            FooterGUI();
        }
    }
}
