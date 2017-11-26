using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankReplica : MonoBehaviour {

	public int PlayerID = 0;

	public void SetReplicaTransform(Vector3 _pos, Quaternion _rot)
	{
		transform.position = _pos;
		transform.rotation = _rot;
	}

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

		var net = GameNetworkManager.singleton;
		if(!net)
			return;

		if(net.IsLocalPlayer(PlayerID))
			net.SendTransform(transform.position, transform.rotation);
		else
		{
			net.replicaTank = this;
		}
	}
}
