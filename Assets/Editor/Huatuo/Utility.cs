using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
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
    }
}
