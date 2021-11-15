﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Reflection;


#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(OnlineBehavior), true)]
public class OnlineBehaviorEditor : Editor
{
    int selectedField = 0;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        OnlineBehavior ob = target as OnlineBehavior;
        var syncedFieldsByScript = target.GetType().GetFields(BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.FlattenHierarchy
            | BindingFlags.Instance
            | BindingFlags.Static);
        GUILayout.BeginHorizontal("fieldPopup");
        string[] fieldNames = syncedFieldsByScript.Select(f => f.Name).ToArray();
        selectedField = EditorGUILayout.Popup("Fields", selectedField, fieldNames);
        if (GUILayout.Button("+", GUILayout.Width(20)))
        {
            ob.m_serializedFields.Add(fieldNames[selectedField]);
            EditorUtility.SetDirty(ob);
        }
        GUILayout.EndHorizontal();
    }
}
#endif
[RequireComponent(typeof(OnlineIdentity))]
public  class OnlineBehavior : MonoBehaviour
{
    public List<string> m_serializedFields;
    private FieldInfo[] m_syncedFields;

    public void Init()
    {
        m_syncedFields = GetType().GetFields(BindingFlags.NonPublic
           | BindingFlags.Public
           | BindingFlags.FlattenHierarchy
           | BindingFlags.Instance
           | BindingFlags.Static)
           .Where(field => m_serializedFields.Contains(field.Name) ).ToArray();

        OnlineObjectManager.Instance.RegisterOnlineBehavior(this);
    }

    public void OnDestroy()
    {
        OnlineObjectManager.Instance.UnregisterOnlineBehavior(this);
    }
    public bool HasAuthority()
    {
        return GetComponent<OnlineIdentity>().HasAuthority();
    }
    public bool NeedUpdateFields()
    {
        return HasAuthority() && NeedSync();
    }
    public virtual bool NeedSync() { return true; }
    public virtual  void Write(BinaryWriter w)
    {
        foreach (var field in m_syncedFields)
        {
            Type type = field.FieldType;
            if(type == typeof(float))
            {
                w.Write((float)field.GetValue(this));
            }
        }
    }

    public virtual  void Read(BinaryReader r)
    {
        foreach (var field in m_syncedFields)
        {
            Type type = field.FieldType;
            if (type == typeof(float))
            {
                float f = r.ReadSingle();
                field.SetValue(this, f);
            }
        }
    }
  }
