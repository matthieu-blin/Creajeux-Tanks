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
    bool openreflection = true;
    public override void OnInspectorGUI()
    {
        openreflection = EditorGUILayout.BeginFoldoutHeaderGroup(openreflection, "Sync Reflection");

        if (openreflection)
        {
            OnlineBehavior obj = (OnlineBehavior)target;
            var syncedFieldsByScript = target.GetType().GetFields(BindingFlags.NonPublic
                | BindingFlags.Public
                | BindingFlags.FlattenHierarchy
                | BindingFlags.Instance
                | BindingFlags.Static)
                .Where(prop => Attribute.IsDefined(prop, typeof(Sync))).ToArray();

            GUILayout.BeginHorizontal("fieldPopup");
            string[] fieldNames = target.GetType().GetFields(BindingFlags.NonPublic
                | BindingFlags.Public
                | BindingFlags.FlattenHierarchy
                | BindingFlags.Instance
                | BindingFlags.Static).Select(f => f.Name).ToArray();

            selectedField = EditorGUILayout.Popup("Fields", selectedField, fieldNames);
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                obj.m_serializedFields.Add(fieldNames[selectedField]);
                EditorUtility.SetDirty(obj);
            }
            GUILayout.EndHorizontal();
            foreach (var f in syncedFieldsByScript)
            {
                GUILayout.BeginHorizontal("scriptfield");
                EditorGUILayout.LabelField(f.Name);
                EditorGUILayout.LabelField("(script)", GUILayout.Width(40));
                GUILayout.EndHorizontal();
            }
            foreach (var fname in obj.m_serializedFields)
            {
                GUILayout.BeginHorizontal("field");
                EditorGUILayout.LabelField(fname);
                bool b = GUILayout.Button("-", GUILayout.Width(20));
                GUILayout.EndHorizontal();
                if (b)
                {
                    obj.m_serializedFields.Remove(fname);
                    EditorUtility.SetDirty(obj);
                    break;
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        base.OnInspectorGUI();
    }
}
#endif


/// <summary>
/// Synced field will be automatically replicated 
/// ONLY from player who has authority on this object to others.
/// Replication will occurs when Object must be synced (check OnlineBehavior NeedSync)
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class Sync : Attribute { }




[RequireComponent(typeof(OnlineIdentity))]
public class OnlineBehavior : MonoBehaviour
{
    [HideInInspector] public List<string> m_serializedFields;
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

        m_syncedFields = GetType().GetFields(BindingFlags.NonPublic
           | BindingFlags.Public
           | BindingFlags.FlattenHierarchy
           | BindingFlags.Instance
           | BindingFlags.Static)
           .Where(field => Attribute.IsDefined(field, typeof(Sync)) && !m_syncedFields.Contains(field)).ToArray();

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
