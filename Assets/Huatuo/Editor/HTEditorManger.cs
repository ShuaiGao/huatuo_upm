using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using Huatuo.Editor.ThirdPart;

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
        private bool m_bVersionUnsported = false;
        private bool m_bShowOtherVersion = false;

        private GUIStyle m_styleNormalFont = null;
        private GUIStyle m_styleWarningFont = null;
        private GUIStyle m_styleNormalBtn = null;
        private GUIStyle m_styleFooterBtn = null;

        private EditorCoroutines.EditorCoroutine m_corFetchManifest = null;
        private EditorCoroutines.EditorCoroutine m_corUpgrade = null;
        private HuatuoRemoteConfig m_remoteConfig = null;

        //选择安装其他版本时，缓存下拉框值
        private int m_nSelectedhuatuoVersionIndex = 0;
        private int m_nSelectedil2cppVersionIndex = 0;

        private bool busying => m_corUpgrade != null || m_corFetchManifest != null;

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
            
            EditorUtility.ClearProgressBar();

            HTEditorConfig.Init();

            m_logo = new HTLogo();
            m_logo.Init(m_vecMinSize);

            ReloadVersion();

            if (m_corFetchManifest != null)
            {
                this.StopCoroutine(m_corFetchManifest.routine);
            }

            m_corFetchManifest = this.StartCoroutine(GetSdkVersions(true, ReloadVersion));

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
            m_corUpgrade = this.StartCoroutine(Install(version));
        }

        private IEnumerator Install(InstallVersion version)
        {
            HTEditorCache.Instance.SetDownloadCount(2);
            var itor = HTEditorCache.Instance.GetCache(EFILE_NAME.HUATUO, version.huatuoTag, "");
            while (itor.MoveNext())
            {
                yield return itor.Current;
            }

            itor = HTEditorCache.Instance.GetCache(EFILE_NAME.IL2CPP, version.il2cppTag, "");
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
        }

        /*
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
                    itor = HTEditorUtility.DownloadFile(kv.Value.url, kv.Value.tmpfile,
                        p => { EditorUtility.DisplayProgressBar("下载中...", $"{downloading}/{needDownload.Count}", p); },
                        ret =>
                        {
                            if (!string.IsNullOrEmpty(ret))
                            {
                                haserr = true;
                                EditorUtility.DisplayDialog("错误", $"下载{kv.Value.Item1}出错.\n{ret}", "ok");
                            }
                        });
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
                        itor = HTEditorUtility.UnzipAsync(kv.Value.tmpfile, kv.Value.unzipPath, b => { cnt = b; }, p =>
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

                    var err = HTEditorUtility.Mv(kv.Value.unzipPath, kv.Key);
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
*/
        /// <summary>
        /// 对Huatuo环境进行检查
        /// </summary>
        private void InstallOrUpgradeGui()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color=white>Unity3D:\t{HTEditorConfig.UnityFullVersion}</color>", m_styleNormalFont);
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
            GUILayout.Label($"缓存路径:", m_styleNormalFont, GUILayout.Width(65));
            GUILayout.TextField(HTEditorCache.Instance.CacheBasePath);
            if (GUILayout.Button("修改缓存路径", m_styleNormalBtn, GUILayout.Width(150)))
            {
                var cachePath = EditorUtility.OpenFolderPanel("请选择缓存路径", HTEditorCache.Instance.CacheBasePath, "");
                if (cachePath.Length == 0)
                {
                    return;
                }
                if (!Directory.Exists(cachePath))
                {
                    EditorUtility.DisplayDialog("错误", "路径不存在!", "ok");
                    return;
                }
                HTEditorCache.Instance.SetCacheDirectory(cachePath);
            }
            GUILayout.EndHorizontal();

            var strMsg = "";
            var strColor = "<color=green>";
            var installVersion = HTEditorInstaller.Instance.m_HuatuoVersion;
            if (installVersion.HuatuoTag?.Length > 0)
            {
                if (installVersion.HuatuoTag != m_remoteConfig?.huatuo_recommend_version)
                {
                    strMsg = $"<color=red>Huatuo: {installVersion.HuatuoTag}</color>\t";
                }
                else
                {
                    strMsg = $"Huatuo: {installVersion.HuatuoTag}\t";
                }
                if (installVersion.Il2cppTag != m_remoteConfig?.il2cpp_recommend_version)
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
                if (GUILayout.Button("检查更新", m_styleNormalBtn, GUILayout.Width(150)))
                {
                    CheckUpdate();
                }
            }
            GUILayout.EndHorizontal();

            if (m_corFetchManifest != null)
            {
                GUILayout.Label("正在获取配置文件...", m_styleNormalFont);
            }

            if (m_remoteConfig != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"推荐版本:\tHuatuo: {m_remoteConfig.huatuo_recommend_version}\tIL2CPP: {m_remoteConfig.il2cpp_recommend_version}",
                    m_styleNormalFont);

                if (GUILayout.Button(string.IsNullOrEmpty(strMsg) ? "安装" : "更新", m_styleNormalBtn, GUILayout.Width(150)))
                {
                    var version = new InstallVersion()
                    {
                        huatuoTag = m_remoteConfig.huatuo_recommend_version,
                        il2cppTag = m_remoteConfig.il2cpp_recommend_version
                    };
                    Upgrade(version);
                }
                if (GUILayout.Button("其它版本", m_styleNormalBtn, GUILayout.Width(70)))
                {
                    m_bShowOtherVersion = !m_bShowOtherVersion;
                }

                GUILayout.EndHorizontal();
            }
            if (m_bShowOtherVersion)
            {
                GUILayout.BeginArea(new Rect(140, 320, 300, 400));
                GUILayout.BeginVertical();
                m_nSelectedhuatuoVersionIndex = EditorGUILayout.Popup("huatuo:", m_nSelectedhuatuoVersionIndex, m_remoteConfig.huatuo_version.ToArray());
                m_nSelectedil2cppVersionIndex = EditorGUILayout.Popup("IL2CPP: ", m_nSelectedil2cppVersionIndex, m_remoteConfig.il2cpp_version.ToArray());
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("取消", m_styleNormalBtn))
                {
                    m_bShowOtherVersion = false;
                }
                if (GUILayout.Button("安装", m_styleNormalBtn))
                {
                    var version = new InstallVersion()
                    {
                        huatuoTag = m_remoteConfig.huatuo_version[m_nSelectedhuatuoVersionIndex],
                        il2cppTag = m_remoteConfig.il2cpp_version[m_nSelectedil2cppVersionIndex]
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
        /// 获取远程的版本信息/re
        /// </summary>
        /// <param name="silent">静默获取</param>
        /// <param name="callback">获取后的回调</param>
        /// <returns>迭代器</returns>
        private IEnumerator GetSdkVersions(bool silent, Action callback)
        {
            m_bVersionUnsported = false;

            // Wait one frame so that we don't try to show the progress bar in the middle of OnGUI().
            yield return null;

            m_remoteConfig = null;
            var itor = HTEditorUtility.HttpRequest(HTEditorConfig.urlVersionConfig,
                (remoteConfig, err) =>
                {
                    if (!remoteConfig.Equals(default(RemoteConfig)))
                    {
                        m_remoteConfig = new HuatuoRemoteConfig(remoteConfig);
                        if (!m_remoteConfig.unity_version.Contains(HTEditorConfig.UnityVersionDigits))
                        {
                            m_bVersionUnsported = true;
                        }
                    }
                    else if (!string.IsNullOrEmpty(err) && !silent)
                    {
                        EditorUtility.DisplayDialog("错误", err, "ok");
                    }
                });
            while (itor.MoveNext())
            {
                yield return itor.Current;
            }

            if (m_remoteConfig == null)
            {
                Debug.LogError("无法获取版本信息...");
                if (!silent)
                {
                    EditorUtility.DisplayDialog("错误", "无法获取版本信息...", "ok");
                }
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
                Application.OpenURL(HTEditorConfig.WebSite);
            }

            if (GUILayout.Button("Document", m_styleFooterBtn))
            {
                Application.OpenURL(HTEditorConfig.Document);
            }
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
                
                if (m_remoteConfig != null)
                {
                    Debug.Log($"huatuo version: {m_remoteConfig.huatuo_recommend_version}");
                    Debug.Log($"il2cpp version: {m_remoteConfig.il2cpp_recommend_version}");
                }
            }));
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

            GUILayout.Space(120);

            FooterGui();

            EditorGUI.EndDisabledGroup();
        }
    }
}
