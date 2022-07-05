using Huatuo.Editor;
using Huatuo.Editor.ThirdPart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Huatuo.Editor
{
    internal class HTEditorModel
    {
        private static HTEditorModel instance = null;

        public static HTEditorModel Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HTEditorModel();
                }

                return instance;
            }
        }

        internal HTEditorManger Manager;
        private HuatuoRemoteConfig m_remoteConfig = null;
        private List<CommitItem> m_commits = null;
        private List<TagItem> m_tags = null;
        private List<BranchItem> m_il2cpp_branchs = null;
        private EditorCoroutines.EditorCoroutine m_corFetchManifest = null;
        private EditorCoroutines.EditorCoroutine m_corFetchCommit = null;
        private EditorCoroutines.EditorCoroutine m_corFetchTag = null;
        private EditorCoroutines.EditorCoroutine m_corBranchs = null;

        internal bool IsBusy()
        {
            return m_corFetchManifest != null || m_corFetchCommit != null || m_corFetchTag != null || m_corBranchs != null;
        }

        internal InstallVersion GetRecommendVersion(bool forceTag)
        {
            var ret = new InstallVersion();
            //if (m_remoteConfig == null)
            //{
            //    return ret;
            //}

            //ret.il2cppTag = m_remoteConfig.il2cpp_recommend_version;

            if (forceTag)
            {
                ret.il2cppType = EFILE_NAME.IL2CPP;
                ret.huatuoType = EFILE_NAME.HUATUO;
                ret.huatuoTag = m_remoteConfig?.huatuo_recommend_version;
                return ret;
            }
            ret.il2cppType = EFILE_NAME.IL2CPP_BRANCH;
            ret.huatuoTag = m_remoteConfig?.huatuo_recommend_version;
            if (m_commits?.Count > 0)
            {
                if (m_commits[0].sha != m_remoteConfig?.huatuo_recommend_version_sha)
                {
                    // TODO 当推荐版本显示为sha，但是拉取代码却使用的head，故安装过程中有代码更新会导致看到的和更新到的不一致
                    ret.huatuoTag = $"{HTEditorConfig.HEAD}[{m_commits[0].sha.Substring(0, 6)}]";
                    ret.huatuoType = EFILE_NAME.HUATUO_MAIN;
                }
            }
            var branchName = HTEditorConfig.GetIl2cppBranchName();
            if(m_il2cpp_branchs != null)
            {
                foreach (var branch in m_il2cpp_branchs)
                {
                    if (branch.name == branchName)
                    {
                        ret.il2cppBranch = branchName;
                        ret.il2cppTag = branch.commit.GetShaShort();
                    }
                }
            }

            return ret;
        }
        internal List<string> GetHuatuoVersions()
        {
            return m_remoteConfig.huatuo_version;
        }
        internal List<string> GetIl2cppVersions()
        {
            return m_remoteConfig.il2cpp_version;
        }
        private string GetHuatuoTagSha(string tag)
        {
            foreach (var item in this.m_tags)
            {
                if (item.name == tag)
                {
                    return item.commit.sha;
                }
            }
            return "";
        }
        internal void fetchData()
        {
            //if (m_corFetchManifest != null)
            //{
            //    Manager.StopCoroutine(m_corFetchManifest.routine);
            //    m_corFetchManifest = null;
            //}

            //m_corFetchManifest = Manager.StartCoroutine(HTEditorUtility.HttpRequest<RemoteConfig>(
            //    HTEditorConfig.urlVersionConfig, (config, err) =>
            //    {
            //        if (string.IsNullOrEmpty(config.huatuo_recommend_version))
            //        {
            //            Debug.LogError("Unable to retrieve remote config.");
            //            err = $"【3】获取远程版本信息错误 1。";
            //        }

            //        if (!config.Equals(default(RemoteConfig)))
            //        {
            //            m_remoteConfig = new HuatuoRemoteConfig(config);
            //            if (!m_remoteConfig.unity_version.Contains(HTEditorConfig.UnityVersionDigits))
            //            {
            //                Manager.m_bVersionUnsported = true;
            //            }
            //        }

            //        if (!string.IsNullOrEmpty(err))
            //        {
            //            EditorUtility.DisplayDialog("错误", err, "ok");
            //        }

            //        InitHuatuoTagSha();
            //        m_corFetchManifest = null;
            //    }));

            if (m_corFetchCommit != null)
            {
                Manager.StopCoroutine(m_corFetchManifest.routine);
                m_corFetchCommit = null;
            }

            m_corFetchCommit = Manager.StartCoroutine(HTEditorUtility.HttpRequest<ItemSerial<CommitItem>>(HTEditorConfig.urlHuatuoCommits,
                (itemSerial, err) =>
                {
                    var logList = itemSerial.items;
                    if (logList == null || logList.Count == 0)
                    {
                        Debug.LogError("Unable to retrieve commit logs.");
                        err = $"【3】获取远程版本信息错误 2。";
                    }

                    if (!string.IsNullOrEmpty(err))
                    {
                        EditorUtility.DisplayDialog("错误", err, "ok");
                    }

                    this.m_commits = logList;
                    Debug.Log($"commit logs: {logList.ToString()}");
                    InitHuatuoTagSha();
                    m_corFetchCommit = null;
                }));

            m_corFetchTag = Manager.StartCoroutine(HTEditorUtility.HttpRequest<ItemSerial<TagItem>>(HTEditorConfig.urlHuatuoTags,
                (itemSerial, err) =>
                {
                    var tagList = itemSerial.items;
                    if (tagList == null)
                    {
                        Debug.LogError("Unable to retrieve tags.");
                        err = $"【3】获取tags失败。";
                    }

                    if (!string.IsNullOrEmpty(err))
                    {
                        EditorUtility.DisplayDialog("错误", err, "ok");
                    }

                    this.m_tags = tagList;
                    Debug.Log($"tags: {tagList}");
                    InitHuatuoTagSha();
                    m_corFetchTag = null;
                }));

            m_corBranchs = Manager.StartCoroutine(HTEditorUtility.HttpRequest<ItemSerial<BranchItem>>(HTEditorConfig.urlIl2cppBranchs,
                (itemSerial, err) =>
                {
                    var branchs = itemSerial.items;
                    if (branchs == null || branchs.Count == 0)
                    {
                        Debug.LogError("Unable to retrieve il2cpp branchs.");
                        err = $"【3】获取tags失败。";
                    }

                    if (!string.IsNullOrEmpty(err))
                    {
                        EditorUtility.DisplayDialog("错误", err, "ok");
                    }

                    this.m_il2cpp_branchs = branchs;
                    Debug.Log($"branchs: {branchs}");
                    m_corBranchs = null;
                }));
        }
        private void InitHuatuoTagSha()
        {
            if (m_remoteConfig == null || m_tags == null || m_commits == null)
            {
                return;
            }
            // 选择其它版本是，默认使用推荐版本
            for (var i = 0; i < m_remoteConfig.huatuo_version.Count; i++)
            {
                if (m_remoteConfig.huatuo_version[i] == m_remoteConfig.huatuo_recommend_version)
                {
                    Manager.m_nSelectedhuatuoVersionIndex = i;
                }
            }
            m_remoteConfig.InitRecommendTagSha();
        }
    }
}
