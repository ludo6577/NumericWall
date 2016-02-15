using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TouchScript;
using System.Collections;
using System.Collections.Generic;

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
	//public float ZoomSpeed = 0.5f;        // The rate of change of the field of view in perspective mode.

	[HideInInspector]
	public int Layer;

	[HideInInspector]
	public WallScript Wall;

	[HideInInspector]
	public List<TouchPoint> TouchsPoints;

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
	 * 	Touchs
	 */
	/*private int touchCount;
	private Vector2 touchPositionZero;
	private Vector2 touchPositionOne;*/

	/*
	 * 	private
	 */
	private bool dragged;
	private bool fullScreen;
	private Vector2 previousTouchPosition;
	private Vector2 touchPositionToCenter;
	private float previousTouchTime;

	private bool pinched;
	private float previousPinchDistance;

	private Vector2 previousScale;
	private Vector2 destinationScale;


	public void Start(){
		TouchsPoints = new List<TouchPoint>();
		previousTouchPosition = Vector2.zero;
		previousTouchTime = 0f;
		dragged = false;
		pinched = false;
		fullScreen = false;
		destinationScale = new Vector2 (1, 1);
	}

	public void SetImage(Sprite sprite){
		var imageObject = GetComponentInChildren<ImageObject> ();
		imageObject.SetImage (sprite);

		var ratioX = sprite.rect.width / sprite.rect.height;
		//var ratioY = sprite.rect.height / sprite.rect.width;
		RectTransform.sizeDelta = new Vector2 (RectTransform.sizeDelta.x * ratioX, RectTransform.sizeDelta.y);
	}
		
	/*public void Touch(int touchCount, Vector2 touchPositionZero, Vector2 touchPositionOne){
		this.touchCount = touchCount;
		this.touchPositionZero = touchPositionZero;
		this.touchPositionOne = touchPositionOne;
	}*/

	public void Move(Vector2 direction){
		RigidBody.velocity = direction * Time.deltaTime * Inertia * 100;
		RigidBody.AddRelativeForce( direction * Time.deltaTime * Inertia * 100, ForceMode2D.Impulse);
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
		
		transform.position = new Vector3(transform.position.x, transform.position.y, layerNumber * 10);
		destinationScale = new Vector2(1/( 1 + (layerNumber * ScaleFactor)), 1/( 1 + (layerNumber * ScaleFactor)));
	}
		
	public void Compute()
	{
		Vector2 touchPositionZero = new Vector2(0,0);
		if (TouchsPoints.Count > 0)
			touchPositionZero = TouchsPoints [0].Position;

		Vector2 touchPositionOne = new Vector2(0,0);
		if (TouchsPoints.Count > 1)
			touchPositionZero = TouchsPoints [1].Position;
		
		// Update size
		if (transform.localScale.x < destinationScale.x - 0.001f || transform.localScale.x > destinationScale.x + 0.001f)
			transform.localScale = Vector2.Lerp (transform.localScale, destinationScale, ScaleSpeed * Time.deltaTime);

		/*
		 * 	Simple touch
		 */
		// Do nothing
		if (TouchsPoints.Count == 0 && !dragged)
			return;

		// Released (add force and reset parameters)
		if (TouchsPoints.Count == 0 && dragged) {	
			SetLayer (1);

			var deltaTouch = touchPositionZero - previousTouchPosition;
			Move(deltaTouch);

			dragged = false;
			pinched = false;
			RigidBody.isKinematic = false;
			previousTouchPosition = Vector2.zero;
			return;
		}

		// New touch
		if (!dragged) {
			dragged = true;
			RigidBody.isKinematic = true;
			touchPositionToCenter = new Vector2(transform.position.x - touchPositionZero.x, transform.position.y - touchPositionZero.y);
			SetLayer (0);

			if (Time.time - previousTouchTime < DoubleClickDelay)
				DoubleTouch ();
		} 

		// Follow touch
		transform.position = new Vector3(touchPositionZero.x + touchPositionToCenter.x, touchPositionZero.y +  touchPositionToCenter.y, transform.position.z);
		previousTouchPosition = touchPositionZero;
		previousTouchTime = Time.time;


		/*
		 * 		Pinch
		 */
		// Do not pinch anymore
		if (TouchsPoints.Count < 2 && pinched)
			pinched = false;
		
		// New pinch
		if (TouchsPoints.Count >= 2 && !pinched) {
			pinched = true;
			previousPinchDistance = Vector2.Distance (touchPositionZero, touchPositionOne);
		} else if(pinched) {
			var currentPinchDistance = Vector2.Distance (touchPositionZero, touchPositionOne);
			var deltaPinch = (currentPinchDistance - previousPinchDistance)/100;
			previousPinchDistance = currentPinchDistance;
			destinationScale = new Vector2(destinationScale.x + deltaPinch, destinationScale.y + deltaPinch);
		}

		// Reset
		TouchsPoints.Clear();
	}

	private void DoubleTouch(){
		fullScreen = !fullScreen;

		if (fullScreen) {
			SetLayer (0);
			previousScale = transform.localScale;
			var scale = Camera.main.orthographicSize / 2;
			destinationScale = new Vector2(scale, scale);
		}
		else {
			destinationScale = previousScale;
		}
	}
}