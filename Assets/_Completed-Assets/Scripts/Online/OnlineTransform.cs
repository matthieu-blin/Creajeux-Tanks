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
        tgtPos = pos;
        tgtRot = rot;
        Init();
    }

    private float deltaTimeCumulative = 0;
    public float SyncDeltaMax = 0.300f;
    public float SyncDelta = 0.300f;
    public float JitterPercent = 0.2f;

    enum Smoothing { NoInterpolation, Lerp, };
    [SerializeField] Smoothing m_smoothing = Smoothing.Lerp;

    Vector3 srcPos = new Vector3();
    Quaternion srcRot = new Quaternion();
    Vector3 tgtPos = new Vector3();
    Quaternion tgtRot = new Quaternion();

    void Update()
    {
        deltaTimeCumulative += Time.deltaTime;

        if(!HasAuthority())
        {
            switch (m_smoothing)
            {
                case Smoothing.NoInterpolation:
                    {
                        transform.position = pos;
                        transform.rotation = rot;
                        break;
                    }
                case Smoothing.Lerp:
                    {
                        if(srcPos != tgtPos || srcRot != tgtRot)
                        { 
                            float ratio = deltaTimeCumulative / SyncDelta;
                            Debug.Log(ratio);
                            ratio = Mathf.Clamp01(ratio);
                            transform.position = Vector3.Lerp(srcPos, tgtPos, ratio);
                            transform.rotation = Quaternion.Lerp(srcRot, tgtRot, ratio);
                        }
                        
                        break;
                    }
            }
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
        w.Write(deltaTimeCumulative);
        SyncDelta = SyncDeltaMax + Random.Range(-1f, 1f) * SyncDeltaMax * JitterPercent;
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
        SyncDelta  = r.ReadSingle();
        srcPos = tgtPos;
        srcRot = tgtRot;
        tgtPos = pos;
        tgtRot = rot;
        deltaTimeCumulative = 0;

    }

}
