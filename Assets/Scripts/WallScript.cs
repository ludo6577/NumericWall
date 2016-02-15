using UnityEngine;
using UnityEngine.UI;
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

	private static string MediaPath = "Pictures";
	private List<DraggableObject> draggableObjects;
	private Dictionary<int, DraggableObject> objectByTouchId = new Dictionary<int, DraggableObject>(10);

	void Start () {
		draggableObjects = new List<DraggableObject> ();
		RectTransform rect = GetComponent<RectTransform> ();
		Sprite[] sprites = Resources.LoadAll<Sprite> (MediaPath);

		foreach (var sprite in sprites) {
			if (draggableObjects.Count >= MaxObject)
				break;

			float posX = 0f;
			float posY = 0f;
			while(posX>=0f && posX<=rect.sizeDelta.x && posY>=0f && posY<=rect.sizeDelta.y){
				posX = Random.Range(-InitialRange, rect.sizeDelta.x + InitialRange);
				posY = Random.Range(-InitialRange, rect.sizeDelta.y + InitialRange);
			}


			Vector2 initialPosition = new Vector2 (posX, posY);

			Vector2 center = new Vector2 (rect.sizeDelta.x/2, rect.sizeDelta.y/2);
			Vector2 initialSpeed = center - initialPosition;

			var obj = (DraggableObject) Instantiate(ObjectPrefab, initialPosition, transform.rotation);
			obj.transform.SetParent(transform, false);
			obj.Move (initialSpeed * (InitialVelocity/100));
			obj.SetImage (sprite);
			obj.Wall = this;

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

			if (obj.Layer != layerIndex)
				obj.SetLayer (layerIndex);
		}
	}



	private void OnEnable()
	{
		if (TouchManager.Instance != null)
		{
			TouchManager.Instance.TouchesBegan += touchesBeganHandler;
			TouchManager.Instance.TouchesEnded += touchesEndedHandler;
			TouchManager.Instance.TouchesMoved += touchesMovedHandler;
			TouchManager.Instance.TouchesCancelled += touchesCancelledHandler;
		}
	}

	private void OnDisable()
	{
		if (TouchManager.Instance != null)
		{
			TouchManager.Instance.TouchesBegan -= touchesBeganHandler;
			TouchManager.Instance.TouchesEnded -= touchesEndedHandler;
			TouchManager.Instance.TouchesMoved -= touchesMovedHandler;
			TouchManager.Instance.TouchesCancelled -= touchesCancelledHandler;
		}
	}


	#region Event handlers

	private void touchesBeganHandler(object sender, TouchEventArgs e)
	{
		var count = e.Touches.Count;
		for (var i = 0; i < count; i++) {
			var touch = e.Touches[i];

			RaycastHit2D hit = Physics2D.Raycast (touch.Position, Vector2.zero);
			if ((hit != null) &&  (hit.collider!=null) && (hit.collider.gameObject!=null)) {
				var gameObject = hit.collider.gameObject.GetComponent<DraggableObject> ();
				if(gameObject!=null)
					objectByTouchId.Add (touch.Id, gameObject);
			}
		}
	}

	private void touchesMovedHandler(object sender, TouchEventArgs e)
	{
		var gameObjects = new List<DraggableObject> ();
		var count = e.Touches.Count;
		for (var i = 0; i < count; i++)
		{
			var touch = e.Touches[i];
			DraggableObject obj = null;
			if (objectByTouchId.TryGetValue(touch.Id, out obj)) {
				gameObjects.Add (obj);
				obj.TouchsPoints.Add (touch);
			}
		}

		foreach (var obj in gameObjects) {
			obj.Compute ();
		}
	}

	private void touchesEndedHandler(object sender, TouchEventArgs e)
	{
		var count = e.Touches.Count;
		for (var i = 0; i < count; i++) {
			var touch = e.Touches[i];
			objectByTouchId.Remove(touch.Id);
		}
	}

	private void touchesCancelledHandler(object sender, TouchEventArgs e)
	{
		touchesEndedHandler(sender, e);
	}

	#endregion


	/*
	 * 			### OLD METHOD ###
	 * 
	void Update () {
		List<Vector2> touchPositions = new List<Vector2> ();

		//Set previous variable depending on the environment	
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_WEBPLAYER
		if(Input.GetMouseButton(0)){
			touchPositions.Add(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
		}
#else
		for(var i=0; i<Input.touchCount; i++){
			touchPositions.Add(Input.GetTouch (i).position);
		}
#endif

		Dictionary<GameObject, List<Vector2>> gameObjectsHitted = new Dictionary<GameObject, List<Vector2>>();

		// For each hitted gameObjects set a list of hit position
		foreach (var touch in touchPositions) {
			RaycastHit2D hit = Physics2D.Raycast (touch, Vector2.zero);
			if ((hit != null) &&  (hit.collider!=null) && (hit.collider.gameObject!=null)) {
				var gameObject = hit.collider.gameObject;
				if (!gameObjectsHitted.ContainsKey (gameObject))
					gameObjectsHitted.Add (gameObject, new List<Vector2> ());
				gameObjectsHitted [gameObject].Add (touch);
			}
		}

		// Calls the Touch method for each touched objects
		foreach (var gameObject in gameObjectsHitted.Keys) {
			var draggableObject = gameObject.GetComponent<DraggableObject> ();
			if (draggableObject != null) {
				var hitCount = gameObjectsHitted [gameObject].Count;
				var hitOne = hitCount > 0 ? gameObjectsHitted [gameObject][0] : Vector2.zero;
				var hitTwo = hitCount > 1 ? gameObjectsHitted [gameObject][1] : Vector2.zero;
				draggableObject.Touch (hitCount, hitOne, hitTwo); 
			}
		}
	}
	*/
}
