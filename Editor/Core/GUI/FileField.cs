﻿/*********************************************************************************
 *Author:         OnClick
 *Version:        0.0.2.344
 *UnityVersion:   2019.4.22f1c1
 *Date:           2021-08-12
 *Description:    IFramework
 *History:        2018.11--
*********************************************************************************/
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace IFramework
{
    public class FileField
    {
        public event Action<string> onValueChange;
        [SerializeField] private string _path;
        public string path { get { return _path; } }
        public string title;
        public string folder;
        public string extension;
        public FileField(string path = "", string folder = "Assets", string title = "Select File", string extension = "")
        {
            this._path = path;
            this.title = title;
            this.folder = folder;
            this.extension = extension;
        }

        public void SetPath(string path)
        {
            this._path = path;
        }
        public bool legal { get { return Fitter(path); } }
        protected virtual bool Fitter(string path) { return true; }

        public void OnGUI(Rect position)
        {
            var rects = EditorTools.RectEx.VerticalSplit(position, position.width - 30);
            bool last = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.TextField(rects[0], path);
            GUI.enabled = last;
            Event e = Event.current;
            if (rects[0].Contains(e.mousePosition))
            {
                var info = EditorTools.DragAndDropTool.Drag(e, rects[0]);
                if (info.enterArera && info.compelete && info.paths.Length == 1 && File.Exists(info.paths[0]))
                {
                    _path = info.paths[0];
                    onValueChange?.Invoke(_path);
                }
            }
            if (GUI.Button(rects[1], EditorGUIUtility.IconContent("Folder Icon")))
            {
                string tmp = EditorUtility.OpenFilePanel(title, folder, extension);
                if (!string.IsNullOrEmpty(tmp) && Directory.Exists(tmp))
                {
                    _path = tmp.ToAssetsPath();
                    onValueChange?.Invoke(_path);
                }
            }
            if (Fitter(path))
            {
                EditorTools.RectEx.DrawOutLine(rects[0], Color.grey);
            }
            else
            {
                EditorTools.RectEx.DrawOutLine(rects[0], Color.red);
            }
        }

    }

}
