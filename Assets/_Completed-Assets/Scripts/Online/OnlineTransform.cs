using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class OnlineTransform : OnlineBehavior
{
    Vector3 pos = new Vector3();
    Quaternion rot = new Quaternion();

   
     public OnlineTransform()
    {
    }

    // Use this for initialization
    void Start()
    {
        pos = transform.position;
        rot = transform.rotation;
        Init();
    }

    private float deltaTimeCumulative = 0;
    public float SyncDelta = 0.300f;
    void Update()
    {
        deltaTimeCumulative += Time.deltaTime;

        if(!HasAuthority())
        {
            transform.position = pos;
            transform.rotation = rot;
        }
    }

    public override bool NeedSync()
    {
        if (deltaTimeCumulative < SyncDelta)
            return false;
         return true;
    }

    public override void Write(BinaryWriter w)
    {
        w.Write(pos.x);
        w.Write(pos.y);
        w.Write(pos.z);
        w.Write(rot.x);
        w.Write(rot.y);
        w.Write(rot.z);
        w.Write(rot.w);
        pos = transform.position;
        rot = transform.rotation;
        deltaTimeCumulative = 0;
    }

    public override void Read(BinaryReader r)
    {
        pos.x = r.ReadSingle();
        pos.y = r.ReadSingle();
        pos.z = r.ReadSingle();
        rot.x = r.ReadSingle();
        rot.y = r.ReadSingle();
        rot.z = r.ReadSingle();
        rot.w = r.ReadSingle();
    }

}
