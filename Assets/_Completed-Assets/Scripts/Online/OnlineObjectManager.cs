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

    private uint m_IDGenerator = 0;
    public GameObject[] m_DynamicObject = new GameObject[0];
    private List<GameObject> m_DynamicObjectInstances = new List<GameObject>();
    private List<GameObject> m_StaticObject = new List<GameObject>();
    private List<OnlineBehavior> m_OnlineBehaviors = new List<OnlineBehavior>();

    public void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }
    void Start()
    {
        OnlineManager.Instance.RegisterHandler((byte)OnlineProtocol.Handler.ONLINE_OBJECT, RecvOnlineObject);
        OnlineManager.Instance.RegisterHandler((byte)OnlineProtocol.Handler.ONLINE_OBJECT_DESTROY, RecvOnlineObjectDestroy);
        OnlineManager.Instance.RegisterHandler((byte)OnlineProtocol.Handler.ONLINE_OBJECT_FIELDS, RecvOnlineBehaviorFieldsUpdate);
        OnlineManager.Instance.RegisterHandler((byte)OnlineProtocol.Handler.ONLINE_OBJECT_METHODS, RecvOnlineBehaviorMethodsUpdate);
    }

    internal int RegisterOnlineBehavior(OnlineBehavior onlineBehavior)
    {
        var duplicate = m_onlineBehaviors.FindAll(ob => ob.Uid == onlineBehavior.Uid && ob.gameObject != onlineBehavior.gameObject);
        if (duplicate.Count > 0)
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

    public GameObject Instantiate(string _name, Vector3? _pos = null, Quaternion? _rot = null, uint _playerID = 0)
    {
        GameObject obj = Array.Find(m_DynamicObject, go => go.name == _name);
        return Instantiate(obj, _pos, _rot, _playerID);
    }
    public GameObject Instantiate(GameObject _prefab, Vector3? _pos = null, Quaternion? _rot = null, uint _playerID = 0)
    {
        if (_prefab == null ||  !OnlineManager.Instance.IsHost())
            return null;
        GameObject obj = Array.Find(m_DynamicObject, go => go.name == _prefab.name);
        if (obj.GetComponent<OnlineIdentity>() == null)
        {
            OnlineManager.LogError("No Online Identity on object " + _prefab.name);
            return null;
        }
        if (_pos == null)
            _pos = Vector3.zero;
        if (_rot == null)
            _rot = Quaternion.identity;
        GameObject newObj = GameObject.Instantiate(obj, _pos.Value, _rot.Value);
        var onlineID = newObj.GetComponent<OnlineIdentity>();
        onlineID.m_srcName = _prefab.name;
        onlineID.m_type = OnlineIdentity.Type.Dynamic;
        onlineID.m_localPlayerAuthority = _playerID;
        return newObj;
    }

    //Spawn will be automatically called on gameObject with OnlineId set to Static
    //Object should have been created using OnlineObject.Instantiate
    //do not call this except if you know exactly what you're doing
    public void Spawn(GameObject _obj)
    {
        if (!OnlineManager.Instance.IsHost())
            return;
        m_IDGenerator++;
        _obj.GetComponent<OnlineIdentity>().m_uid = m_IDGenerator;
        SendOnlineObject(_obj);
    }

    //this is handled by OnlineIdentity
    //Despawn will be automatically called on gameObject with OnlineId destruction
    //do not call this except if you know exactly what you're doing
    public void Despawn(GameObject _obj)
    {
        if (!OnlineManager.Instance.IsHost())
            return;
        SendOnlineObjectDestroy(_obj);
    }
    //this is handled by OnlineIdentity
    //do not call this except if you know exactly what you're doing
    public void RegisterStaticObject(GameObject _obj)
    {
        m_IDGenerator++;
        _obj.GetComponent<OnlineIdentity>().m_uid = m_IDGenerator;
        m_StaticObject.Add(_obj);
    }
    private void RecvOnlineObject(byte[] _msg)
    {
        using (MemoryStream m = new MemoryStream(_msg))
        {
            using (BinaryReader r = new BinaryReader(m))
            {
                byte type = r.ReadByte();
                string name = r.ReadString();
                ulong uid = r.ReadUInt64();
                uint playerID = r.ReadUInt32();

                switch ((OnlineIdentity.Type)type)
                {
                    case OnlineIdentity.Type.Static:
                        {
                            //search for current parent path
                            GameObject obj = m_StaticObject.Find(go => go.name == name);
                            obj.GetComponent<OnlineIdentity>().m_uid = uid;
                            obj.GetComponent<OnlineIdentity>().m_localPlayerAuthority = playerID;
                            obj.SetActive(true);
                            break;
                        }
                    case OnlineIdentity.Type.Dynamic:
                        {
                            Vector3 position = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                            Quaternion rotation = new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                            GameObject obj = Array.Find(m_DynamicObject, go => go.name == name);
                            GameObject newObj = GameObject.Instantiate(obj, position, rotation);
                            newObj.GetComponent<OnlineIdentity>().m_srcName = name;
                            newObj.GetComponent<OnlineIdentity>().m_uid = uid;
                            newObj.GetComponent<OnlineIdentity>().m_localPlayerAuthority = playerID;
                            newObj.GetComponent<OnlineIdentity>().m_type = (OnlineIdentity.Type)type;
                            m_DynamicObjectInstances.Add(newObj);
                            break;
                        }

                }
            }
        }
    }
    private void SendOnlineObject(GameObject _obj)
    {
        using (MemoryStream m = new MemoryStream())
        {
            using (BinaryWriter w = new BinaryWriter(m))
            {
                w.Write((byte)_obj.GetComponent<OnlineIdentity>().m_type);
                w.Write(_obj.GetComponent<OnlineIdentity>().m_srcName);
                w.Write(_obj.GetComponent<OnlineIdentity>().m_uid);
                w.Write(_obj.GetComponent<OnlineIdentity>().m_localPlayerAuthority);
                if (_obj.GetComponent<OnlineIdentity>().m_type == OnlineIdentity.Type.Dynamic)
                {
                    w.Write(_obj.transform.position.x);
                    w.Write(_obj.transform.position.y);
                    w.Write(_obj.transform.position.z);
                    w.Write(_obj.transform.rotation.x);
                    w.Write(_obj.transform.rotation.y);
                    w.Write(_obj.transform.rotation.z);
                    w.Write(_obj.transform.rotation.w);
                }
                OnlineManager.Instance.SendMessage((byte)OnlineProtocol.Handler.ONLINE_OBJECT, m.GetBuffer());
            }
        }
    }

    private void RecvOnlineObjectDestroy(byte[] _msg)
    {
        using (MemoryStream m = new MemoryStream(_msg))
        {
            using (BinaryReader r = new BinaryReader(m))
            {
                byte type = r.ReadByte();
                ulong uid = r.ReadUInt64();

                switch ((OnlineIdentity.Type)type)
                {
                    case OnlineIdentity.Type.Static:
                        {
                            break;
                        }
                    case OnlineIdentity.Type.Dynamic:
                        {
                            GameObject obj = m_DynamicObjectInstances.Find(go => go.GetComponent<OnlineIdentity>().m_uid == uid);
                            m_DynamicObjectInstances.Remove(obj);
                            Destroy(obj);
                            break;
                        }

                }
            }
        }
    }
    private void SendOnlineObjectDestroy(GameObject _obj)
    {
        using (MemoryStream m = new MemoryStream())
        {
            using (BinaryWriter w = new BinaryWriter(m))
            {
                w.Write((byte)_obj.GetComponent<OnlineIdentity>().m_type);
                w.Write(_obj.GetComponent<OnlineIdentity>().m_uid);
                OnlineManager.Instance.SendMessage((byte)OnlineProtocol.Handler.ONLINE_OBJECT_DESTROY, m.GetBuffer());
            }
        }
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

//static shortcut to OnlineObjectManager method
public class OnlineObject
{
    public static GameObject Instantiate(string _name, Vector3? _pos = null, Quaternion? _rot = null, uint _playerID = 0)
    {
        return OnlineObjectManager.Instance.Instantiate(_name, _pos, _rot, _playerID);
    }

    public static GameObject Instantiate(GameObject _prefab, Vector3? _pos = null, Quaternion? _rot = null, uint _playerID = 0)
    {
        return OnlineObjectManager.Instance.Instantiate(_prefab, _pos, _rot, _playerID);
    }
    /// <summary>
    /// Call this function Only on a Dynamic online object, after a successfull OnlineObject.Instantiate
    /// </summary>
    public static void Spawn(GameObject _obj) { OnlineObjectManager.Instance.Spawn(_obj); }
    //very stupid determinist id for now
    //TODO : should check other ID to avoid collision
    public static ulong ComputeDeterministID(uint _id)
    {
        return (1 << 32) + _id;
    }


}
