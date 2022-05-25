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
        private static readonly string DefaultAssetPath = "Assets/Huatuo/Editor/Resources/BuildSettings.asset";

        [HideInInspector] public bool Enable = true;
        [HideInInspector] public string HuatuoVersion = "";
        [HideInInspector] public string HuatuoIl2CPPVersion = "";
        [HideInInspector] public string HuatuoHelperPath = "";

        private static HtBuildSettings _settings = null;

        public static HtBuildSettings Instance
        {
            get
            {
                if (_settings != null)
                {
                    if (string.IsNullOrEmpty(_settings.HuatuoHelperPath))
                    {
                        _settings.HuatuoHelperPath = HTEditorConfig.HuatuoHelperPath;
                        AssetDatabase.SaveAssets();
                    }

                    return _settings;
                }

                _settings = AssetDatabase.LoadAssetAtPath<HtBuildSettings>(DefaultAssetPath);
                if (_settings != null)
                {
                    if (string.IsNullOrEmpty(_settings.HuatuoHelperPath))
                    {
                        _settings.HuatuoHelperPath = HTEditorConfig.HuatuoHelperPath;
                        AssetDatabase.SaveAssets();
                    }

                    return _settings;
                }

                HTEditorUtility.EnsureFilePath(DefaultAssetPath);
                _settings = CreateInstance<HtBuildSettings>();
                _settings.HuatuoHelperPath = HTEditorConfig.HuatuoHelperPath;
                AssetDatabase.CreateAsset(_settings, DefaultAssetPath);
                AssetDatabase.SaveAssets();

                return _settings;
            }
        }
    }
}
