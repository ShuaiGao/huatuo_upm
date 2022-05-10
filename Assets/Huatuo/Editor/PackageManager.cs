using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace Huatuo.Editor
{
    internal class PackageManager
    {
        private static PackageManager instance = null;
        public static PackageManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PackageManager();
                }
                return instance;
            }
        }
        private static string CacheDirName = ".huatuo_cache";
        public string CacheBasePath;

        PackageManager()
        {
        }
        public void SetCacheDirectory(string path)
        {
            if (path == null || path.Length == 0)
            {
                CacheBasePath = Path.Combine(Path.GetFullPath("."), CacheDirName);
            }
            else
            {
                CacheBasePath = path;
            }
            Directory.CreateDirectory(CacheBasePath);
            Installer.Instance.SaveCacheDir();
        }
        public IEnumerator DownLoad(string url, string fileName, string hashCode)
        {
            var filePath = Path.Combine(CacheBasePath, fileName);
            if (File.Exists(filePath))
            {
                yield return null;
            }
            var itor = Utility.DownloadFile(url, filePath,
                        p =>
                        {
                            //EditorUtility.DisplayProgressBar("下载中...", $"{downloading}/{needDownload.Count}", p); 
                        },
                        ret =>
                        {
                            if (!string.IsNullOrEmpty(ret))
                            {
                                EditorUtility.DisplayDialog("错误", $"下载{fileName}出错.\n{ret}", "ok");
                            }
                        }, false);
            while (itor.MoveNext())
            {
                yield return itor.Current;
            }
        }
    }
}
