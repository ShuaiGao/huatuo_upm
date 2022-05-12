using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
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
        HTEditorInstaller()
        {
        }
        public void Init()
        {
            if (File.Exists(HTEditorConfig.HuatuoVersionPath))
            {
                var data = File.ReadAllText(HTEditorConfig.HuatuoVersionPath, Encoding.UTF8);
                m_HuatuoVersion = JsonUtility.FromJson<HuatuoVersion>(data);
            }
            else
            {
                m_HuatuoVersion = default;
            }

            HTEditorCache.Instance.SetCacheDirectory(m_HuatuoVersion.CacheDir);
        }
        public void DoUninstall()
        {
            string libil2cppPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
            string original = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp_original_unity");
            // backup libil2cpp
            if (Directory.Exists(original))
            {
                if (Directory.Exists(libil2cppPath))
                {
                    Directory.Delete(libil2cppPath, true);
                }
                Directory.Move(original, libil2cppPath);
            }
            m_InstallVersion.huatuoTag = "";
            m_InstallVersion.il2cppTag = "";
            SaveVersionLog();
            // 不存在原始备份目录
            // TODO 这里考虑下是否帮用户下载libil2cpp
        }
        public static void Enable(Action<string> callback)
        {
            var mv1 = HTEditorUtility.Mv(HTEditorConfig.LibIl2cppPath, HTEditorConfig.LibIl2cppBackPath);
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

            mv1 = HTEditorUtility.Mv(HTEditorConfig.LibIl2cppBackPath, HTEditorConfig.LibIl2cppPath);
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

        public void Install(InstallVersion installVersion)
        {
            this.m_InstallVersion = installVersion;
            try
            {
                BackupLibil2cpp();
                Extract();
                SaveVersionLog();
                DelBackupLibil2cpp();
            }
            catch (UnauthorizedAccessException)
            {
                EditorUtility.DisplayDialog("警告", "权限不足!!!\n请关闭libil2cpp目录及打开的内部文件，然后重试", "ok");
            }
            catch (Exception ex)
            {
                RevertInstall();
                Debug.LogError($"Install huatuo Error: {ex.Message}");
            }
        }
        public void RevertInstall()
        {
            m_InstallVersion.huatuoTag = m_HuatuoVersion.HuatuoTag;
            m_InstallVersion.il2cppTag = m_HuatuoVersion.Il2cppTag;
            if (!m_bDoBackup)
            {
                return;
            }
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
            string installPathBak = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", m_sBackupFileName);
            // backup libil2cpp
            if (Directory.Exists(installPathBak))
            {
                Directory.Delete(installPath, true);
                Directory.Move(installPathBak, installPath);
            }
        }
        public void DelBackupLibil2cpp()
        {
            if (!m_bDoBackup)
            {
                return;
            }
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
            string installPathBak = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", m_sBackupFileName);
            // backup libil2cpp
            if (Directory.Exists(installPathBak))
            {
                Directory.Delete(installPathBak, true);
            }
        }
        public void BackupLibil2cpp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            m_sBackupFileName = $"libil2cpp_{ts.TotalSeconds}";
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
            string installPathBak = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", m_sBackupFileName);
            string original = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp_original_unity");

            if (!Directory.Exists(installPath))
            {
                return;
            }
            // backup libil2cpp original
            if (!Directory.Exists(original))
            {
                Directory.Move(installPath, original);
            }
            if (Directory.Exists(installPath))
            {
                m_bDoBackup = true;
                Directory.Move(installPath, installPathBak);
            }
        }
        public void SaveVersionLog()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);

            // TODO 记录libil2cpp 和 huatuo 版本信息
            m_HuatuoVersion.HuatuoTag = m_InstallVersion.huatuoTag;
            m_HuatuoVersion.Il2cppTag = m_InstallVersion.il2cppTag;
            m_HuatuoVersion.Il2cppUrl = HTEditorCache.Instance.GetDownUrlWithTagIl2cpp(m_InstallVersion.il2cppTag);
            m_HuatuoVersion.HuatuoUrl = HTEditorCache.Instance.GetDownUrlWithTagHuatuo(m_InstallVersion.huatuoTag);
            m_HuatuoVersion.InstallTime = DateTime.Now.ToString();
            m_HuatuoVersion.Timestamp = Convert.ToInt64(ts.TotalMilliseconds);
            Debug.Log($"Save huatuo install version, path: {HTEditorConfig.HuatuoVersionPath}");
            File.WriteAllText(HTEditorConfig.HuatuoVersionPath, JsonUtility.ToJson(m_HuatuoVersion, true), Encoding.UTF8);
        }
        public void SaveCacheDir()
        {
            m_HuatuoVersion.CacheDir = HTEditorCache.Instance.CacheBasePath;
            File.WriteAllText(HTEditorConfig.HuatuoVersionPath, JsonUtility.ToJson(m_HuatuoVersion, true), Encoding.UTF8);
        }
        public static HuatuoRemoteConfig GetVersionData()
        {
            var data = File.ReadAllText(HTEditorConfig.HuatuoVersionPath, Encoding.UTF8);
            return JsonUtility.FromJson<HuatuoRemoteConfig>(data);
        }
        public void Extract()
        {
            var DirName = $"il2cpp_huatuo-{m_InstallVersion.il2cppTag}";
            var zipPath = HTEditorCache.Instance.GetZipPath(EFILE_NAME.IL2CPP, m_InstallVersion.il2cppTag);
            var extractDir = @$"{DirName}/libil2cpp";
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
            Extract(zipPath, extractDir, installPath);


            DirName = $"huatuo-{m_InstallVersion.huatuoTag}";
            zipPath = HTEditorCache.Instance.GetZipPath(EFILE_NAME.HUATUO, m_InstallVersion.huatuoTag);
            extractDir = @$"{DirName}/huatuo";
            installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp", "huatuo");
            Extract(zipPath, extractDir, installPath);
        }

        public static bool Extract(string zipPath, string extractDir, string installPath)
        {
            var result = ExtractZip(zipPath, extractDir, installPath);
            return result.Count > 0;
        }

        public static List<string> ExtractZip(string zipFilePath, string relativePath, string destPath)
        {
            var result = new List<string>();

            relativePath = relativePath.Replace(@"\", @"/");

            using (FileStream zipToOpen = new FileStream(zipFilePath, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                {
                    var entry = archive.Entries.FirstOrDefault(x => x.FullName.ToUpper() == relativePath.ToUpper());
                    if (entry == null)
                        entry = archive.Entries.FirstOrDefault(x => x.FullName.ToUpper() == (relativePath + "/").ToUpper());

                    if (!string.IsNullOrWhiteSpace(entry.Name))
                    {
                        var path = Path.Combine(destPath, entry.Name);
                        using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
                        {
                            entry.Open().CopyTo(file);
                            file.Close();
                        }
                        result.Add(path);
                    }
                    else
                    {
                        var items = archive.Entries.Where(x => x.FullName.StartsWith(entry.FullName)).ToList();
                        foreach (var item in items.Where(x => string.IsNullOrWhiteSpace(x.Name)).OrderBy(x => x.Length))
                        {
                            var path = Path.Combine(destPath, item.FullName.Substring(entry.FullName.Length));
                            if (!Directory.Exists(path))
                                Directory.CreateDirectory(path);
                        }

                        foreach (var item in items.Where(x => !string.IsNullOrWhiteSpace(x.Name)).OrderBy(x => x.Length))
                        {
                            var path = new FileInfo(Path.Combine(destPath, item.FullName.Substring(entry.FullName.Length))).Directory.FullName;
                            path = Path.Combine(path, item.Name);
                            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
                            {
                                item.Open().CopyTo(file);
                                file.Close();
                            }
                            result.Add(path);
                        }
                    }
                }
            }
            return result;
        }
    }

}
