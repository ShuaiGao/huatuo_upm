using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Editor.Huatuo
{
    internal class Installer
    {
        private string baseUrl;
        private bool useGithub;
        private string libil2cppPrefix;
        private string huatuoPrefix;

        private HashSet<string> versionSet = new HashSet<string>() { "2020.3.33", "2021.3.1", "2020.3.7", "2020.3.9" };
        private string libil2cppPrefixGitee = "https://gitee.com/juvenior/il2cpp_huatuo/repository/archive";
        private string libil2cppPrefixGithub = "https://github.com/pirunxi/il2cpp_huatuo/archive/refs/heads";
        private string huatuoPrefixGitee = "https://gitee.com/focus-creative-games/huatuo/repository/archive";
        private string huatuoPrefixGithub = "https://github.com/focus-creative-games//huatuo/archive/refs/heads";

        private List<string> downloadList = new List<string>();

        public Installer(bool github)
        {
            useGithub = github;
            libil2cppPrefix = github ? libil2cppPrefixGithub : libil2cppPrefixGitee;
            huatuoPrefix = github ? huatuoPrefixGithub : huatuoPrefixGitee;
        }

        public static string PathIl2cpp
        {
            get
            {
                string str7 = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                return Path.Combine(str7, "Data", "il2cpp");
            }
        }
        public static string UnityVersion
        {
            get
            {
                return Application.unityVersion.Split('f')[0];
            }
        }
        public static string HuatuoVersionPath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, ".huatuo");
            }
        }
        public string GetDownUrlLibil2cpp()
        {
            return $@"{libil2cppPrefix}/{UnityVersion}.zip";
        }
        public string GetDownUrlHuatuo()
        {
            return $"{huatuoPrefix}/main.zip";
        }
        public string GetDownUrlWithTag(string tag)
        {
            return @$"{libil2cppPrefix}/il2cpp_huatuo/archive/refs/tags/{tag}.zip";
        }
        public static bool CheckIl2cpp()
        {
            return Directory.Exists(PathIl2cpp);
        }
        public void Install()
        {
            if (!CheckSupport())
            {
                return;
            }
            try
            {
                BackupLibil2cpp();
                InstallLibil2cpp();
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
                ClearCache();
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
            //if (!useGithub)
            //{
            //    Debug.LogError("Not Support gitee， Please use github!!!");
            //    return false;
            //}
            // TODO unity版本判断
            var version = Application.unityVersion.Split('f')[0];
            if (!versionSet.Contains(version))
            {
                Debug.LogError($"Not Support unity version{Application.unityVersion}");
                return false;
            }
            // TODO 检查libil2cpp, huatuo 版本，避免不必要的更新
            return true;
        }
        public static void UnBackupLibil2cpp()
        {
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
            string installPathBak = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp_bak");
            // backup libil2cpp
            if (!Directory.Exists(installPathBak))
            {
                Directory.Move(installPathBak, installPath);
            }
        }
        public static void BackupLibil2cpp()
        {
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
            string installPathBak = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp_bak");
            // backup libil2cpp
            if (!Directory.Exists(installPathBak))
            {
                Directory.Move(installPath, installPathBak);
            }
        }
        public void SaveVersionLog()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);

            // TODO 记录libil2cpp 和 huatuo 版本信息
            var data = new HuatuoVersion();
            data.Libil2cppUrl = GetDownUrlLibil2cpp();
            data.HuatuoUrl = GetDownUrlHuatuo();
            data.InstallTime = DateTime.Now.ToString();
            data.Timestamp = Convert.ToInt64(ts.TotalMilliseconds);
            Debug.Log($"Save huatuo install version, path: {HuatuoVersionPath}");
            File.WriteAllText(HuatuoVersionPath, JsonUtility.ToJson(data, true), Encoding.UTF8);
        }
        public static HuatuoVersion GetVersionData()
        {
            var data = File.ReadAllText(HuatuoVersionPath, Encoding.UTF8);
            return JsonUtility.FromJson<HuatuoVersion>(data);
        }
        public void InstallHuatuo()
        {
            string basePath = Path.GetFullPath(".");
            var zipPath = Path.Combine(basePath, "huatuo-main.zip");
            var downloadUrl = GetDownUrlHuatuo();
            Debug.Log($"Download url: {downloadUrl}");
            Debug.Log($"Download huatuo.zip path {zipPath}");
            DownloadFile(downloadUrl, zipPath);

            var extractDir = @$"huatuo-main/huatuo";
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp", "huatuo");
            Extract(zipPath, extractDir, installPath);
        }
        public void InstallLibil2cpp()
        {
            string basePath = Path.GetFullPath(".");
            var zipPath = Path.Combine(basePath, "libil2cpp_huatuo.zip");
            var downloadUrl = GetDownUrlLibil2cpp();
            Debug.Log($"Download libil2cpp_huatuo url: {downloadUrl}");
            Debug.Log($"Download libil2cpp_huatuo.zip path {zipPath}");
            DownloadFile(downloadUrl, zipPath);

            var extractDir = @$"il2cpp_huatuo-{UnityVersion}/libil2cpp";
            string installPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Data", "il2cpp", "libil2cpp");
            Extract(zipPath, extractDir, installPath);
        }

        public static void Extract(string zipPath, string extractDir, string installPath)
        {
            var p = ExtractZip(zipPath, extractDir, installPath);
            foreach (var entry in p)
            {
                Debug.Log(entry);
            }
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
            catch (System.Exception e)
            {
                throw;
            }
        }
    }

}
