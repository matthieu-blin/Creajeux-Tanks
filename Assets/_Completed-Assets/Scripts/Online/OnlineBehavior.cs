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




/// <summary>
/// Inherit this instead of MonoBehavior to use automatic replication features to your script
/// IMPORTANT, You must call Init() function at the end of you Start function
/// 
/// Field can be automatically replicated using [Sync] attribute in script, Or setting them up in editor inspector
/// WARNING : types of function parameters and fields that could be replicated are limited to specific objet type
///         If you want to add your own, Check Init function to see how to register a writer and reader
///TODO : use static dictionnary for this and static method
///
/// Object are only synced when Needed : By default everyframe, but you can override NeedSync method for your purpose
/// </summary>
[RequireComponent(typeof(OnlineIdentity))]
public class OnlineBehavior : MonoBehaviour
{
    [HideInInspector] public List<string> m_serializedFields;
    private FieldInfo[] m_syncedFields;
    private int m_index = 0;
    public int Index { get => m_index;  }
    private OnlineIdentity m_identity = null;
    public ulong Uid { get => m_identity.m_uid; }

    public delegate object ObjectReader(BinaryReader _r);
    public delegate void ObjectWriter(object _o, BinaryWriter _r);
    private Dictionary<Type, ObjectReader> m_ObjectReaders = new Dictionary<Type, ObjectReader>();
    private Dictionary<Type, ObjectWriter> m_ObjectWriter = new Dictionary<Type, ObjectWriter>();

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

        //find OnlineIdentity Component
        m_ObjectReaders.Add(typeof(Vector3), ReadVector3);
        m_ObjectWriter.Add(typeof(Vector3), WriteVector3);
        m_ObjectReaders.Add(typeof(Quaternion), ReadQuaternion);
        m_ObjectWriter.Add(typeof(Quaternion), WriteQuaternion);
        m_ObjectReaders.Add(typeof(int), ReadInt);
        m_ObjectWriter.Add(typeof(int), WriteInt);
        m_ObjectReaders.Add(typeof(float), ReadSingle);
        m_ObjectWriter.Add(typeof(float), WriteSingle);

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
    //event called just after sending sync data during online loop
    protected virtual void OnSync() { }
    //event called just after receiving sync data during online loop
    protected virtual void OnSynced() { }

    public virtual  void Write(BinaryWriter w)
    {
        foreach (var field in m_syncedFields)
        {
            Type type = field.FieldType;
            ObjectWriter ow;
            if (m_ObjectWriter.TryGetValue(type, out ow))
            {
                ow(field.GetValue(this), w);
            }
            else
            {
                OnlineManager.Log("No Writer for this type " + type.Name);
            }
        }
        OnSync();
    }

    public virtual  void Read(BinaryReader r)
    {
        foreach (var field in m_syncedFields)
        {
            Type type = field.FieldType;
            ObjectReader or;
            if (m_ObjectReaders.TryGetValue(type, out or))
            {
                field.SetValue(this, or(r));
            }
            else
            {
                OnlineManager.LogError("No Reader for this type " + type.Name);
            }

        }
        m_justSynced = true;
        OnSynced();
    }

    private void WriteVector3(object _obj, BinaryWriter _w)
    {
        var v = (Vector3)_obj;
        _w.Write(v.x);
        _w.Write(v.y);
        _w.Write(v.z);
    }
    private object ReadVector3(BinaryReader _r)
    {
        var v = new Vector3();
        v.x = _r.ReadSingle();
        v.y = _r.ReadSingle();
        v.z = _r.ReadSingle();
        return v;
    }
    private void WriteQuaternion(object _obj, BinaryWriter _w)
    {
        var q = (Quaternion)_obj;
        _w.Write(q.x);
        _w.Write(q.y);
        _w.Write(q.z);
        _w.Write(q.w);
    }
    private object ReadQuaternion(BinaryReader _r)
    {
        var q = new Quaternion();
        q.x = _r.ReadSingle();
        q.y = _r.ReadSingle();
        q.z = _r.ReadSingle();
        q.w = _r.ReadSingle();
        return q;
    }
    private void WriteInt(object _obj, BinaryWriter _w)
    {
        var t = (int)_obj;
        _w.Write(t);
    }

    private object ReadInt(BinaryReader _r)
    {
        int i = _r.ReadInt32();
        return i;
    }
    private void WriteSingle(object _obj, BinaryWriter _w)
    {
        var t = (float)_obj;
        _w.Write(t);
    }

    private object ReadSingle(BinaryReader _r)
    {
        float i = _r.ReadSingle();
        return i;
    }


}

