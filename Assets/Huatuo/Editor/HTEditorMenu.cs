using System;
using System.Collections.Generic;
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
    }
}
