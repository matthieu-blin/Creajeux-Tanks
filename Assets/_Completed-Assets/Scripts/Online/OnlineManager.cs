using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OnlineManager : MonoBehaviour
{
    private static OnlineManager instance = null;
    public static OnlineManager Instance
    {
        get
        {
            return instance;
        }
    }
    
    
    [SerializeField] private String m_Host = "localhost";

    [SerializeField] private int m_port = 25000;

    private SocketAPI m_api;

    
    private bool m_connected = false;

    private bool m_host = false;
    
    
    public delegate void GameMessageCallback(byte[] _msg);

    Dictionary<byte,GameMessageCallback> m_MessageCallbacksHandler;

    public void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }
    // Start is called before the first frame update
    void Start()
    {
        m_api = new SocketAPI();
        m_api.Log = Debug.Log;
        m_api.OnMessage = OnGameMessage; 
        m_MessageCallbacksHandler = new Dictionary<byte, GameMessageCallback>();
    }

    public EndPoint GetLocalEndpoint()
    {
        return m_api.GetLocalEndPoint();
    }
    public void FetchClients(ref List<EndPoint> _clients)
    {
        _clients.Clear();
        m_api.FetchClients(_clients);
    }

    public bool IsConnected()
    {
        return m_api.IsConnected();
    }
    public void Log(string txt)
    {
        m_api.Log(txt);
    }


        public void Host()
    {
        m_api.Host(m_Host, m_port);
        m_connected = true;
        m_host = true;
    }
    
    public bool IsHost()
    {
        return m_host;
    }

    public int GetNumConnectedClient()
    {
        return m_api.GetNumConnectedClient();
    }
    

    public void Join()
    {
        m_api.Join(m_Host, m_port);
        m_connected = true;
    }


    public void RegisterHandler(byte _handlerType, GameMessageCallback _cb)
    {
        GameMessageCallback cb;
        if (m_MessageCallbacksHandler.TryGetValue(_handlerType, out cb))
        {
            m_MessageCallbacksHandler[_handlerType] = cb + _cb;
        }
        else
        {
            m_MessageCallbacksHandler.Add(_handlerType, _cb);
        }
    }
// Update is called once per frame
    void Update()
    {
        if(m_connected)
            m_api.Process();
    }
     public void SendMessage(byte _handlerType, byte[] _msg)
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter w = new BinaryWriter(m))
                {
                    w.Write(_handlerType);
                    w.Write(_msg.Length);
                    w.Write(_msg);
                    m_api.Send(m.ToArray());
                }
            }
            
        }
    
        public int OnGameMessage(byte[] msg )
        {
            using (MemoryStream m = new MemoryStream(msg))
            {
                using(BinaryReader r = new BinaryReader(m))
                {
                    while (r.BaseStream.Position != r.BaseStream.Length)
                    {
                        byte handlerType = r.ReadByte();
                        int size = r.ReadInt32();
                        byte[] buffer = r.ReadBytes(size);
                        GameMessageCallback cb;
                        if (m_MessageCallbacksHandler.TryGetValue(handlerType, out cb))
                        {
                            cb(buffer);
                        }
                        else
                        {
                            m_api.Log("Unhandled Message");
                        }
                    }
                }
            }
            return 0;
        }

    public void LoadScene()
    {
       SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }
}