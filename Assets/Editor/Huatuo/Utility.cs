using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Editor.Huatuo.ThirdPart.ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

// Utility.cs
//
// Author:
//   ldr123 (ldr12@163.com)
//

namespace Editor.Huatuo
{
    /// <summary>
    /// 这个类是Huatuo编辑器中使用到的各种小工具
    /// </summary>
    public static class Utility
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
    }
}
