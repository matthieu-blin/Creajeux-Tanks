using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;


#if UNITY_EDITOR 
using UnityEditor;
[CustomEditor(typeof(OnlineBehavior), true)]
public class OnlineBehaviorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.LabelField(target.GetType().FullName);
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
