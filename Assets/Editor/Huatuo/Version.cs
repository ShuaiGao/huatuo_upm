using System;

// Version.cs
//
// Author:
//   ldr123 (ldr12@163.com)
//

namespace Assets.Editor.Huatuo
{
    /// <summary>
    /// 这个类提供了Huatuo和IL2CPP相关的版本信息
    /// </summary>
    [Serializable]
    internal class HuatuoVersion
    {
        public string ver;
        public string commitid;
        public string Libil2cppUrl;
        public string HuatuoUrl;
        public string InstallTime;
        public long Timestamp;
    }

    /// <summary>
    /// 这个类提供了远程的Huatuo和IL2CPP版本信息
    /// </summary>
    [Serializable]
    internal class RemoteHuatuoVersion
    {
        public string ver = "";
        public string il2cppver = "";

        public RemoteHuatuoVersion(RemoteHuatuoVersion from)
        {
            if (from == null)
            {
                return;
            }

            ver = from.ver;
            il2cppver = from.il2cppver;
        }

        /// <summary>
        /// 判断两个版本是否一致
        /// </summary>
        public bool Compare(RemoteHuatuoVersion other)
        {
            if (other == null)
            {
                return false;
            }

            if (ver == other.ver && il2cppver == other.il2cppver)
            {
                return true;
            }

            if (Utility.CompareVersions(ver, other.ver) < 0)
            {
                return false;
            }
            
            if (Utility.CompareVersions(il2cppver, other.il2cppver) < 0)
            {
                return false;
            }

            return true;
        }
    }
}
