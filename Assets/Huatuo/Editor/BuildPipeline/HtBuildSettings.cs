using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Huatuo.Editor.BuildPipeline
{
    [Serializable]
    public class HtBuildSettings : ScriptableObject
    {
        private static readonly string DefaultAssetPath = "Assets/Editor/HuatuoBuildSettings.asset";

        //[HideInInspector] public bool Enable = false;
        //[HideInInspector] public string HuatuoVersion = "";
        //[HideInInspector] public string HuatuoIl2CPPVersion = "";
        //[HideInInspector] public string HuatuoHelperPath = "";

        public bool Enable = false;
        //public string HuatuoVersion = "";
        //public string HuatuoIl2CPPVersion = "";

        private static HtBuildSettings _settings = null;

        public static void ReverseEnable()
        {
            HtBuildSettings.Instance.Enable = !HtBuildSettings.Instance.Enable;
            AssetDatabase.SaveAssets();
        }

        public static HtBuildSettings Instance
        {
            get
            {
                if (_settings != null)
                {
                    //if (string.IsNullOrEmpty(_settings.HuatuoHelperPath))
                    //{
                    //    _settings.HuatuoHelperPath = HTEditorConfig.Instance.HuatuoHelperPath;
                    //    AssetDatabase.SaveAssets();
                    //}
                    return _settings;
                }

                _settings = AssetDatabase.LoadAssetAtPath<HtBuildSettings>(DefaultAssetPath);
                if (_settings != null)
                {
                    //if (string.IsNullOrEmpty(_settings.HuatuoHelperPath))
                    //{
                    //    _settings.HuatuoHelperPath = HTEditorConfig.Instance.HuatuoHelperPath;
                    //    AssetDatabase.SaveAssets();
                    //}
                    return _settings;
                }
                HTEditorUtility.EnsureFilePath(DefaultAssetPath);
                _settings = CreateInstance<HtBuildSettings>();
                //_settings.HuatuoHelperPath = HTEditorConfig.Instance.HuatuoHelperPath;
                AssetDatabase.CreateAsset(_settings, DefaultAssetPath);
                AssetDatabase.SaveAssets();

                return _settings;
            }
        }
    }
}
