using System;
using System.Collections.Generic;
using UnityEditorInternal;

// Version.cs
//
// Author:
//   ldr123 (ldr12@163.com)
//

namespace Huatuo.Editor
{
    [Serializable]
    internal struct HuatuoVersion
    {
        public string huatuoTag;
        public string libil2cppTag;
        public string ver;
        public string commitid;
        public string Libil2cppUrl;
        public string HuatuoUrl;
        public string InstallTime;
        public long Timestamp;
    }
    internal struct InstallVersion
    {
        public string il2cppTag;
        public string huatuoTag;
    }
    /// <summary>
    /// 这个类提供了Huatuo和IL2CPP相关的版本信息
    /// </summary>
    [Serializable]
    internal class HuatuoRemoteConfig
    {
        public string ver = "";
        public string hash = "";
        public List<string> unity_version;
        public List<string> huatuo_version;
        public List<string> il2cpp_version;
        public List<string> il2cpp_min_version;
        public List<string> huatuo_deprecated_version;
        public List<string> il2cpp_deprecated_version;
        public List<string> il2cpp_recommend_version;
        public string huatuo_recommend_version;
        public string huatuo_min_version;
        private string use_huatuo_version;
        private string use_il2cpp_version;

        public string GetIl2cppRecommendVersion()
        {
            foreach (string version in il2cpp_recommend_version)
            {
                if(version.StartsWith(InternalEditorUtility.GetUnityVersionDigits()))
                {
                    return version;
                }
            }
            return null;
        }

        public HuatuoRemoteConfig(HuatuoRemoteConfig from)
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
        public bool Compare(HuatuoRemoteConfig other)
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
