using System;
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
    private int m_index = 0;
    public int Index { get => m_index;  }
    private OnlineIdentity m_identity = null;
    public ulong Uid { get => m_identity.m_uid; }
    public void Init()
    {
        m_identity = GetComponent<OnlineIdentity>();
        m_syncedFields = GetType().GetFields(BindingFlags.NonPublic
           | BindingFlags.Public
           | BindingFlags.FlattenHierarchy
           | BindingFlags.Instance
           | BindingFlags.Static)
           .Where(field => m_serializedFields.Contains(field.Name) ).ToArray();

        m_index = OnlineObjectManager.Instance.RegisterOnlineBehavior(this);
    }


    private void LateUpdate()
    {
        m_justSynced = false;
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
    bool m_justSynced = false;
    //return true only one frame after receiving a replication
    public bool HasSynced() { return m_justSynced; }
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
        m_justSynced = true;
    }
  }
