using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageObject : MonoBehaviour {

	public void SetImage(Sprite sprite){
		var image = GetComponent<Image> ();
		image.sprite = sprite;
	}

}
