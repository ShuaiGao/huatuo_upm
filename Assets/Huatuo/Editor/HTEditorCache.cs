using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Huatuo.Editor
{
    enum EFILE_NAME
    {
        NONE,
        HUATUO,
        IL2CPP,
        HUATUO_MAIN,
        IL2CPP_BRANCH,
    }

    internal class HTEditorCache
    {
        private string libil2cppTagPrefix;
        private string huatuoTagPrefix;
        private int m_nDownloadTotal;
        private int m_counter;
        private int m_nSuccessCount;
        private int m_nFailedCount;
        private Dictionary<Type, string> m_dictCacheName = new Dictionary<Type, string>
        {
            [typeof(RemoteConfig)] = $"{nameof(RemoteConfig)}.json",
            [typeof(ItemSerial<TagItem>)] = $"{nameof(TagItem)}.json",
            [typeof(ItemSerial<CommitItem>)] = $"{nameof(CommitItem)}.json",
            [typeof(ItemSerial<BranchItem>)] = $"{nameof(BranchItem)}.json",
        };

        private static HTEditorCache instance = null;

        public static HTEditorCache Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HTEditorCache();
                }

                return instance;
            }
        }

        private static string CacheDirName = ".huatuo_cache";
        //private static string CacheDirName = "cache";
        public string CacheBasePath;

        HTEditorCache()
        {
            libil2cppTagPrefix = HTEditorConfig.libil2cppTagPrefixGithub;
            huatuoTagPrefix = HTEditorConfig.huatuoTagPrefixGithub;
        }

        public void SetDownloadCount(int count)
        {
            m_nDownloadTotal = count;
            m_counter = 0;
            m_nSuccessCount = 0;
            m_nFailedCount = 0;
        }
        public void SaveVersionJson<T>(T data)
        {
            var fileName = m_dictCacheName[typeof(T)];
            File.WriteAllText(Path.Combine(CacheBasePath, fileName), JsonUtility.ToJson(data, true), Encoding.UTF8);
        }
        public T LoadVersionJson<T>()
        {
            var fileName = m_dictCacheName[typeof(T)];
            var txt = File.ReadAllText(Path.Combine(CacheBasePath, fileName));
            if (string.IsNullOrEmpty(txt))
            {
                throw new Exception("no cache data");
            }

            return JsonUtility.FromJson<T>(txt);
        }

        public bool DownloadDone()
        {
            return m_nDownloadTotal == m_nFailedCount + m_nSuccessCount;
        }

        public bool DownloadSuccess()
        {
            return m_nDownloadTotal == m_nSuccessCount;
        }

        public void SetCacheDirectory(string path)
        {
            var tmp = "";
            if (path == null || path.Length == 0)
            {
                tmp = Path.Combine(Path.GetFullPath("Library"), CacheDirName);
                //tmp = Path.Combine(HTEditorConfig.HuatuoHelperPath, CacheDirName);
            }
            else
            {
                tmp = path;
            }

            try
            {
                Directory.CreateDirectory(tmp);
                CacheBasePath = tmp;
                HTEditorInstaller.Instance.SaveCacheDir();
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.LogError("缓存设置失败，请不要使用C盘路径做缓存");
                Debug.LogException(ex);
            }
        }

        public string GetDownUrlWithTagHuatuo(string tag)
        {
            return @$"{huatuoTagPrefix}/{tag}.zip";
        }

        public string GetDownUrlWithTagIl2cpp(string tag)
        {
            return @$"{libil2cppTagPrefix}/{tag}.zip";
        }

        public string GetZipName(EFILE_NAME nameType, string tag)
        {
            var zipFileName = "";
            switch (nameType)
            {
                case EFILE_NAME.HUATUO:
                    zipFileName = $"huatuo-{tag}";
                    break;
                case EFILE_NAME.IL2CPP:
                    zipFileName = $"il2cpp_huatuo-{tag}";
                    break;
                default:
                    throw new Exception($"no support file type{nameof(nameType)}");
            }

            return zipFileName;
        }
        public static string GetZipInnerFolder(EFILE_NAME nameType, InstallVersion version)
        {
            switch (nameType)
            {
                case EFILE_NAME.IL2CPP:
                    return $"il2cpp_huatuo-{version.il2cppTag}/libil2cpp";
                case EFILE_NAME.IL2CPP_BRANCH:
                    return $"il2cpp_huatuo-{version.il2cppBranch}/libil2cpp";
                case EFILE_NAME.HUATUO_MAIN:
                    return $"huatuo-main/huatuo";
                case EFILE_NAME.HUATUO:
                    return $"huatuo-{version.huatuoTag}/huatuo";
            }
            return "error param";
        }
        public string GetZipPath(EFILE_NAME nameType, InstallVersion version)
        {
            var zipFileName = "";
            switch (nameType)
            {
                case EFILE_NAME.HUATUO_MAIN:
                    zipFileName = $"huatuo-{version.huatuoTag}";
                    break;
                case EFILE_NAME.HUATUO:
                    zipFileName = $"huatuo-{version.huatuoTag}";
                    break;
                case EFILE_NAME.IL2CPP_BRANCH:
                    zipFileName = $"il2cpp_huatuo-{version.il2cppBranch}-{version.il2cppTag}";
                    break;
                case EFILE_NAME.IL2CPP:
                    zipFileName = $"il2cpp_huatuo-{version.il2cppTag}";
                    break;
                default:
                    throw new Exception($"no support file type{nameof(nameType)}");
            }

            return Path.Combine(CacheBasePath, $"{zipFileName}.zip");
        }

        public IEnumerator GetCache(EFILE_NAME nameType, InstallVersion version, string hashCode)
        {
            m_counter++;
            var downloadUrl = "";
            var zipFileName = "";
            switch (nameType)
            {
                case EFILE_NAME.HUATUO_MAIN:
                    zipFileName = $"huatuo-{version.huatuoTag}";
                    downloadUrl = @$"{HTEditorConfig.huatuoPrefixGithub}/main.zip";
                    break;
                case EFILE_NAME.HUATUO:
                    zipFileName = $"huatuo-{version.huatuoTag}";
                    downloadUrl = @$"{huatuoTagPrefix}/{version.huatuoTag}.zip";
                    break;
                case EFILE_NAME.IL2CPP_BRANCH:
                    zipFileName = $"il2cpp_huatuo-{version.il2cppBranch}-{version.il2cppTag}";
                    downloadUrl = @$"{HTEditorConfig.libil2cppPrefixGithub}/{version.il2cppBranch}.zip";
                    break;
                case EFILE_NAME.IL2CPP:
                    zipFileName = $"il2cpp_huatuo-{version.il2cppTag}";
                    downloadUrl = @$"{libil2cppTagPrefix}/{version.il2cppTag}.zip";
                    break;
                default:
                    throw new Exception($"no support file type{nameof(nameType)}");
            }

            var downloadErr = false;
            var zipPath = Path.Combine(CacheBasePath, $"{zipFileName}.zip");
            if (File.Exists(zipPath))
            {
                // TODO 校验文件MD5
                Debug.Log($"Download {zipFileName}, use cache file: {zipPath}");
                yield return null;
            }
            else
            {
                var curRetryCnt = 0;
                var maxRetryCnt = 0;
                var itor = HTEditorUtility.DownloadFile(downloadUrl, zipPath,
                    (curCnt, maxCnt) =>
                    {
                        curRetryCnt = curCnt;
                        maxRetryCnt = maxCnt;
                    },
                    p =>
                    {
                        var msg = $"下载中{(curRetryCnt > 0 ? $"[重试{curRetryCnt}/{maxRetryCnt}]" : "...")}";
                        EditorUtility.DisplayProgressBar(msg, $"{m_counter}/{m_nDownloadTotal}", p);
                    },
                    ret =>
                    {
                        EditorUtility.ClearProgressBar();
                        if (!string.IsNullOrEmpty(ret))
                        {
                            downloadErr = true;
                            EditorUtility.DisplayDialog("错误", $"下载{zipFileName}出错.\n{ret}", "ok");
                        }
                    });
                while (itor.MoveNext())
                {
                    yield return itor.Current;
                }

                if (!File.Exists(zipPath))
                {
                    EditorUtility.DisplayDialog("错误", $"下载的文件{zipPath}不存在", "ok");
                    downloadErr = false;
                }

                //else if (MD5.ComputeFileMD5(zipPath).ToLower() != hashCode)
                //{
                //    EditorUtility.DisplayDialog("错误", $"下载的文件{zipPath} hash不匹配，请重新下载", "ok");
                //    downloadErr = false;
                //}
            }

            if (downloadErr)
            {
                m_nFailedCount++;
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
            }
            else
            {
                m_nSuccessCount++;
            }
        }
    }
}
