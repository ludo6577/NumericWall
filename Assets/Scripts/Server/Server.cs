using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Collections;

// Official: http://docs.unity3d.com/Manual/UNetUsingTransport.html
// Cool:     http://www.robotmonkeybrain.com/good-enough-guide-to-unitys-unet-transport-layer-llapi/
public class Server : MonoBehaviour {

	public static int Port = 8888;
	public static ushort MaxConnections = 10;
	public static ushort MaxPacketSize = 500;

	public static bool Connected = false;

	void Start () {
		InitServer ();
	}


	public void InitServer(){
		GlobalConfig gConfig = new GlobalConfig();
		gConfig.MaxPacketSize = MaxPacketSize;
		NetworkTransport.Init();

		ConnectionConfig config = new ConnectionConfig();
		int myReiliableChannelId  = config.AddChannel(QosType.Reliable);
		//int myUnreliableChannelId = config.AddChannel(QosType.Unreliable);

		HostTopology topology = new HostTopology(config, MaxConnections);
		int hostId = NetworkTransport.AddHost(topology, Port);
		Debug.Log ("Server started (hostId: " + hostId + ")");
	}


	void Update()
	{
		int recHostId; 
		int connectionId; 
		int channelId; 
		byte[] recBuffer = new byte[1024]; 
		int bufferSize = 1024;
		int dataSize;
		byte error;
		NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
		switch (recData) {
			case NetworkEventType.Nothing:
				break;
			case NetworkEventType.ConnectEvent:
				Debug.Log("Server: incoming connection event received");
				break;
			case NetworkEventType.DataEvent:
				Stream stream = new MemoryStream(recBuffer);
				BinaryFormatter formatter = new BinaryFormatter();
				string message = formatter.Deserialize(stream) as string;
				Debug.Log("Server: incoming message event received: " + message);
				break;
			case NetworkEventType.DisconnectEvent:
				Debug.Log("Server: remote client event disconnected");
				break;
		}
	}





	// http://answers.unity3d.com/questions/190340/how-can-i-send-a-render-texture-over-the-network.html
	public static Color[] GetRenderTexturePixels(RenderTexture tex)
	{
		RenderTexture.active = tex;
		Texture2D tempTex = new Texture2D(tex.width, tex.height);
		tempTex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
		tempTex.Apply();
		return tempTex.GetPixels();
	}


	byte[] SerializeObject<T>(T objectToSerialize)
	{
		BinaryFormatter bf = new BinaryFormatter();
		MemoryStream memStr = new MemoryStream();


		bf.Serialize(memStr, objectToSerialize);

		memStr.Position = 0;


		//return "";
		return memStr.ToArray();
	}

	T DeserializeObject<T>(byte[] dataStream)
	{
		MemoryStream stream = new MemoryStream(dataStream);
		stream.Position = 0;
		BinaryFormatter bf = new BinaryFormatter();
		bf.Binder = new VersionFixer();
		T retV = (T)bf.Deserialize(stream);
		return retV;
	}

	sealed class VersionFixer : SerializationBinder 
	{
		public override Type BindToType(string assemblyName, string typeName) 
		{
			Type typeToDeserialize = null;


			// For each assemblyName/typeName that you want to deserialize to
			// a different type, set typeToDeserialize to the desired type.
			String assemVer1 = Assembly.GetExecutingAssembly().FullName;
			if (assemblyName != assemVer1) 
			{
				// To use a type from a different assembly version, 
				// change the version number.
				// To do this, uncomment the following line of code.
				assemblyName = assemVer1;
				// To use a different type from the same assembly, 
				// change the type name.
			}
			// The following line of code returns the type.
			typeToDeserialize = Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
			return typeToDeserialize;
		}

	}
}
