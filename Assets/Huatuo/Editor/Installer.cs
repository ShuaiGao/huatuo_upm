using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using Huatuo.Editor.ThirdPart;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Huatuo.Editor
{
    internal class Uninstaller
    {
        public static void DoUninstall()
        {
            string libil2cppPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
            string original = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp_original_unity");
            // backup libil2cpp
            if (Directory.Exists(original))
            {
                Directory.Delete(libil2cppPath, true);
                Directory.Move(original, libil2cppPath);
            }
            // 不存在原始备份目录
            // TODO 这里考虑下是否帮用户下载libil2cpp
        }
    }

    internal class Installer
    {
        private static Installer instance = null;
        public static Installer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Installer();
                }
                return instance;
            }
        }
        Installer() { }
        public void Init()
        {

            var data = File.ReadAllText(Config.HuatuoVersionPath, Encoding.UTF8);
            huatuoVersion = JsonUtility.FromJson<HuatuoVersion>(data);
            PackageManager.Instance.SetCacheDirectory(huatuoVersion.CacheDir);
        }
        public static void Enable(Action<string> callback)
        {
            var mv1 = Utility.Mv(Config.LibIl2cppPath, Config.LibIl2cppBackPath);
            if (!string.IsNullOrEmpty(mv1))
            {
                Debug.LogError(mv1);
                callback?.Invoke(mv1);
                return;
            }

            mv1 = Utility.Mv(Config.HuatuoIL2CPPBackPath, Config.HuatuoIL2CPPPath);
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
            var mv1 = Utility.Mv(Config.HuatuoIL2CPPPath, Config.HuatuoIL2CPPBackPath);
            if (!string.IsNullOrEmpty(mv1))
            {
                Debug.LogError(mv1);
                callback?.Invoke(mv1);
                return;
            }

            mv1 = Utility.Mv(Config.LibIl2cppBackPath, Config.LibIl2cppPath);
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

                if (Directory.Exists(Config.HuatuoIL2CPPBackPath))
                {
                    Directory.Delete(Config.HuatuoIL2CPPBackPath, true);
                }

                callback?.Invoke(null);
            });
        }

        public InstallVersion version;
        private bool useGithub;
        private bool doBackup;
        private string backupFileName;
        private string libil2cppPrefix;
        private string huatuoPrefix;

        private string libil2cppTagPrefix;
        private string huatuoTagPrefix;
        public HuatuoVersion huatuoVersion;

        private string libil2cppPrefixGitee = "https://gitee.com/juvenior/il2cpp_huatuo/repository/archive";
        private string libil2cppPrefixGithub = "https://github.com/pirunxi/il2cpp_huatuo/archive/refs/heads";
        private string huatuoPrefixGitee = "https://gitee.com/focus-creative-games/huatuo/repository/archive";
        private string huatuoPrefixGithub = "https://github.com/focus-creative-games//huatuo/archive/refs/heads";

        private string libil2cppTagPrefixGithub = "https://github.com/pirunxi/il2cpp_huatuo/archive/refs/tags";
        private string huatuoTagPrefixGithub = "https://github.com/focus-creative-games/huatuo/archive/refs/tags";

        private List<string> downloadList = new List<string>();

        public void Install(bool github, InstallVersion installVersion)
        {
            this.version = installVersion;
            useGithub = github;
            libil2cppPrefix = github ? libil2cppPrefixGithub : libil2cppPrefixGitee;
            huatuoPrefix = github ? huatuoPrefixGithub : huatuoPrefixGitee;

            libil2cppTagPrefix = github ? libil2cppTagPrefixGithub : "";
            huatuoTagPrefix = github ? huatuoTagPrefixGithub : "";
            Install();
        }
        public static string PathIl2cpp
        {
            get
            {
                string str7 = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                return Path.Combine(str7, "Data", "il2cpp");
            }
        }
        public string GetDownUrlWithTagIl2cpp()
        {
            return @$"{libil2cppTagPrefix}/{version.il2cppTag}.zip";
        }
        public string GetDownUrlWithTagHuatuo()
        {
            return @$"{huatuoTagPrefix}/{version.huatuoTag}.zip";
        }
        public static bool CheckIl2cpp()
        {
            return Directory.Exists(PathIl2cpp);
        }
        private void Install()
        {
            // 安装过程，不需要对比版本，有时候需要覆盖安装的
            if (!CheckSupport())
            {
                return;
            }
            try
            {
                BackupLibil2cpp();
                InstallIl2cpp();
                InstallHuatuo();
            }
            catch (Exception ex)
            {
                UnBackupLibil2cpp();
                Debug.LogError("Install huatuo Error");
                Debug.LogError(ex.Message);
            }
            finally
            {
                //ClearCache();
            }
            SaveVersionLog();

            //var version = GetVersionData();
            //Debug.Log(version.Timestamp);
            //Debug.Log(version.InstallTime);
        }
        private void ClearCache()
        {
            foreach (string item in downloadList)
            {
                File.Delete(item);
            }
        }
        private bool CheckSupport()
        {
            // TODO 做gitee测试，并支持gitee
            if (!useGithub)
            {
                //Debug.LogError("Not Support gitee， Please use github!!!");
                EditorUtility.DisplayDialog("错误", "当前不支持gitee, 请使用github!", "ok");
                return false;
            }
            // TODO unity版本判断
            //if (!versionSet.Contains(InternalEditorUtility.GetUnityVersionDigits()))
            //{
            //    Debug.LogError($"Not Support unity version{Application.unityVersion}");
            //    return false;
            //}
            // TODO 检查libil2cpp, huatuo 版本，避免不必要的更新
            return true;
        }
        public void UnBackupLibil2cpp()
        {
            if (!doBackup)
            {
                return;
            }
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
            string installPathBak = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", backupFileName);
            // backup libil2cpp
            if (Directory.Exists(installPathBak))
            {
                Directory.Delete(installPathBak, true);
            }
        }
        public void BackupLibil2cpp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            backupFileName = $"libil2cpp_{ts.TotalSeconds}";
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
            string installPathBak = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", backupFileName);
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

            doBackup = true;
            Directory.Move(installPath, installPathBak);
        }
        public void SaveVersionLog()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);

            // TODO 记录libil2cpp 和 huatuo 版本信息
            huatuoVersion.HuatuoTag = version.huatuoTag;
            huatuoVersion.Il2cppTag = version.il2cppTag;
            huatuoVersion.Il2cppUrl = GetDownUrlWithTagIl2cpp();
            huatuoVersion.HuatuoUrl = GetDownUrlWithTagHuatuo();
            huatuoVersion.InstallTime = DateTime.Now.ToString();
            huatuoVersion.Timestamp = Convert.ToInt64(ts.TotalMilliseconds);
            Debug.Log($"Save huatuo install version, path: {Config.HuatuoVersionPath}");
            File.WriteAllText(Config.HuatuoVersionPath, JsonUtility.ToJson(huatuoVersion, true), Encoding.UTF8);
        }
        public void SaveCacheDir()
        {
            huatuoVersion.CacheDir = PackageManager.Instance.CacheBasePath;
            File.WriteAllText(Config.HuatuoVersionPath, JsonUtility.ToJson(huatuoVersion, true), Encoding.UTF8);
        }
        public static HuatuoRemoteConfig GetVersionData()
        {
            var data = File.ReadAllText(Config.HuatuoVersionPath, Encoding.UTF8);
            return JsonUtility.FromJson<HuatuoRemoteConfig>(data);
        }
        public void InstallHuatuo()
        {
            var zipFileName = $"huatuo-{version.huatuoTag}";
            var zipPath = Path.Combine(PackageManager.Instance.CacheBasePath, $"{zipFileName}.zip");
            if (!File.Exists(zipPath))
            {
                var downloadUrl = GetDownUrlWithTagHuatuo();
                Debug.Log($"Download url: {downloadUrl}");
                Debug.Log($"Download {zipFileName} path {zipPath}");
                DownloadFile(downloadUrl, zipPath);
            }
            else
            {
                Debug.Log($"Download {zipFileName}, use cache file: {zipPath}");
            }

            var extractDir = @$"{zipFileName}/huatuo";
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp", "huatuo");
            Extract(zipPath, extractDir, installPath);
        }
        public void InstallIl2cpp()
        {
            var zipFileName = $"il2cpp_huatuo-{version.il2cppTag}";
            var zipPath = Path.Combine(PackageManager.Instance.CacheBasePath, $"{zipFileName}.zip");
            if (!File.Exists(zipPath))
            {
                var downloadUrl = GetDownUrlWithTagIl2cpp();
                Debug.Log($"Download il2cpp_huatuo url: {downloadUrl}");
                Debug.Log($"Download {zipFileName} path {zipPath}");
                DownloadFile(downloadUrl, zipPath);
            }
            else
            {
                Debug.Log($"Download {zipFileName}, use cache file: {zipPath}");
            }

            var extractDir = @$"{zipFileName}/libil2cpp";
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
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

        public bool DownloadFile(string URL, string filename)
        {
            downloadList.Add(filename);
            try
            {
                HttpWebRequest Myrq = (HttpWebRequest)HttpWebRequest.Create(URL);
                HttpWebResponse myrp = (HttpWebResponse)Myrq.GetResponse();
                Stream st = myrp.GetResponseStream();
                Stream so = new FileStream(filename, FileMode.Create);
                byte[] by = new byte[1024];
                int osize = st.Read(by, 0, (int)by.Length);
                while (osize > 0)
                {
                    so.Write(by, 0, osize);
                    osize = st.Read(by, 0, (int)by.Length);
                }
                so.Close();
                st.Close();
                myrp.Close();
                Myrq.Abort();
                return true;
            }
            catch (System.Exception _)
            {
                throw;
            }
        }
    }

}
