using Huatuo.Editor.BuildPipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Huatuo.Editor
{
    internal class HTEditorMenu
    {
        [MenuItem("HuaTuo/Manager...", false, 3)]
        public static void ShowManager()
        {
            var win = HTEditorManger.GetWindow<HTEditorManger>(true);
            win.titleContent = new GUIContent("Huatuo Manager");
            win.ShowUtility();
        }
        [MenuItem("HuaTuo/Manager...", true)]
        public static bool CheckHuatuo()
        {
            Menu.SetChecked("HuaTuo/Enable Huatuo", HtBuildSettings.Instance.Enable);
            return true;
        }

        [MenuItem("HuaTuo/Enable Huatuo", false, 5)]
        public static void EnableHuatuo()
        {
            HtBuildSettings.ReverseEnable();
        }

        [MenuItem("HuaTuo/卸载 0.1.x huatuo安装版本", false, 5)]
        public static void RemoveOldHuatuo()
        {
            var huatuoOldVersion = Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), ".huatuo");
            if(File.Exists(huatuoOldVersion))
            {
                File.Delete(huatuoOldVersion);
            }

            if (Directory.Exists(HTEditorConfig.Libil2cppOritinalPath))
            {
                if (Directory.Exists(HTEditorConfig.Libil2cppPath))
                {
                    Directory.Delete(HTEditorConfig.Libil2cppPath, true);
                }
                HTEditorUtility.Mv(HTEditorConfig.Libil2cppOritinalPath, HTEditorConfig.Libil2cppPath);
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "未找到使用huatuo tookit安装的旧版本", "ok");
                return;
            }

            EditorUtility.DisplayDialog("提示", "卸载完成！", "ok");
        }
    }
}
