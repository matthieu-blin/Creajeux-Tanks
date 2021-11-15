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
        var syncedFieldsByScript = target.GetType().GetFields(BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.FlattenHierarchy
            | BindingFlags.Instance
            | BindingFlags.Static);
        string[] fieldNames = syncedFieldsByScript.Select(f => f.Name).ToArray();
        EditorGUILayout.Popup("Fields", selectedField, fieldNames);
    }
}
#endif
[RequireComponent(typeof(OnlineIdentity))]
public  class OnlineBehavior : MonoBehaviour
{
    public void Init()
    {
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
    }

    public virtual  void Read(BinaryReader r)
    {
    }
  }
