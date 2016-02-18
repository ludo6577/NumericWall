using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;

// http://www.robotmonkeybrain.com/good-enough-guide-to-unitys-unet-transport-layer-llapi/
public class Client : MonoBehaviour {

	public static int Port = 8889;
	public string ServerIP = "127.0.0.1";

	private int hostId;
	private int connectionId;
	private int myReliableChannelId;

	// Use this for initialization
	void Start () {
		ConnectClient ();
	}
	
	// Update is called once per frame
	void ConnectClient () {
		byte error;

		NetworkTransport.Init();
		ConnectionConfig config = new ConnectionConfig();
		myReliableChannelId = config.AddChannel(QosType.Reliable);
		HostTopology topology = new HostTopology(config, 1);
		hostId = NetworkTransport.AddHost(topology, Port);
		connectionId = NetworkTransport.Connect(hostId, ServerIP, Server.Port, 0, out error);

		if (error != (byte)NetworkError.Ok) {
			Debug.LogError("Client: error: " + (NetworkError)error); //not good
			return;
		}
		Debug.Log ("Client: connection succed");

		SendSocketMessage ();
	}


	public void SendSocketMessage() {
		byte error;
		byte[] buffer = new byte[1024];
		Stream stream = new MemoryStream(buffer);
		BinaryFormatter formatter = new BinaryFormatter();
		formatter.Serialize(stream, "HelloServer");

		int bufferSize = 1024;

		NetworkTransport.Send(hostId, connectionId, myReliableChannelId, buffer, bufferSize, out error);

		if (error != (byte)NetworkError.Ok) {
			Debug.LogError("Client: error: " + (NetworkError)error); //not good
			return;
		}
		Debug.Log ("Client: connection succed");
	}
}
