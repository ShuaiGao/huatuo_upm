using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Huatuo.Editor.ThirdPart.ICSharpCode.SharpZipLib.Zip;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;


namespace Huatuo.Editor
{
    /// <summary>
    /// 这个类是Huatuo编辑器中使用到的各种小工具
    /// </summary>
    public static class HTEditorUtility
    {
        /// <summary>
        /// Compares two versions to see which is greater.
        /// </summary>
        /// <param name="a">Version to compare against second param</param>
        /// <param name="b">Version to compare against first param</param>
        /// <returns>-1 if the first version is smaller, 1 if the first version is greater, 0 if they are equal</returns>
        public static int CompareVersions(string a, string b)
        {
            var versionA = VersionStringToInts(a);
            var versionB = VersionStringToInts(b);
            for (var i = 0; i < Mathf.Max(versionA.Length, versionB.Length); i++)
            {
                if (VersionPiece(versionA, i) < VersionPiece(versionB, i))
                    return -1;
                if (VersionPiece(versionA, i) > VersionPiece(versionB, i))
                    return 1;
            }

            return 0;
        }

        private static int VersionPiece(IList<int> versionInts, int pieceIndex)
        {
            return pieceIndex < versionInts.Count ? versionInts[pieceIndex] : 0;
        }

        private static int[] VersionStringToInts(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return new[] {0};
            }

            int piece;
            return version.Split('.')
                .Select(v => int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out piece) ? piece : 0)
                .ToArray();
        }

        /// <summary>
        /// 异步解压zip文件
        /// </summary>
        /// <param name="zipFile">zip文件</param>
        /// <param name="destDir">解压后的目录</param>
        /// <param name="begin">开始解压</param>
        /// <param name="progress">解压中的进度</param>
        /// <param name="complete">解压结束</param>
        /// <param name="failure">解压失败</param>
        /// <returns>协程</returns>
        public static IEnumerator UnzipAsync(string zipFile, string destDir, Action<int> begin,
            Action<int> progress, Action complete, Action failure)
        {
            if (Directory.Exists(destDir))
            {
                Directory.Delete(destDir, true);
            }

            Debug.Log($"[UnzipAsync]----:{zipFile} {destDir}");
            var tmpCnt = 0;

            var itor = FastZip.ExtractZip(zipFile, destDir, count =>
            {
                tmpCnt = count;
                Debug.Log($"[UnzipAsync] begin:{zipFile} {destDir}");
                begin?.Invoke(count);
                progress?.Invoke(0);
            }, progress, () =>
            {
                Debug.Log($"[UnzipAsync] complete:{zipFile} {destDir}");
                progress?.Invoke(tmpCnt);
                complete?.Invoke();
            }, () =>
            {
                Debug.Log($"[UnzipAsync] failure:{zipFile} {destDir}");
                failure?.Invoke();
            });
            while (itor.MoveNext())
            {
                yield return itor.Current;
            }
        }

        /// <summary>
        /// 移动目录
        /// </summary>
        /// <param name="src">源目录</param>
        /// <param name="dst">目标目录</param>
        /// <returns>错误信息</returns>
        public static string Mv(string src, string dst)
        {
            var ret = "";
            if (!Directory.Exists(src))
            {
                ret = $"Can't Find {src}";
            }
            else if (Directory.Exists(dst))
            {
                ret = $"{dst} Already Exists!";
            }
            else
            {
                Directory.Move(src, dst);
            }

            return ret;
        }

        /// <summary>
        /// 对https的请求不做证书验证
        /// </summary>
        private class IgnoreHttps : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="strUrl">来源url</param>
        /// <param name="strDstFile">解压后的目录</param>
        /// <param name="progress">下载进度</param>
        /// <param name="done">下载完成，参数是错误信息</param>
        /// <param name="bValidateCertificate">是否选择忽略证书检查</param>
        /// <returns>协程</returns>
        public static IEnumerator DownloadFile(string strUrl, string strDstFile,
            Action<float> progress, Action<string> done, int retryCnt = 3, bool bValidateCertificate = false)
        {
            var nPos = 0;
            retryCnt = Math.Max(1, retryCnt);

            var err = "";
            do
            {
                nPos++;
                
                Debug.Log($"[DownloadFile]{strUrl}\ndest:{strDstFile}");
                if (File.Exists(strDstFile))
                {
                    File.Delete(strDstFile);
                }

                yield return null;

                using var www = new UnityWebRequest(strUrl)
                {
                    downloadHandler = new DownloadHandlerFile(strDstFile),
                    timeout = 1000
                };

                if (!bValidateCertificate)
                {
                    www.certificateHandler = new IgnoreHttps();
                }

                progress?.Invoke(0f);
                var req = www.SendWebRequest();
                while (!req.isDone)
                {
                    progress?.Invoke(req.progress);
                    yield return null;
                }

                err = www.error;
                if (string.IsNullOrEmpty(www.error))
                {
                    break;
                }
            } while (nPos < retryCnt);


            done?.Invoke(err);
        }

        public static IEnumerator HttpRequest(string url, bool silent, Action<RemoteConfig> callback, int retryCnt = 3,
            bool bValidateCertificate = false)
        {
            var nPos = 0;
            retryCnt = Math.Max(1, retryCnt);

            RemoteConfig ret = default;
            do
            {
                nPos++;

                Debug.Log($"Fetching {url} retry:{nPos - 1}");
                using var www = new UnityWebRequest(url)
                {
                    downloadHandler = new DownloadHandlerBuffer(),
                    timeout = 100
                };

                if (!bValidateCertificate)
                {
                    www.certificateHandler = new IgnoreHttps();
                }

                yield return www.SendWebRequest();
                do
                {
                    if (!string.IsNullOrEmpty(www.error))
                    {
                        Debug.LogError(www.error);
                        if (!silent)
                        {
                            EditorUtility.DisplayDialog("错误", $"【1】获取远程版本信息错误。\n[{www.error}]", "ok");
                        }

                        break;
                    }

                    var json = www.downloadHandler.text;
                    if (string.IsNullOrEmpty(json))
                    {
                        Debug.LogError("Unable to retrieve SDK version manifest.  Showing installed SDKs only.");
                        if (!silent)
                        {
                            EditorUtility.DisplayDialog("错误", $"【2】获取远程版本信息错误。", "ok");
                        }

                        break;
                    }

                    ret = JsonUtility.FromJson<RemoteConfig>(json);
                    if (string.IsNullOrEmpty(ret.huatuo_recommend_version))
                    {
                        Debug.LogError("Unable to retrieve SDK version manifest.  Showing installed SDKs only.");
                        if (!silent)
                        {
                            EditorUtility.DisplayDialog("错误", $"【2】获取远程版本信息错误。", "ok");
                        }

                        break;
                    }
                } while (false);

                if (!ret.Equals(default(RemoteConfig)))
                {
                    break;
                }
            } while (nPos < retryCnt);

            callback?.Invoke(ret);
        }
    }
}
