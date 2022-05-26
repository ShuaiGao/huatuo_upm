using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Il2Cpp;
using UnityEditor.UnityLinker;
using UnityEngine.SceneManagement;
using UnityEngine;

#if UNITY_ANDROID
    using UnityEditor.Android;
#endif

namespace Huatuo.Editor.BuildPipeline
{
    /// <summary>
    /// 这个类是Huatuo的BuildPipeline，用于在导出项目的时候对Huatuo进行相关的资源支持
    /// </summary>
    public class HtBuildPipeline/* : IPreprocessBuildWithReport, IProcessSceneWithReport, IFilterBuildAssemblies,
        IPostBuildPlayerScriptDLLs, IIl2CppProcessor, IUnityLinkerProcessor,
#if UNITY_ANDROID
        IPostGenerateGradleAndroidProject,
#endif
        IPostprocessBuildWithReport*/
    {
        public int callbackOrder => 1;

        private static MethodInfo _sBuildReportAddMessage = null;

        // int IOrderedCallback.callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log($"[HtBuildPipeline]OnPreprocessBuild");

            if (!HtBuildSettings.Instance.Enable)
            {
                Debug.Log($"[HtBuildPipeline]Huatuo has been disabled!");
                return;
            }

            if (_sBuildReportAddMessage == null)
            {
                _sBuildReportAddMessage =
                    typeof(BuildReport).GetMethod("AddMessage", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            HtBuildException.Init((successful, willCancel) =>
            {
                var msg = $"[HtBuildPipeline]BuildFinished:{(successful ? "Successful" : "Failed")}";

                Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", "");
                if (successful)
                {
                    Debug.Log(msg);
                }
                else
                {
                    Debug.LogError(msg);
                }

                if (!willCancel)
                {
                    return;
                }

                EditorUtility.DisplayDialog("错误", "有异常发生，请根据控制台提示修正对应错误!", "确定");
                _sBuildReportAddMessage.Invoke(report,
                    new object[] {LogType.Exception, "用户取消", "BuildFailedException"});
            });

            //准备il2cpp环境
            HTEditorInstaller.Instance.Prepare(ret =>
            {
                if (ret)
                {
                    return;
                }

                throw new Exception("Huatuo环境准备失败，请打开Huatuo Manager进行检查。");
            });

            //检查huatuo是否存在
            HTEditorInstaller.Instance.CheckHuatuo(ret =>
            {
                if (string.IsNullOrEmpty(ret))
                {
                    Debug.Log("Huatuo 准备ok");
                }
                else
                {
           //         throw new Exception($"Huatuo环境准备失败，请打开Huatuo Manager进行检查。\n{ret}");
                }
            });
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            //      Debug.Log($"[HtBuildPipeline]OnProcessScene");
        }

        public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
        {
            //       Debug.Log($"[HtBuildPipeline]OnFilterAssemblies");
            return assemblies;
        }

        public void OnPostBuildPlayerScriptDLLs(BuildReport report)
        {
            //        Debug.Log($"[HtBuildPipeline]OnPostBuildPlayerScriptDLLs");
        }

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            //        Debug.Log($"[HtBuildPipeline]GenerateAdditionalLinkXmlFile");
            return string.Empty;
        }

        public void OnBeforeRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            Debug.Log($"[HtBuildPipeline]OnBeforeRun");

            if (!HtBuildSettings.Instance.Enable)
            {
                Debug.Log($"[HtBuildPipeline]Huatuo has been disabled!");
                return;
            }

            //如果启用了huatuo，需要对环境变量进行配置
            var newIl2cppPath = Path.Combine(HtBuildSettings.Instance.HuatuoHelperPath, "il2cpp/");
            newIl2cppPath = Path.GetFullPath(newIl2cppPath).Replace('\\', '/');
            if (!Directory.Exists(newIl2cppPath))
            {
                throw new FileNotFoundException("Huatuo相关支持文件未找到，请打开Huatuo Manager进行检查。");
            }

            Environment.SetEnvironmentVariable("UNITY_IL2CPP_PATH", newIl2cppPath);
        }

        public void OnAfterRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            //           Debug.Log($"[HtBuildPipeline]OnAfterRun");
        }

        public void OnBeforeConvertRun(BuildReport report, Il2CppBuildPipelineData data)
        {
            //           Debug.Log($"[HtBuildPipeline]OnBeforeConvertRun");
        }

#if UNITY_ANDROID
        public void OnPostGenerateGradleAndroidProject(string path)
        {
 //           Debug.Log($"[HtBuildPipeline]OnPostGenerateGradleAndroidProject");
        }
#endif

        public void OnPostprocessBuild(BuildReport report)
        {
            Debug.Log($"[HtBuildPipeline]OnPostprocessBuild");

            HtBuildException.Destroy();
        }
    }
}
