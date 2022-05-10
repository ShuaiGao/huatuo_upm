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
        public static string CacheDir = ".huatuo_cache";
        public string CacheBasePath;

        PackageManager()
        {
            // create cache dir
            if(!Directory.Exists(CacheBasePath))
            {
                CacheBasePath = Path.Combine(Path.GetFullPath("."), CacheDir);
                Directory.CreateDirectory(CacheBasePath);
            }
        }
        public IEnumerator DownLoad(string url, string fileName, string hashCode)
        {
            var filePath = Path.Combine(CacheBasePath, fileName);
            if (File.Exists(filePath))
            {
                yield return null;
            }
            bool haserr;
            var itor = Utility.DownloadFile(url, filePath,
                        p =>
                        {
                            //EditorUtility.DisplayProgressBar("下载中...", $"{downloading}/{needDownload.Count}", p); 
                        },
                        ret =>
                        {
                            if (!string.IsNullOrEmpty(ret))
                            {
                                haserr = true;
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
