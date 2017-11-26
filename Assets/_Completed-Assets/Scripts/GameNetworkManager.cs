using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class GameNetworkManager : NetworkManager {

    /// <PROTOCOL>
    const short TRANSFORM_MSG = 0x1000;
    //note : we should register exact same messages for every clients since we want to simulate a p2p
    void RegisterProtocol(NetworkConnection _conn)
    {
       _conn.RegisterHandler(TRANSFORM_MSG, OnTransformMessage);
    }
    /// </PROTOCOL>

    /// <P2P>

    private bool m_isHost = false;
    //Send message to all : abstract client/server distinction from basic UNet module
    public void SendToAll(short _msgType, MessageBase _msg)
    {
        if (!IsClientConnected())
            return;
        if(m_isHost)
        {
            NetworkServer.SendToAll(_msgType, _msg);
        }
        else
        {
            client.Send(_msgType, _msg);
        }
    }
    /// </P2P>

    static public  new GameNetworkManager singleton;
    void Awake()
    {
        //Check if singleton already exists
        if (singleton == null)
        {
            //if not, set singleton to this
            singleton = this;
        }
        //If singleton already exists and it's not this:
        else if (singleton != this)
        {
            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one singleton of a GameManager.
            Destroy(gameObject);
        }

        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);
        //for each client we register handler server to this client connection
        RegisterProtocol(conn);
        m_isHost = true;
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        //register handlers for client to server connection
        //except for host to avoid callback on our own message
        if (!m_isHost)
        {
            RegisterProtocol(conn);
        }
    }


    /// <GAMEPLAY>

    //return true if character linked to its id is handle locally
    public bool IsLocalPlayer(int _CharacterID)
    {
        //since we have 2 players only here, use simple trick :
        // 1 is host, 2 is client
        if (_CharacterID == 1 && m_isHost)
            return true;
        if (_CharacterID == 2 && !m_isHost)
            return true;
        return false;

    }

		public TankReplica replicaTank;

    //shortcut : NetworkTransform IS a message base, should not
    public class NetworkTransform : MessageBase
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public void SendTransform(Vector3 _pos, Quaternion _rot)
    {
        NetworkTransform transform = new NetworkTransform();
        transform.position = _pos;
        transform.rotation = _rot;
        SendToAll(TRANSFORM_MSG, transform);
    }

    void OnTransformMessage(NetworkMessage msg)
    {
        NetworkTransform netTransform = msg.ReadMessage<NetworkTransform>();
				replicaTank.SetReplicaTransform(netTransform.position, netTransform.rotation);
    }

		public float TickInS = 0.1f;
		float currentTime = 0.1f;
		void Update()
		{
			currentTime += Time.deltaTime;
			if(currentTime > TickInS)
			{
				currentTime = 0.0f;
				SendTransform(replicaTank.transform.position, replicaTank.transform.rotation);
			}

		}
    /// </GAMEPLAY>


}
