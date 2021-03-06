using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Huatuo.Editor.BuildPipeline
{
    /// <summary>
    /// 这个类是用于在Huatuo的BuildPipeline过程中出现的异常进行捕获
    /// </summary>
    public static class HtBuildException
    {
        private static Action<bool, bool> _actBuildFinished = null;

        private static void BuildFinished(bool successful, bool willCancel = false)
        {
            AppDomain.CurrentDomain.UnhandledException -= UnhandledExceptionEventHandler;
            Application.logMessageReceived -= OnLogCallbackHandler;

            _actBuildFinished?.Invoke(successful, willCancel);
            _actBuildFinished = null;
        }

        private static void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Debug.LogError("[HtBuildPipeline]UnhandledExceptionEventHandler");

            BuildFinished(false);
        }

        private static void OnLogCallbackHandler(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception)
            {
                return;
            }
            BuildFinished(false, !condition.Contains("OperationCanceledException"));
        }

        /// <summary>
        /// 初始化异常捕捉
        /// </summary>
        /// <param name="finished">build流程结束后的回调，参数1：是否成功 参数2：是否需要终止整个build操作。</param>
        /// <returns>void</returns>
        public static void Init(Action<bool, bool> finished)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEventHandler;
            Application.logMessageReceived += OnLogCallbackHandler;

            _actBuildFinished = finished;
        }

        /// <summary>
        /// 关闭异常捕捉
        /// </summary>
        /// <returns>void</returns>
        public static void Destroy()
        {
            BuildFinished(true);
        }
    }
}
