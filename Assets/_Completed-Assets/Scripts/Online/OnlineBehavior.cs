using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

[RequireComponent(typeof(OnlineIdentity))]

public abstract class OnlineBehavior : MonoBehaviour
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
