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
    }

    internal void RegisterOnlineBehavior(OnlineBehavior onlineBehavior)
    {
        var sameObject = m_onlineBehaviors.FindAll(ob => ob.gameObject == onlineBehavior.gameObject);
        if (sameObject.Count > 0)
            Debug.LogError("double object");
        m_onlineBehaviors.Add(onlineBehavior);
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
        }
    }
    private void RecvOnlineBehaviorFieldsUpdate(byte[] _msg)
    {
        using (MemoryStream m = new MemoryStream(_msg))
        {
            using (BinaryReader r = new BinaryReader(m))
            {
                ulong uid = r.ReadUInt64();
                var obj = m_onlineBehaviors.Find(ob => ob.GetComponent<OnlineIdentity>().m_uid == uid );
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
                _obj.Write(w);
                OnlineManager.Instance.SendMessage((byte)OnlineProtocol.Handler.ONLINE_OBJECT_FIELDS, m.GetBuffer());
            }
        }
    }

}
