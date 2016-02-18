using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TouchScript;
using TouchScript.Utils;
using System.Collections;
using System.Collections.Generic;
using System.IO;


public class WallScript : MonoBehaviour {
	
	public DraggableObject ObjectPrefab;
	public int MaxObject;
	[Range(1, 7)]
	public int LayersCount;
	[Range(0, 10)]
	public float InitialVelocity;
	[Range(50, 1000)]
	public int InitialRange;

	private RectTransform _rectTransform;
	public RectTransform RectTransform{
		get{ 
			if(_rectTransform == null)
				_rectTransform = GetComponent<RectTransform> ();
			return _rectTransform; 
		}
	}

	private static string MediaPath = "Pictures";
	private List<DraggableObject> draggableObjects;
	private Dictionary<int, DraggableObject> objectByTouchId = new Dictionary<int, DraggableObject>(10);

	void Start () {
		draggableObjects = new List<DraggableObject> ();
		Sprite[] sprites = Resources.LoadAll<Sprite> (MediaPath);

		foreach (var sprite in sprites) {
			if (draggableObjects.Count >= MaxObject)
				break;

			// TODO: ugly, but... its a poc...
			float posX = 0f, posY = 0f;
			while(posX>=0f && posX<=RectTransform.sizeDelta.x && posY>=0f && posY<=RectTransform.sizeDelta.y){
				posX = Random.Range(-InitialRange, RectTransform.sizeDelta.x + InitialRange);
				posY = Random.Range(-InitialRange, RectTransform.sizeDelta.y + InitialRange);
			}

			Vector2 initialPosition = new Vector2 (posX, posY);
			Vector2 center = new Vector2 (RectTransform.sizeDelta.x/2, RectTransform.sizeDelta.y/2);
			Vector2 initialSpeed = center - initialPosition;

			var obj = (DraggableObject) Instantiate(ObjectPrefab, initialPosition, transform.rotation);
			obj.transform.SetParent(transform, false);
			obj.Move (initialSpeed * (InitialVelocity));
			obj.SetImage (sprite);
			obj.Wall = this;
			obj.Layer = -1;

			draggableObjects.Add (obj);
		}

		this.UpdateLayers ();
	}

	public void UpdateLayers(){
		foreach (var obj in draggableObjects) {
			var index = obj.transform.GetSiblingIndex ();
			var length = draggableObjects.Count;

			if(length <= 1)
				return;

			var layerIndex = LayersCount - (int)(index*LayersCount) / (length-1);
			layerIndex = (layerIndex == 0 && index != length) ? 1 : layerIndex; //Only last element on layer 0

			if (obj.Layer != layerIndex)
				obj.SetLayer (layerIndex);
		}
	}
}
