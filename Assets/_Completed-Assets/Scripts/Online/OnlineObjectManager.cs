using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OnlineObjectManager : MonoBehaviour
{
    private static OnlineObjectManager instance = null;
    private List<OnlineBehavior> m_onlineBehaviors = new List<OnlineBehavior>();
    public static OnlineObjectManager Instance
    {
        get
        {
            return instance;
        }
    }
    public void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }
    void Start()
    {
        OnlineManager.Instance.RegisterHandler((byte)OnlineProtocol.Handler.ONLINE_OBJECT_FIELDS, RecvOnlineBehaviorFieldsUpdate);
        OnlineManager.Instance.RegisterHandler((byte)OnlineProtocol.Handler.ONLINE_OBJECT_METHODS, RecvOnlineBehaviorMethodsUpdate);
    }

    internal int RegisterOnlineBehavior(OnlineBehavior onlineBehavior)
    {
        var duplicate = m_onlineBehaviors.FindAll(ob => ob.Uid == onlineBehavior.Uid && ob.gameObject != onlineBehavior.gameObject);
        if(duplicate.Count > 0)
        {
            OnlineManager.LogError("Online object already registered with the same Unique Id " + onlineBehavior.Uid +
                " first :" + duplicate[0] + " , second : " + onlineBehavior.name);
            return -1;
        }

        var behaviors = m_onlineBehaviors.FindAll(ob => ob.gameObject == onlineBehavior.gameObject);
        m_onlineBehaviors.Add(onlineBehavior);
        OnlineManager.Log("registering " + onlineBehavior.GetType().FullName + " with index " + behaviors.Count);
        return behaviors.Count;
    }

    internal void UnregisterOnlineBehavior(OnlineBehavior onlineBehavior)
    {
        m_onlineBehaviors.Remove(onlineBehavior);
    }
    // Update is called once per frame
    void Update()
    {
        foreach (var ob in m_onlineBehaviors)
        {
            if(ob.NeedUpdateFields())
                SendOnlineBehaviorFieldsUpdate(ob);
            if (ob.NeedUpdateMethods())
                SendOnlineBehaviorMethodsUpdate(ob);
        }
    }
    private void RecvOnlineBehaviorFieldsUpdate(byte[] _msg)
    {
        using (MemoryStream m = new MemoryStream(_msg))
        {
            using (BinaryReader r = new BinaryReader(m))
            {
                ulong uid = r.ReadUInt64();
                int index = r.ReadInt32();
                var obj = m_onlineBehaviors.Find(ob => ob.GetComponent<OnlineIdentity>().m_uid == uid  && ob.Index == index );
                //note : in case of parallel creation, we could receive msg before instanciation
                //this should be buffered instead
                if(obj != null)
                    obj.Read(r);
            }
        }
    }
    private void SendOnlineBehaviorFieldsUpdate(OnlineBehavior _obj)
    {
        using (MemoryStream m = new MemoryStream())
        {
            using (BinaryWriter w = new BinaryWriter(m))
            {
                w.Write(_obj.GetComponent<OnlineIdentity>().m_uid);
                w.Write(_obj.Index);
                _obj.Write(w);
                OnlineManager.Instance.SendMessage((byte)OnlineProtocol.Handler.ONLINE_OBJECT_FIELDS, m.GetBuffer());
            }
        }
    }


    private void RecvOnlineBehaviorMethodsUpdate(byte[] _msg)
    {
        using (MemoryStream m = new MemoryStream(_msg))
        {
            using (BinaryReader r = new BinaryReader(m))
            {
                ulong uid = r.ReadUInt64();
                int index = r.ReadInt32();
                var obj = m_onlineBehaviors.Find(ob =>  ob.Uid == uid && ob.Index == index);
                //note : in case of parallel creation, we could receive msg before instanciation
                //this should be buffered instead
                if (obj != null)
                {
                    obj.ReadCMDs(r);
                    obj.ReadRPCs(r);
                }
            }
        }
    }
    private void SendOnlineBehaviorMethodsUpdate(OnlineBehavior _obj)
    {
        using (MemoryStream m = new MemoryStream())
        {
            using (BinaryWriter w = new BinaryWriter(m))
            {
                w.Write(_obj.Uid);
                w.Write(_obj.Index);
                _obj.WriteCMDs(w);
                _obj.WriteRPCs(w);
                OnlineManager.Instance.SendMessage((byte)OnlineProtocol.Handler.ONLINE_OBJECT_METHODS, m.GetBuffer());
            }
        }
    }

}
