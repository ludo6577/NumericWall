using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

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

	/*
	 *	Object components 
	 */
	private Rigidbody2D rigidBody;
	private RectTransform rectTransf;

	/*
	 * 	Touchs
	 */
	private int touchCount;
	private Vector2 touchPositionZero;
	private Vector2 touchPositionOne;

	/*
	 * 	private
	 */
	private bool dragged;
	private bool fullScreen;
	private Vector2 previousTouchPosition;
	private Vector2 touchPositionToCenter;
	private float previousTouchTime;

	private Vector2 previousScale;
	private Vector2 destinationScale;


	public void Start(){
		rigidBody = GetComponent<Rigidbody2D> ();
		rectTransf = GetComponent<RectTransform> ();

		touchCount = 0;
		previousTouchPosition = Vector2.zero;
		previousTouchTime = 0f;
		dragged = false;
		fullScreen = false;
	}

	public void SetImage(Sprite sprite){
		var imageObject = GetComponentInChildren<ImageObject> ();
		imageObject.SetImage (sprite);
	}
		
	public void Touch(int touchCount, Vector2 touchPositionZero, Vector2 touchPositionOne){
		this.touchCount = touchCount;
		this.touchPositionZero = touchPositionZero;
		this.touchPositionOne = touchPositionOne;
	}

	public void SetLayer(int layerNumber){
		if (fullScreen)
			return;
		
		if(layerNumber==0)
			transform.SetAsLastSibling();
		else
			transform.SetSiblingIndex(10 - layerNumber);
		transform.position = new Vector3(transform.position.x, transform.position.y, layerNumber * 10);
		destinationScale = new Vector2(1/( 1 + (layerNumber * ScaleFactor)), 1/( 1 + (layerNumber * ScaleFactor)));
	}

	public void Update()
	{
		// Update size / position
		if (transform.localScale.x < destinationScale.x - 0.001f || transform.localScale.x > destinationScale.x + 0.001f)
			transform.localScale = Vector2.Lerp (transform.localScale, destinationScale, ScaleSpeed * Time.deltaTime);

		// Do nothing
		if (touchCount == 0 && !dragged)
			return;

		// Released (add force and reset parameters)
		if (touchCount == 0 && dragged) {	
			SetLayer (1);

			var deltaTouch = touchPositionZero - previousTouchPosition;
			rigidBody.velocity = deltaTouch * Time.deltaTime * Inertia * 100;
			rigidBody.AddRelativeForce( deltaTouch * Time.deltaTime * Inertia * 100, ForceMode2D.Impulse);

			dragged = false;
			rigidBody.isKinematic = false;
			previousTouchPosition = Vector2.zero;
			return;
		}

		// New touch
		if (!dragged) {
			dragged = true;
			rigidBody.isKinematic = true;
			touchPositionToCenter = new Vector2(transform.position.x - touchPositionZero.x, transform.position.y - touchPositionZero.y);
			SetLayer (0);

			if (Time.time - previousTouchTime < DoubleClickDelay)
				DoubleTouch ();
		} 	

		// Follow touch
		transform.position = new Vector3(touchPositionZero.x + touchPositionToCenter.x, touchPositionZero.y +  touchPositionToCenter.y, transform.position.z);
		previousTouchPosition = touchPositionZero;
		previousTouchTime = Time.time;

		// Reset
		touchCount = 0;
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