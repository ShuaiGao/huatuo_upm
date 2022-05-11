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
    internal class HTEditorCache
    {
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
        public string CacheBasePath;

        HTEditorCache()
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
            HTEditorInstaller.Instance.SaveCacheDir();
        }
        public IEnumerator DownLoad(string url, string fileName, string hashCode)
        {
            var filePath = Path.Combine(CacheBasePath, fileName);
            if (File.Exists(filePath))
            {
                yield return null;
            }
            var itor = HTEditorUtility.DownloadFile(url, filePath,
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
