using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class OnlineTransform : OnlineBehavior
{
    [Sync]
    Vector3 pos = new Vector3();
    [Sync]
    Quaternion rot = new Quaternion();
    [Sync]
    private float timecode = 0;


   
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
    private float totalTimeCumulative = 0;
    public float SyncDeltaMax = 0.050f;
    public float JitterPercent = 0.2f;
    private float SyncDelta = 0.000f;
    [SerializeField] float  m_LerpDelay = 0.1f;

    enum Smoothing { NoInterpolation, Lerp, };
    [SerializeField] Smoothing m_smoothing = Smoothing.Lerp;


    void Update()
    {
        deltaTimeCumulative += Time.unscaledDeltaTime;
        totalTimeCumulative += Time.unscaledDeltaTime;

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
                        if(SyncDeltaMax > m_LerpDelay)
                        {
                            m_LerpDelay = SyncDeltaMax;
                            OnlineManager.Log("Warning : Lerp delay < sync frequency, will result in glitches");
                        }
                        LinearInterpolation();
                        
                        break;
                    }
            }
        }
    }

    public override bool NeedSync()
    {
        if (deltaTimeCumulative < SyncDelta)
            return false;
        pos = transform.position;
        rot = transform.rotation;
        timecode = totalTimeCumulative;
        return true;
    }
    protected override void OnSync()
    {
        SyncDelta = SyncDeltaMax + Random.Range(-1f, 1f) * SyncDeltaMax * JitterPercent;
        deltaTimeCumulative = 0;
    }

    protected override void OnSynced()
    {
        LerpTransform recvTransform = new LerpTransform();
        recvTransform.position = pos;
        recvTransform.rotation = rot;
        recvTransform.timecode = timecode;
        m_transforms.Add(recvTransform);
    }

    struct LerpTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timecode;
    }
    private List<LerpTransform> m_transforms = new List<LerpTransform>();

    private void LinearInterpolation()
    {
        //every time vars are relative to 0 : start of this component
        //Debug.Log(m_transforms.Count);
        //wait at least lerp delay
        float pastTime = totalTimeCumulative - m_LerpDelay;
        if (m_transforms.Count > 0 && m_transforms[0].timecode > pastTime)
            return;
        //ok so pasttime is above our first timecode: we can start to interpolate
        //but only if we have enough data
        float ratio = 0;
        while (m_transforms.Count > 1)
        {
            float delta = pastTime - m_transforms[0].timecode;
            float total = m_transforms[1].timecode - m_transforms[0].timecode;
            ratio = delta / total;
            if (ratio < 1) 
                break;
            //we finished our current step, switch to next one
            m_transforms.RemoveAt(0);
        }
        //check if we still have enough data
        if (m_transforms.Count <= 1)
            return;
        //Debug.Log(ratio);
        //lerping between 2 
        transform.position = Vector3.Lerp(m_transforms[0].position, m_transforms[1].position, ratio);
        transform.rotation = Quaternion.Lerp(m_transforms[0].rotation, m_transforms[1].rotation, ratio);
    }
}

