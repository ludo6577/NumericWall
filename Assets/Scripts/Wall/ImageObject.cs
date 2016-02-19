using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageObject : MonoBehaviour {

	public Sprite Sprite;

	public void SetImage(Sprite sprite){
		this.Sprite = sprite;
		var image = GetComponent<Image> ();
		image.sprite = sprite;
	}
}
