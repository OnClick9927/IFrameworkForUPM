﻿/*********************************************************************************
 *Author:         OnClick
 *Version:        0.0.1
 *UnityVersion:   2018.3.1f1
 *Date:           2019-03-18
 *Description:    IFramework
 *History:        2018.11--
*********************************************************************************/
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System;
using System.Linq;

namespace IFramework.UI
{
    public class ScriptCreatorFieldsDrawer
    {
        private class Tree : TreeView
        {
            private GameObject go;
            private ScriptCreator sc;
            public void SetGameObject(ScriptCreator sc)
            {
                this.sc = sc;
                if (this.go != sc.gameObject)
                {
                    this.go = sc.gameObject;
                    this.Reload();
                }
            }
            public Tree(TreeViewState state) : base(state)
            {
                this.showBorder = true;
                this.showAlternatingRowBackgrounds = true;
                this.multiColumnHeader = new MultiColumnHeader(new MultiColumnHeaderState(new MultiColumnHeaderState.Column[]
                {
                        new MultiColumnHeaderState.Column()
                        {
                            headerContent=new GUIContent("Name")
                        },
                        new MultiColumnHeaderState.Column()
                        {
                           headerContent=new GUIContent("Mark")
                        },
                         new MultiColumnHeaderState.Column()
                        {
                           headerContent=new GUIContent("FieldName")
                        },
                })); ;
                Reload();
                this.multiColumnHeader.ResizeToFit();
            }

            protected override TreeViewItem BuildRoot()
            {
                return new TreeViewItem { id = 0, depth = -1 };
            }
            protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
            {
                var rows = GetRows() ?? new List<TreeViewItem>();
                rows.Clear();
                AddChildrenRecursive(this.go, root, rows);
                SetupDepthsFromParentsAndChildren(root);
                return rows;
            }
            static TreeViewItem CreateTreeViewItemForGameObject(GameObject gameObject)
            {
                return new TreeViewItem(gameObject.GetInstanceID(), -1, gameObject.name);
            }
            GameObject GetGameObject(int instanceID)
            {
                return (GameObject)EditorUtility.InstanceIDToObject(instanceID);
            }
            void AddChildrenRecursive(GameObject go, TreeViewItem root, IList<TreeViewItem> rows)
            {
                if (go == null) return;
                if (sc.IsPrefabInstance(go)) return;

                var item = CreateTreeViewItemForGameObject(go);
                item.parent = root;
                if (root.children == null)
                    root.children = new List<TreeViewItem>();
                rows.Add(item);
                root.children.Add(item);
                item.depth = root.depth + 1;
                int childCount = go.transform.childCount;
                bool ok = false;
                for (int i = 0; i < childCount; i++)
                    if (!sc.IsPrefabInstance(go.transform.GetChild(i).gameObject))
                    {
                        ok = true;
                        break;
                    }
                if (ok)
                {

                    if (IsExpanded(item.id))
                        for (int i = 0; i < childCount; i++)
                            AddChildrenRecursive(go.transform.GetChild(i).gameObject, item, rows);
                    else
                        item.children = CreateChildListForCollapsedParent();
                }
            }
            private bool GetActive(GameObject go)
            {
                if (go == null) return true;
                if (!go.gameObject.activeSelf)
                    return false;
                return GetActive(go.transform.parent);
            }
            private bool GetActive(Transform trans)
            {
                if (trans == null) return true;
                if (!trans.gameObject.activeSelf)
                    return false;
                return GetActive(trans.parent);
            }
            protected override void RowGUI(RowGUIArgs args)
            {
                var go = GetGameObject(args.item.id);
                float indet = this.GetContentIndent(args.item);
                var first = EditorTools.RectEx.Zoom(args.GetCellRect(0), TextAnchor.MiddleRight, new Vector2(-indet, 0));
                if (!GetActive(go))
                    GUI.color = Color.gray;

                if (sc.IsPrefabInstance(go))
                    GUI.color *= Color.Lerp(Color.cyan, Color.blue, 0.3f);
                GUI.Label(first, args.label);
                if (go != null)
                {
                    ScriptMark sm = go.GetComponent<ScriptMark>();
                    if (sm != null)
                    {
                        var rect = args.GetCellRect(1);
                        GUI.Label(rect, sm.fieldType);
                        rect = args.GetCellRect(2);
                        GUI.Label(rect, sm.fieldName);
                    }
                }
                GUI.color = Color.white;
            }


            void SaveGo()
            {
                EditorUtility.SetDirty(this.go);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Reload();
            }
            
            protected override bool CanRename(TreeViewItem item)
            {
                var go = GetGameObject(item.id);
                return go.GetComponent<ScriptMark>() != null;

            }
            
            protected override void RenameEnded(RenameEndedArgs args)
            {
                var id = args.itemID;
                var go = GetGameObject(id);
                var sm = go.GetComponent<ScriptMark>();
                sm.fieldName = args.newName;
                SaveGo();
            }
            protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
            {
                return this.multiColumnHeader.GetCellRect(2, rowRect);
            }


            protected override void ContextClicked()
            {
                GenericMenu menu = new GenericMenu();
                Dictionary<Type, int> help = new Dictionary<Type, int>();
                List<GameObject> gameobjects = new List<GameObject>();
                List<ScriptMark> marks = new List<ScriptMark>();
                var s = this.GetSelection();
                for (int i = 0; i < s.Count; i++)
                {
                    var go = GetGameObject(s[i]);
                    if (sc.IsPrefabInstance(go)) continue;
                    gameobjects.Add(go);
                    ScriptMark sm = go.GetComponent<ScriptMark>();
                    if (sm != null) marks.Add(sm);

                    Component[] components = go.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        Type componentType = component.GetType();
                        if (component is ScriptMark) continue;
                        if (help.ContainsKey(componentType)) help[componentType]++;
                        else help[componentType] = 1;
                    }

                }
                List<Type> types = new List<Type>() {
                typeof(GameObject),
                typeof(Transform)
                };
                if (help.Count > 0)
                {
                    int max = help.Values.ToList().Max();
                    foreach (var item in help)
                        if (item.Value == max)
                            types.Add(item.Key);
                }

                if (types.Count == 0)
                    menu.AddDisabledItem(new GUIContent("Mark Component"));
                else
                    for (int i = 0; i < types.Count; i++)
                    {
                        var type = types[i];
                        menu.AddItem(new GUIContent($"Mark Component/{type.FullName}"), false, () =>
                        {
                            sc.RemoveMarks(marks);
                            for (int i = 0; i < gameobjects.Count; i++)
                            {
                                ScriptMark sm = sc.AddMark(gameobjects[i]);
                                if (sm != null)
                                {
                                    sm.fieldType = type.FullName;
                                    sc.ValidateMarkFieldName(sm);
                                }
                            }
                            SaveGo();
                        });
                        if (i == 1)
                            menu.AddSeparator("Mark Component/");
                    }
                if (marks.Count == 0)
                    menu.AddDisabledItem(new GUIContent("Remove Marks"));
                else
                    menu.AddItem(new GUIContent("Remove Marks"), false, () =>
                    {
                        sc.RemoveMarks(marks);
                        SaveGo();
                    });
                menu.AddItem(new GUIContent("Destory All Marks"), false, () =>
                  {
                      sc.DestoryMarks();
                      SaveGo();
                  });
                menu.AddSeparator("");
                if (go == null)
                    menu.AddDisabledItem(new GUIContent("Fresh FieldNames"));
                else
                    menu.AddItem(new GUIContent("Fresh FieldNames"), false, () =>
                    {
                        var all = sc.GetMarks();
                        var goes = all.ConvertAll(m => { return (m.gameObject, m.fieldType); });
                        sc.RemoveMarks(all);
                        foreach (var item in goes)
                        {
                            ScriptMark sm = sc.AddMark(item.gameObject);
                            if (sm != null)
                            {
                                sm.fieldType = item.fieldType;
                                sc.ValidateMarkFieldName(sm);
                            }
                        }
                        SaveGo();
                    });

                menu.AddItem(new GUIContent("Check FiledNames"), false, () =>
                {
                    string same;
                    if (sc.HandleSameFieldName(out same))
                    {
                        same = "same FieldName\n" + same;
                        same += "\n err repair ok ";
                        EditorWindow.focusedWindow.ShowNotification(new GUIContent(same));
                        SaveGo();
                    }
                    else
                    {
                        same = "perfect!";
                    }
                    EditorWindow.focusedWindow.ShowNotification(new GUIContent(same));
                });

                menu.ShowAsContext();
            }
        }
        private ScriptCreator _creater;
        private Tree _tree;
        public ScriptCreatorFieldsDrawer(ScriptCreator creater, TreeViewState state)
        {
            this._creater = creater;
            if (state == null)
            {
                state = new TreeViewState();
            }
            _tree = new Tree(state);
        }


        public void OnGUI()
        {
            _tree.SetGameObject(_creater);
            _tree.OnGUI(EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true)));
        }
    }

}
