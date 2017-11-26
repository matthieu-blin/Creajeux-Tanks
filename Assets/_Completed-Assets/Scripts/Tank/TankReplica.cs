using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankReplica : MonoBehaviour {

	public int PlayerID = 0;

	Vector3 originalPosition;
	Vector3 replicaPosition;
	Quaternion originaRotation;
	Quaternion replicaRotation;
	float timeSinceLastReplica = 0f;
	public void SetReplicaTransform(Vector3 _pos, Quaternion _rot)
	{
		originalPosition = transform.position;
		originaRotation = transform.rotation;
		replicaPosition = _pos;
		replicaRotation = _rot;
		timeSinceLastReplica = 0f;
	}

	// Use this for initialization
	void Start () {
		//initialise replica transform
			SetReplicaTransform(transform.position, transform.rotation);
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
			timeSinceLastReplica += Time.deltaTime;
			float ratio = timeSinceLastReplica / net.TickInS;
			//interpolation based on tick delta Time
			Vector3 position = Vector3.Lerp(originalPosition, replicaPosition, ratio);
			Quaternion rotation = Quaternion.Lerp(originaRotation, replicaRotation, ratio);
			transform.position = position;
			transform.rotation = rotation;
		}
	}
}
