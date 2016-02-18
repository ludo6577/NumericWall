using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TouchScript;
using TouchScript.Gestures;
using TouchScript.Utils;

[RequireComponent(typeof(Image))]
public class DraggableObject : MonoBehaviour {

	/*
	 * 	Public variables
	 */
	[Range(0, 1)]
	public float DoubleClickDelay = 0.5f;
	[Range(0, 1)]
	public float ScaleFactor = 0.1f;
	[Range(0, 10)]
	public float ScaleSpeed = 0.1f;
	[Range(0, 100)]
	public float Inertia = 10f;

	[HideInInspector]
	public int Layer;

	[HideInInspector]
	public WallScript Wall;

	[HideInInspector]
	public List<TouchPoint> TouchsPoints;

	[HideInInspector]
	public bool Dragged;

	/*
	 *	Object components 
	 */
	private Rigidbody2D _rigidBody;
	private Rigidbody2D RigidBody{
		get{ 
			if(_rigidBody == null)
				_rigidBody = GetComponent<Rigidbody2D> ();
			return _rigidBody; 
		}
	}
		
	private RectTransform _rectTransform;
	private RectTransform RectTransform{
		get{ 
			if(_rectTransform == null)
				_rectTransform = GetComponent<RectTransform> ();
			return _rectTransform; 
		}
	}
		
	/*
	 * 	private
	 */
	private bool dragged;
	private bool fullScreen;
	private Vector2 previousTouchPosition;


	public void Start(){
		TouchsPoints = new List<TouchPoint>();
		previousTouchPosition = Vector2.zero;
		dragged = false;
		fullScreen = false;
	}

	private void OnEnable()
	{
		GetComponent<TapGesture>().Tapped += tappedHandler;
		GetComponent<TransformGesture>().Transformed += transformedHandler;
		GetComponent<TransformGesture>().TransformCompleted += transformCompletedhandler;
	}

	private void OnDisable()
	{
		GetComponent<TransformGesture> ().Transformed -= transformedHandler;
		GetComponent<TransformGesture>().TransformCompleted -= transformCompletedhandler;
		GetComponent<TapGesture>().Tapped -= tappedHandler;
	}

	public void Update(){
		if (Wall == null)
			return;
		
		// Goes out
		if (RectTransform.anchoredPosition.x < 0 
			|| RectTransform.anchoredPosition.y < 0 
			|| RectTransform.anchoredPosition.x > 1920 
			|| RectTransform.anchoredPosition.y > 1080) {

			Vector2 initialPosition = new Vector2 (RectTransform.anchoredPosition.x , RectTransform.anchoredPosition.y);
			Vector2 center = new Vector2 (1920 / 2, 1080 / 2);
			Vector2 direction = center - initialPosition;
			Move (direction/10);
		}


		// Update size
		//if ((fullScreen && transform.localScale.x < destinationScale.x - 0.001f) || (!fullScreen && transform.localScale.x > destinationScale.x + 0.001f))
		//	transform.localScale = Vector2.Lerp (transform.localScale, destinationScale, ScaleSpeed * Time.deltaTime);
	}


	public void SetImage(Sprite sprite){
		var imageObject = GetComponentInChildren<ImageObject> ();
		imageObject.SetImage (sprite);

		var collider = GetComponent<BoxCollider2D> ();

		var ratioX = sprite.rect.width / sprite.rect.height;
		var ratioY = sprite.rect.height / sprite.rect.width;
		if (ratioX > 1) {
			var size = new Vector2 (RectTransform.sizeDelta.x * ratioX, RectTransform.sizeDelta.y);;
			RectTransform.sizeDelta = size;
			collider.size = size;
		} else {
			var size = new Vector2 (RectTransform.sizeDelta.x, RectTransform.sizeDelta.y * ratioY);
			RectTransform.sizeDelta = size;
			collider.size = size;
		}
	}


	public void Move(Vector2 direction){
		RigidBody.velocity = direction * Time.deltaTime;
		RigidBody.AddRelativeForce( direction * Time.deltaTime, ForceMode2D.Impulse);
	}


	public void SetLayer(int layerNumber){
		if (fullScreen)
			return;

		Layer = layerNumber;

		var layer = LayerMask.NameToLayer("Layer" + layerNumber);
		gameObject.layer = layer >= 0 ? layer : Wall.LayersCount;
		gameObject.name = Wall.ObjectPrefab.name + "(Layer: " + layerNumber +")";

		if (layerNumber == 0) {
			transform.SetAsLastSibling ();
			Wall.UpdateLayers ();
		}

		//transform.position = new Vector3(transform.position.x, transform.position.y, layerNumber * 10);
		//destinationScale = new Vector2(1/( 1 + (layerNumber * ScaleFactor)), 1/( 1 + (layerNumber * ScaleFactor)));
	}





	private void transformedHandler(object sender, EventArgs e)
	{
		Dragged = true;
		SetLayer (0);
		RigidBody.isKinematic = true;
	}
	private void transformCompletedhandler(object sender, EventArgs e)
	{
		Dragged = false;
		RigidBody.isKinematic = false;
	}


	private void tappedHandler(object sender, EventArgs e)
	{
		//TODO
		/*
		fullScreen = !fullScreen;
		if (fullScreen) {
			//SetLayer (0);
			previousScale = transform.localScale;
			var scale = Camera.main.orthographicSize / 2;
			destinationScale = new Vector2(scale, scale);
		}
		else {
			destinationScale = previousScale;
		}
		*/
	}

}