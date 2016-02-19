using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TouchScript;
using TouchScript.Gestures;
using TouchScript.Utils;

public class DraggableImageObject : DraggableObject {
	
	public void SetImage(Sprite sprite){
		var imageObject = GetComponentInChildren<ImageObject> ();
		imageObject.SetImage (sprite);

		var collider = GetComponent<BoxCollider2D> ();

		var ratioX = (float) sprite.rect.width / sprite.rect.height;
		var ratioY = (float) sprite.rect.height / sprite.rect.width;
		if (ratioX > 1) {
			var size = new Vector2 (RectTransform.sizeDelta.x * ratioX, RectTransform.sizeDelta.y);
			RectTransform.sizeDelta = size;
			collider.size = size;
		} else {
			var size = new Vector2 (RectTransform.sizeDelta.x, RectTransform.sizeDelta.y * ratioY);
			RectTransform.sizeDelta = size;
			collider.size = size;
		}
	}

}