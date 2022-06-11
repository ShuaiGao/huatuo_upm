using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Huatuo.Editor
{
    internal class HTEditorInstaller
    {
        private static HTEditorInstaller instance = null;

        public static HTEditorInstaller Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HTEditorInstaller();
                }

                return instance;
            }
        }

        public void Init()
        {
            if (File.Exists(HTEditorConfig.Instance.GetVersionPath()))
            {
                var data = File.ReadAllText(HTEditorConfig.Instance.GetVersionPath(), Encoding.UTF8);
                m_HuatuoVersion = JsonUtility.FromJson<HuatuoVersion>(data);
            }
            else
            {
                m_HuatuoVersion = default;
            }
        }

        public void Prepare(Action<bool> callback)
        {
            var ret = true;
            try
            {
                EditorUtility.DisplayProgressBar("初始化", "初始化基础环境", 0f);

                if (!Directory.Exists(HTEditorConfig.Instance.HuatuoHelperPath))
                {
                    Directory.CreateDirectory(HTEditorConfig.Instance.HuatuoHelperPath);
                }

                var il2CppPath = Path.Combine(HTEditorConfig.Instance.HuatuoHelperPath, "il2cpp");
                if (!Directory.Exists(il2CppPath))
                {
                    HTEditorUtility.CopyFilesRecursively(HTEditorConfig.Il2cppPath, il2CppPath);
                }

                var monoBleedingPath = Path.Combine(HTEditorConfig.Instance.HuatuoHelperPath, "MonoBleedingEdge");
                if (!Directory.Exists(monoBleedingPath))
                {
                    HTEditorUtility.CopyFilesRecursively(HTEditorConfig.MonoBleedingEdgePath, monoBleedingPath);
                }
            }
            catch (Exception ex)
            {
                ret = false;
                Debug.LogException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                callback?.Invoke(ret);
            }
        }

        public void CheckHuatuo(Action<string> callback)
        {
            var il2CppPath = Path.Combine(HTEditorConfig.Instance.HuatuoHelperPath, "il2cpp");
            if (!Directory.Exists(il2CppPath))
            {
                callback?.Invoke($"[CheckHuatuo] {il2CppPath} not exists!");
                return;
            }

            var libil2CppPath = Path.Combine(il2CppPath, "libil2cpp");
            if (!Directory.Exists(il2CppPath))
            {
                callback?.Invoke($"[CheckHuatuo] {libil2CppPath} not exists!");
                return;
            }
            
            var huatuoPath = Path.Combine(libil2CppPath, "huatuo");
            if (!Directory.Exists(huatuoPath))
            {
                callback?.Invoke($"[CheckHuatuo] {huatuoPath} not exists!");
                return;
            }
            
            callback?.Invoke("");
        }

        public void DoUninstall()
        {
            // backup libil2cpp
            if (Directory.Exists(HTEditorConfig.Instance.GetUnityIl2cppPath()))
            {
                Directory.Delete(HTEditorConfig.Instance.GetUnityIl2cppPath(), true);
            }
            if (Directory.Exists(HTEditorConfig.Instance.GetVersionPath()))
            {
                Directory.Delete(HTEditorConfig.Instance.GetVersionPath(), true);
            }
            
            m_InstallVersion.huatuoTag = "";
            m_InstallVersion.il2cppTag = "";
            m_HuatuoVersion.HuatuoTag = "";
            m_HuatuoVersion.Il2cppTag = "";
            SaveVersionLog();
            // 不存在原始备份目录
            // TODO 这里考虑下是否帮用户下载libil2cpp
        }

        public static void Enable(Action<string> callback)
        {
            var mv1 = HTEditorUtility.Mv(HTEditorConfig.Libil2cppPath, HTEditorConfig.Libil2cppOritinalPath);
            if (!string.IsNullOrEmpty(mv1))
            {
                Debug.LogError(mv1);
                callback?.Invoke(mv1);
                return;
            }

            mv1 = HTEditorUtility.Mv(HTEditorConfig.HuatuoIL2CPPBackPath, HTEditorConfig.HuatuoIL2CPPPath);
            if (!string.IsNullOrEmpty(mv1))
            {
                Debug.LogError(mv1);
                callback?.Invoke(mv1);
                return;
            }

            callback?.Invoke(null);
        }

        public static void Disable(Action<string> callback)
        {
            var mv1 = HTEditorUtility.Mv(HTEditorConfig.HuatuoIL2CPPPath, HTEditorConfig.HuatuoIL2CPPBackPath);
            if (!string.IsNullOrEmpty(mv1))
            {
                Debug.LogError(mv1);
                callback?.Invoke(mv1);
                return;
            }

            mv1 = HTEditorUtility.Mv(HTEditorConfig.Libil2cppOritinalPath, HTEditorConfig.Libil2cppPath);
            if (!string.IsNullOrEmpty(mv1))
            {
                Debug.LogError(mv1);
                callback?.Invoke(mv1);
                return;
            }

            callback?.Invoke(null);
        }

        public static void Uninstall(Action<string> callback)
        {
            Disable(ret =>
            {
                if (!string.IsNullOrEmpty(ret))
                {
                    callback?.Invoke(ret);
                    return;
                }

                if (Directory.Exists(HTEditorConfig.HuatuoIL2CPPBackPath))
                {
                    Directory.Delete(HTEditorConfig.HuatuoIL2CPPBackPath, true);
                }

                callback?.Invoke(null);
            });
        }

        public InstallVersion m_InstallVersion; // 当前安装临时使用的版本数据
        public HuatuoVersion m_HuatuoVersion; // 已安装的版本信息

        private bool m_bDoBackup;
        private string m_sBackupFileName;

        private IEnumerator Extract(Action<bool> callback)
        {
            var il2cppZip = HTEditorCache.Instance.GetZipPath(m_InstallVersion.il2cppType, m_InstallVersion);
            var huatuozip = HTEditorCache.Instance.GetZipPath(m_InstallVersion.huatuoType, m_InstallVersion);
            var il2cppExtractTo = Path.GetFileNameWithoutExtension(il2cppZip);
            var huatuoExtractTo = Path.GetFileNameWithoutExtension(huatuozip);

            var il2cppCachePath = Path.Combine(Path.GetDirectoryName(il2cppZip), il2cppExtractTo);
            var huatuoCachePath = Path.Combine(Path.GetDirectoryName(huatuozip), huatuoExtractTo);
            var cnt = 0;
            var haserr = false;
            var itor = HTEditorUtility.UnzipAsync(il2cppZip, il2cppCachePath, b => { cnt = b; },
                p => { EditorUtility.DisplayProgressBar("解压中...", $"il2cpp:{p}/{cnt}", (float) p / cnt); }, null,
                () => { haserr = true; });
            while (itor.MoveNext())
            {
                yield return itor.Current;
            }
            EditorUtility.ClearProgressBar();
            if (haserr)
            {
                callback?.Invoke(true);
                yield break;
            }

            cnt = 0;
            itor = HTEditorUtility.UnzipAsync(huatuozip, huatuoCachePath, b => { cnt = b; },
                p => { EditorUtility.DisplayProgressBar("解压中...", $"huatuo:{p}/{cnt}", (float) p / cnt); }, null,
                () => { haserr = true; });
            while (itor.MoveNext())
            {
                yield return itor.Current;
            }

            EditorUtility.ClearProgressBar();
            if (haserr)
            {
                callback?.Invoke(true);
                yield break;
            }

            var il2cppDirName = Path.Combine(il2cppCachePath, HTEditorCache.GetZipInnerFolder(m_InstallVersion.il2cppType, m_InstallVersion));
            var huatuoDirName = Path.Combine(huatuoCachePath, HTEditorCache.GetZipInnerFolder(m_InstallVersion.huatuoType, m_InstallVersion));

            if (!Directory.Exists(il2cppDirName))
            {
                Debug.LogError($"{il2cppDirName} not exists!!!");
                callback?.Invoke(true);
                yield break;
            }

            if (!Directory.Exists(huatuoDirName))
            {
                Debug.LogError($"{huatuoDirName} not exists!!!");
                callback?.Invoke(true);
                yield break;
            }

            try
            {
                if (!Directory.Exists(HTEditorConfig.Instance.GetUnityIl2cppPath()))
                {
                    HTEditorUtility.CopyFilesRecursively(Path.Combine(EditorApplication.applicationContentsPath, "il2cpp"), HTEditorConfig.Instance.GetUnityIl2cppPath());
                    Directory.Delete(HTEditorConfig.Instance.GetUnityLibil2cppPath(), true);
                }

                if (Directory.Exists(HTEditorConfig.Instance.GetUnityLibil2cppPath()))
                {
                    Directory.Delete(HTEditorConfig.Instance.GetUnityLibil2cppPath(), true);
                }
                
                HTEditorUtility.CopyFilesRecursively(il2cppDirName, HTEditorConfig.Instance.GetUnityLibil2cppPath());
                HTEditorUtility.CopyFilesRecursively(huatuoDirName, HTEditorConfig.Instance.GetUnityHuatuoPath());
                if (!Directory.Exists(Path.Combine(HTEditorConfig.Instance.GetUnityDigitsPath(), "MonoBleedingEdge")))
                {
                    HTEditorUtility.CopyFilesRecursively(Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge"), Path.Combine(HTEditorConfig.Instance.GetUnityDigitsPath(), "MonoBleedingEdge"));
                }
            }
            catch (IOException ex)
            {
                // 当cmd占用了libil2cpp目录时，会出现这个报错
                Debug.LogError("libil2cpp 文件夹或路径被其它程序打开无法操作, 请检查占用并重试。");
                Debug.LogException(ex);
                haserr = true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                haserr = true;
            }

            callback?.Invoke(haserr);
        }

        public IEnumerator Install(InstallVersion installVersion, Action<bool> callback)
        {
            this.m_InstallVersion = installVersion;

            Debug.Log("备份il2cpp目录");
            var task = Task.Run(BackupLibil2cpp);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            var hasErr = false;
            var itor = Extract(r => { hasErr = r; });
            while (itor.MoveNext())
            {
                yield return itor.Current;
            }

            if (hasErr)
            {
                RevertInstall();
                callback?.Invoke(false);
                yield break;
            }

            task = Task.Run(SaveVersionLog);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            task = Task.Run(DelBackupLibil2cpp);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            callback?.Invoke(true);
        }

        public void RevertInstall()
        {
            m_InstallVersion.huatuoTag = m_HuatuoVersion.HuatuoTag;
            m_InstallVersion.il2cppTag = m_HuatuoVersion.Il2cppTag;
            if (!m_bDoBackup)
            {
                return;
            }

            // backup libil2cpp
            if (Directory.Exists(m_sBackupFileName))
            {
                Directory.Delete(HTEditorConfig.Instance.GetUnityLibil2cppPath(), true);
                Directory.Move(m_sBackupFileName, HTEditorConfig.Instance.GetUnityLibil2cppPath());
            }
        }

        public void DelBackupLibil2cpp()
        {
            if (!m_bDoBackup)
            {
                return;
            }

            // backup libil2cpp
            if (Directory.Exists(m_sBackupFileName))
            {
                Directory.Delete(m_sBackupFileName, true);
            }
        }

        public void BackupLibil2cpp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            m_sBackupFileName = $"{HTEditorConfig.Instance.GetUnityLibil2cppPath()}_bak_{ts.TotalSeconds}";
            //string original = Path.Combine(HTEditorConfig.Il2cppPath, "libil2cpp_original_unity");

            if (!Directory.Exists(HTEditorConfig.Instance.GetUnityLibil2cppPath()))
            {
                return;
            }

            // backup libil2cpp original
            //if (!Directory.Exists(original))
            //{
            //    Directory.Move(HTEditorConfig.Libil2cppPath, original);
            //}

            if (Directory.Exists(HTEditorConfig.Instance.GetUnityLibil2cppPath()))
            {
                m_bDoBackup = true;
                Directory.Move(HTEditorConfig.Instance.GetUnityLibil2cppPath(), m_sBackupFileName);
            }
        }

        private HuatuoVersion loadVersions()
        {
            HuatuoVersion versionData;
            try
            {
                var data = File.ReadAllText(HTEditorConfig.Instance.GetVersionPath(), Encoding.UTF8);
                versionData = JsonUtility.FromJson<HuatuoVersion>(data);
            }catch (Exception)
            {
                versionData = default;
            }
            return versionData;
        }

        public void SaveVersionLog()
        {
            var v = loadVersions();
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);

            // TODO 记录libil2cpp 和 huatuo 版本信息
            v.HuatuoTag = m_InstallVersion.huatuoTag;
            v.Il2cppTag = m_InstallVersion.il2cppTag;
            v.InstallTime = DateTime.Now.ToString();
            v.Timestamp = Convert.ToInt64(ts.TotalMilliseconds);
            Debug.Log($"Save huatuo install version, path: {HTEditorConfig.Instance.GetVersionPath()}， data={JsonUtility.ToJson(v, true)}");
            File.WriteAllText(HTEditorConfig.Instance.GetVersionPath(), JsonUtility.ToJson(v, true), Encoding.UTF8);
        }

        public void SaveCacheDir()
        {
            var tmp = new HuatuoVersionDict();
            tmp.UnityIl2cppDir = HTEditorConfig.Instance.HuatuoPath;
            File.WriteAllText(HTEditorConfig.Instance.HuatuoCacheData, EditorJsonUtility.ToJson(tmp, true),Encoding.UTF8);
        }
    }
}
