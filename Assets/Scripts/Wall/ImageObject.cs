using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageObject : MonoBehaviour {

	public Sprite Sprite;

	private RectTransform _rectTransform;
	public RectTransform RectTransform{
		get{ 
			if(_rectTransform == null)
				_rectTransform = GetComponent<RectTransform> ();
			return _rectTransform; 
		}
	}

	public void SetImage(Sprite sprite){
		this.Sprite = sprite;
		var image = GetComponent<Image> ();
		image.sprite = sprite;
	}

}
