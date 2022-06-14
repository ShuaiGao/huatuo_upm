using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using Huatuo.Editor.ThirdPart;
using Huatuo.Editor.BuildPipeline;

namespace Huatuo.Editor
{
    /// <summary>
    /// 这个类是Huatuo管理器，用于对Huatuo进行开关和更新等相关操作
    /// </summary>
    public class HTEditorManger : EditorWindow
    {
        private HTLogo m_logo = null;
        private static readonly Vector2 m_vecMinSize = new Vector2(620f, 455);

        private bool m_bInitialized = false;
        private bool m_bHasIl2cpp = false;
        internal bool m_bVersionUnsported = false;
        private bool m_bShowOtherVersion = false;
        private bool m_bEnableHuatuo= false;

        private GUIStyle m_styleNormalFont = null;
        private GUIStyle m_styleWarningFont = null;
        private GUIStyle m_styleNormalBtn = null;
        private GUIStyle m_styleFooterBtn = null;


        private EditorCoroutines.EditorCoroutine m_corUpgrade = null;
        private HTEditorModel m_Model = null;

        //选择安装其他版本时，缓存下拉框值
        internal int m_nSelectedhuatuoVersionIndex = 0;
        internal int m_nSelectedil2cppVersionIndex = 0;

        private bool busying => m_corUpgrade != null || m_Model.IsBusy();

        private void OnEnable()
        {
            minSize = maxSize = m_vecMinSize;

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
            m_bHasIl2cpp = Directory.Exists(HTEditorConfig.Il2cppPath);
            HTEditorInstaller.Instance.Init();

            Repaint();
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
            m_Model = HTEditorModel.Instance;
            m_Model.Manager = this;

            EditorUtility.ClearProgressBar();

            m_logo = new HTLogo();
            m_logo.Init(m_vecMinSize, this);

            ReloadVersion();
            CheckUpdate();

            m_bInitialized = true;
        }

        private void OnDestroy()
        {
            m_logo.Destroy();
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

        /*
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
                HTEditorInstaller.Enable(ret => { endFunc?.Invoke(ret); });
            }
            else
            {
                HTEditorInstaller.Disable(ret => { endFunc?.Invoke(ret); });
            }
        }
*/
        /// <summary>
        /// 升级中...
        /// </summary>
        private void Upgrade(InstallVersion version)
        {
            if (m_corUpgrade != null)
            {
                EditorUtility.DisplayDialog("提示", $"其它任务进行中", "ok");
                return;
            }

            if (version.huatuoTag.StartsWith(HTEditorConfig.HEAD))
            {
                version.huatuoType = EFILE_NAME.HUATUO_MAIN;
            }

            if (version.huatuoType == EFILE_NAME.HUATUO_MAIN)
            {
                m_corUpgrade = this.StartCoroutine(HTEditorUtility.HttpRequest<ItemSerial<CommitItem>>(
                    HTEditorConfig.urlHuatuoCommits, (itemSerial, err) =>
                    {
                        var logList = itemSerial.items;
                        if (logList == null || logList.Count == 0)
                        {
                            Debug.LogError("Unable to retrieve commit logs.");
                            err = $"【3】获取logs失败。";
                        }

                        if (!string.IsNullOrEmpty(err))
                        {
                            EditorUtility.DisplayDialog("错误", err, "ok");
                        }
                        else
                        {
                            version.huatuoTag = logList[0].GetShaShort();
                            m_corUpgrade = this.StartCoroutine(Install(version));
                        }

                        Debug.Log($"commit logs: {logList}");
                    })
                );
            }
            else
            {
                m_corUpgrade = this.StartCoroutine(Install(version));
            }
        }

        private IEnumerator Install(InstallVersion version)
        {
            // 同一时间只能有一个在安装
            FileStream objFileStream = null;
            try
            {
                objFileStream = new FileStream(HTEditorConfig.Instance.HuatuoLockerFilePath, FileMode.Append, FileAccess.Write, FileShare.None);
            }
            catch (IOException)
            {
                EditorUtility.DisplayDialog("失败", "安装失败，其它程序安装中！", "ok");
            }
            HTEditorCache.Instance.SetDownloadCount(2);
            var itor = HTEditorCache.Instance.GetCache(version.huatuoType, version, "");
            while (itor.MoveNext())
            {
                yield return itor.Current;
            }

            itor = HTEditorCache.Instance.GetCache(version.il2cppType, version, "");
            while (itor.MoveNext())
            {
                yield return itor.Current;
            }

            if (!HTEditorCache.Instance.DownloadSuccess())
            {
                EditorUtility.DisplayDialog("错误", "下载失败!", "ok");
                Debug.Log("下载失败");
                yield return null;
            }
            else
            {
                itor = HTEditorInstaller.Instance.Install(version,
                    ret => { EditorUtility.DisplayDialog("结果", $"安装{(ret ? "成功" : "失败")}", "ok"); });
                while (itor.MoveNext())
                {
                    yield return itor.Current;
                }
            }

            m_corUpgrade = null;
            ReloadVersion();
            objFileStream?.Close();
        }

        /// <summary>
        /// 对Huatuo环境进行检查
        /// </summary>
        private void InstallOrUpgradeGui()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color=white>Unity3D:\t{HTEditorConfig.UnityFullVersion}</color>", m_styleNormalFont);


            var bEnableHuatuo = HtBuildSettings.Instance.Enable;
            m_bEnableHuatuo = EditorGUILayout.ToggleLeft("是否启用Huatuo", bEnableHuatuo);
            if (bEnableHuatuo != m_bEnableHuatuo)
            {
                m_bEnableHuatuo = bEnableHuatuo;
                HtBuildSettings.ReverseEnable();
            }
            
            GUILayout.EndHorizontal();

            if (m_bVersionUnsported)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<color=red>你的Unity版本不被支持，请使用受支持的版本!</color>", m_styleWarningFont);
                if (GUILayout.Button("查看受支持的版本", m_styleNormalBtn))
                {
                    Application.OpenURL(HTEditorConfig.SupportedVersion);
                }

                GUILayout.EndHorizontal();
                return;
            }

            if (!m_bHasIl2cpp)
            {
                GUILayout.Label("<color=red>Build Support(IL2CPP) 未安装！</color>", m_styleWarningFont);
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Huatuo路径:", m_styleNormalFont, GUILayout.Width(85));
            GUILayout.TextField(HTEditorConfig.Instance.HuatuoPath, GUILayout.MaxWidth(380));
            if (GUILayout.Button("修改", m_styleNormalBtn, GUILayout.Width(70)))
            {
                var cachePath = EditorUtility.OpenFolderPanel("请选择Huatuo路径", HTEditorConfig.Instance.HuatuoPath, "");
                if (cachePath.Length == 0)
                {
                    return;
                }

                if (!Directory.Exists(cachePath))
                {
                    EditorUtility.DisplayDialog("错误", "路径不存在!", "ok");
                    return;
                }
                HTEditorConfig.Instance.SetHuatuoDirectory(cachePath);
            }
            if (GUILayout.Button("打开", m_styleNormalBtn, GUILayout.Width(70)))
            {
                EditorUtility.RevealInFinder(HTEditorConfig.Instance.HuatuoPath);
            }
            GUILayout.EndHorizontal();

            var recommend = m_Model.GetRecommendVersion(false);
            var strMsg = "";
            var strColor = "<color=green>";
            var installVersion = HTEditorInstaller.Instance.m_HuatuoVersion;
            if (installVersion.HuatuoTag?.Length > 0)
            {
                if (installVersion.HuatuoTag != recommend.huatuoTag && $"{HTEditorConfig.HEAD}[{installVersion.HuatuoTag}]" != recommend.huatuoTag)
                {
                    strMsg = $"<color=red>Huatuo: {installVersion.HuatuoTag}</color>\t";
                }
                else
                {
                    strMsg = $"Huatuo: {installVersion.HuatuoTag}\t";
                }

                if (installVersion.Il2cppTag != recommend.il2cppTag)
                {
                    strMsg = strMsg + $"<color=red>IL2CPP: {installVersion.Il2cppTag}</color>";
                }
                else
                {
                    strMsg = strMsg + $"IL2CPP: {installVersion.Il2cppTag}";
                }

                //strMsg = $"Huatuo: {installVersion.HuatuoTag}\tIL2CPP: {installVersion.Il2cppTag}";
            }

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(strMsg))
            {
                strMsg = $"{strColor}已安装:\t{strMsg}</color>";
                GUILayout.Label(strMsg, m_styleNormalFont);
                if (GUILayout.Button("检查更新", m_styleNormalBtn, GUILayout.Width(70)))
                {
                    CheckUpdate();
                }

                if (GUILayout.Button("卸载", m_styleNormalBtn, GUILayout.Width(70)))
                {
                    HTEditorInstaller.Instance.DoUninstall();
                }
            }
            else
            {
                GUILayout.Label("未安装", m_styleNormalFont);
                if (GUILayout.Button("检查更新", m_styleNormalBtn, GUILayout.Width(143)))
                {
                    CheckUpdate();
                }
            }

            GUILayout.EndHorizontal();

            if (m_Model.IsBusy())
            {
                GUILayout.Label("正在获取配置文件...", m_styleNormalFont);
                return;
            }

            GUILayout.BeginHorizontal();
            if (String.IsNullOrEmpty(recommend.il2cppBranch))
            {
                GUILayout.Label($"最新版本:\tHuatuo: {recommend.huatuoTag}\tIL2CPP: {recommend.il2cppTag}", m_styleNormalFont);
            }
            else
            {
                GUILayout.Label($"最新版本:\tHuatuo: {recommend.huatuoTag}\tIL2CPP: {recommend.il2cppBranch}-{recommend.il2cppTag}", m_styleNormalFont);
            }


            if (GUILayout.Button(string.IsNullOrEmpty(strMsg) ? "安装" : "更新", m_styleNormalBtn, GUILayout.Width(70)))
            {
                Upgrade(m_Model.GetRecommendVersion(false));
            }

            if (GUILayout.Button("其它版本", m_styleNormalBtn, GUILayout.Width(70)))
            {
                m_bShowOtherVersion = !m_bShowOtherVersion;
            }

            GUILayout.EndHorizontal();


            if (m_bShowOtherVersion)
            {
                GUILayout.BeginArea(new Rect(140, 320, 300, 400));
                GUILayout.BeginVertical();
                m_nSelectedhuatuoVersionIndex = EditorGUILayout.Popup("huatuo:", m_nSelectedhuatuoVersionIndex,
                    m_Model.GetHuatuoVersions().ToArray());
                m_nSelectedil2cppVersionIndex = EditorGUILayout.Popup("IL2CPP: ", m_nSelectedil2cppVersionIndex,
                    m_Model.GetIl2cppVersions().ToArray());
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("取消", m_styleNormalBtn))
                {
                    m_bShowOtherVersion = false;
                }

                if (GUILayout.Button("安装", m_styleNormalBtn))
                {
                    var version = new InstallVersion()
                    {
                        huatuoType = EFILE_NAME.HUATUO,
                        huatuoTag = m_Model.GetHuatuoVersions()[m_nSelectedhuatuoVersionIndex],
                        il2cppType = EFILE_NAME.IL2CPP,
                        il2cppTag = m_Model.GetIl2cppVersions()[m_nSelectedil2cppVersionIndex]
                    };
                    Upgrade(version);
                    m_bShowOtherVersion = false;
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.EndArea();
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
                Application.OpenURL(HTEditorConfig.WebSite);
            }

            if (GUILayout.Button("Document", m_styleFooterBtn))
            {
                Application.OpenURL(HTEditorConfig.Document);
            }

            if (GUILayout.Button("安装最新版本", m_styleFooterBtn))
            {
                Upgrade(m_Model.GetRecommendVersion(false));
            }

            GUILayout.EndHorizontal();
        }

        private void CheckUpdate()
        {
            if (m_corUpgrade != null)
            {
                return;
            }

            ReloadVersion();
            m_Model.fetchData();
        }

        private void OnGUI()
        {
            CheckStyle();

            if (m_logo != null)
            {
                m_logo.OnGUI();

                GUILayout.Space(m_logo.ImgHeight + 8);
            }

            EditorGUI.BeginDisabledGroup(busying);

            InstallOrUpgradeGui();

            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            FooterGui();
        }
    }
}
