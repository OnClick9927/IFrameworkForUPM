﻿/*********************************************************************************
 *Author:         OnClick
 *Version:        0.0.1
 *UnityVersion:   2018.3.1f1
 *Date:           2019-03-18
 *Description:    IFramework
 *History:        2018.11--
*********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IFramework
{
    partial class EditorTools
    {
        static class Prefs
        {
            private static Dictionary<string, string> map = new Dictionary<string, string>();
            private static string StringToPath(string key, bool unique)
            {
                string _key = key.Replace("/", "_");
                if (unique)
                {
                    string ukey = SystemInfo.deviceUniqueIdentifier;
                    if (!string.IsNullOrEmpty(ProjectConfig.UserName)) ukey = ProjectConfig.UserName;
                    string dir = EditorTools.projectMemoryPath.CombinePath(ukey);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    return dir.CombinePath($"Prefs_{_key}.txt").ToAssetsPath();
                }
                return EditorTools.projectMemoryPath.CombinePath($"Prefs_{_key}.txt").ToAssetsPath();
            }
            private static string GetKey(Type type, string key)
            {
                return string.Format("{0}/{1}", type, key);
            }
            private static string GetString(Type type, string key, bool unique)
            {
                var path = StringToPath(GetKey(type, key), unique);
                if (!map.ContainsKey(path))
                {
                    var result = string.Empty;
                    if (File.Exists(path))
                        result = File.ReadAllText(path);
                    map[path] = result;
                }
                return map[path];
            }

            private static void SetString(Type type, string key, string value, bool unique)
            {
                var path = StringToPath(GetKey(type, key), unique);
                if (!map.ContainsKey(path))
                    map[path] = value;
                else
                {
                    if (map[path] == value && File.Exists(path))
                        return;
                    map[path] = value;
                }
                File.WriteAllText(path, value);
                AssetDatabase.Refresh();
            }
            public static void SetObject(Type type, string key, object value, bool unique)
            {
                SetString(type, key, JsonUtility.ToJson(value, true), unique);
            }
            public static V GetObject<V>(Type type, string key, bool unique)
            {
                var str = GetString(type, key, unique);
                return (V)JsonUtility.FromJson(str, type);
            }
            public static object GetObject(Type type, string key, bool unique)
            {
                var str = GetString(type, key, unique);
                return JsonUtility.FromJson(str, type);
            }
        }
    }
}
