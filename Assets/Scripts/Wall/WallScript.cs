using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TouchScript;
using TouchScript.Utils;
using System.Collections;
using System.Collections.Generic;
using System.IO;


public class WallScript : MonoBehaviour {
	
	public DraggableImageObject ObjectImagePrefab;
	public DraggableVideoObject ObjectVideoPrefab;

	public GridLayout Grid;

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

	private static string ImagesPath = "Pictures";
	private static string VideosPath = "Videos";
	private List<DraggableObject> draggableObjects;

	void Start () {
		draggableObjects = new List<DraggableObject> ();

		Sprite[] sprites = Resources.LoadAll<Sprite> (ImagesPath);
		foreach (var sprite in sprites) {
			if (draggableObjects.Count >= MaxObject/2)
				break;

			var obj = CreateNewObject (ObjectImagePrefab, "Image");
			((DraggableImageObject) obj).SetImage (sprite);
			draggableObjects.Add (obj);
		}

		MovieTexture[] movies = Resources.LoadAll<MovieTexture> (VideosPath);
		foreach (var movie in movies) {
			if (draggableObjects.Count >= MaxObject)
				break;
			
			var obj = CreateNewObject (ObjectVideoPrefab, "Video");
			((DraggableVideoObject)obj).SetVideo (movie);
			draggableObjects.Add (obj);
		}

		this.UpdateLayers ();
		this.UpdateMove (true);
	}

	void Update(){
		this.UpdateMove (false);
	}


	private DraggableObject CreateNewObject(DraggableObject prefab, string name){
		// TODO: ugly, but... its a poc...
		float posX = 0f, posY = 0f;
		while(posX>=0f && posX<=RectTransform.sizeDelta.x && posY>=0f && posY<=RectTransform.sizeDelta.y){
			posX = Random.Range(-InitialRange, RectTransform.sizeDelta.x + InitialRange);
			posY = Random.Range(-InitialRange, RectTransform.sizeDelta.y + InitialRange);
		}

		Vector2 initialPosition = new Vector2 (posX, posY);
		Vector2 center = new Vector2 (RectTransform.sizeDelta.x/2, RectTransform.sizeDelta.y/2);
		Vector2 initialSpeed = center - initialPosition;

		var obj = (DraggableImageObject) Instantiate(prefab, initialPosition, transform.rotation);
		obj.Init (transform, this, name); 
		return obj;
	}


	public void UpdateLayers(){
		foreach (var obj in draggableObjects) {
			var index = obj.transform.GetSiblingIndex ();
			var length = draggableObjects.Count;

			if(length <= 1)
				return;

			var layerIndex = LayersCount - (int)(index*LayersCount) / (length-1);
			layerIndex = (layerIndex == 0 && index != length-1) ? 1 : layerIndex; //Only last element on layer 0
			obj.SetLayer (layerIndex);
		}
	}


	public void UpdateMove(bool updateAll){
		var minGrid = 4;
		var maxGridX = Grid.GridCellX - 4;
		var maxGridY = Grid.GridCellY - 4;

		if (updateAll || Random.Range (0, 100) == 0) {
			if (updateAll) {
				foreach (var obj in draggableObjects) {
					var pos = new Vector2 (Random.Range (minGrid, maxGridX), Random.Range (minGrid, maxGridY));
					obj.SetGridPosition (pos);
				}
			} else {
				var obj = draggableObjects [Random.Range (0, draggableObjects.Count - 1)];
				var pos = new Vector2 (Random.Range (minGrid, maxGridX), Random.Range (minGrid, maxGridY));
				obj.SetGridPosition(pos);
			}
		}
	}
}
