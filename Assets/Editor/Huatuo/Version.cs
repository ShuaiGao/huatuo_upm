using System;
using System.Collections.Generic;

// Version.cs
//
// Author:
//   ldr123 (ldr12@163.com)
//

namespace Assets.Editor.Huatuo
{
    [Serializable]
    internal struct VersionConfig
    {
    }
    /// <summary>
    /// 这个类提供了Huatuo和IL2CPP相关的版本信息
    /// </summary>
    [Serializable]
    internal class HuatuoVersion
    {
        public string ver = "";
        public string hash = "";
       
        public HuatuoVersion(HuatuoVersion from)
        {
            if (from == null)
            {
                return;
            }

            ver = from.ver;
        }

        /// <summary>
        /// 判断两个版本是否一致
        /// </summary>
        public bool Compare(HuatuoVersion other)
        {
            if (other == null)
            {
                return false;
            }
            if (ver == other.ver && hash == other.hash)
            {
                return true;
            }

            return Utility.CompareVersions(ver, other.ver) >= 0;
        }
    }
}
