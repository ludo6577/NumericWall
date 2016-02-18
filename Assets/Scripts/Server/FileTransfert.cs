using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Collections;

public class FileTransfert : MonoBehaviour {

	/*
	public static byte SendFile(int hostId, int connectionId, int channelId){

		//TODO 
		string fileName = "TODO";

		string base64String;
		using (Image image = Image.FromFile(fileName))
		{
			using (MemoryStream m = new MemoryStream())
			{
				image.Save(m, image.RawFormat);
				byte[] imageBytes = m.ToArray();

				// Convert byte[] to Base64 String
				base64String = Convert.ToBase64String(imageBytes);
			}
		}

		byte[] byData = System.Text.Encoding.ASCII.GetBytes(base64String);
		int bufferSize = byData.Length;

		byte error;
		NetworkTransport.Send(hostId, connectionId, channelId, byData, bufferSize, out error);

		return error;
	}

	public static void GetFile(byte[] buffer){
		string msg;
		using (NetworkStream ns = client.GetStream())
		using (StreamReader sr = new StreamReader(ns))
		{
			msg = sr.ReadToEnd();
		}

		byte[] binaryData = Convert.FromBase64String(msg);
		var image = Image.FromStream(new MemoryStream(binaryData));
	}
	*/
}
