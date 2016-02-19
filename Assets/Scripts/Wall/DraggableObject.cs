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
	public string Name;

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
	protected Rigidbody2D RigidBody{
		get{ 
			if(_rigidBody == null)
				_rigidBody = GetComponent<Rigidbody2D> ();
			return _rigidBody; 
		}
	}

	private RectTransform _rectTransform;
	protected RectTransform RectTransform{
		get{ 
			if(_rectTransform == null)
				_rectTransform = GetComponent<RectTransform> ();
			return _rectTransform; 
		}
	}

	/*
	 * 	private
	 */
	private bool fullScreen;
	private Vector2 lastTouchPosition;




	/*
	 * 	Start / Update
	 */
	public void Start(){
	}

	public void Update(){
		if (Wall == null)
			return;

		// Goes out
		if (RectTransform.anchoredPosition.x < 0 
			|| RectTransform.anchoredPosition.y < 0 
			|| RectTransform.anchoredPosition.x > Wall.RectTransform.sizeDelta.x 
			|| RectTransform.anchoredPosition.y > Wall.RectTransform.sizeDelta.y) {

			Vector2 initialPosition = new Vector2 (RectTransform.anchoredPosition.x , RectTransform.anchoredPosition.y);
			Vector2 center = new Vector2 (Wall.RectTransform.sizeDelta.x / 2, Wall.RectTransform.sizeDelta.y / 2);
			Vector2 direction = center - initialPosition;
			Move (direction/10);
		}
	}




	/*
	 * 	EVENTS
	 */
	private void OnEnable()
	{
		GetComponent<TransformGesture>().Transformed += transformedHandler;
		GetComponent<TransformGesture>().TransformCompleted += transformCompletedhandler;
		//GetComponent<TapGesture>().Tapped += tappedHandler;
	}

	private void OnDisable()
	{
		GetComponent<TransformGesture> ().Transformed -= transformedHandler;
		GetComponent<TransformGesture>().TransformCompleted -= transformCompletedhandler;
		//GetComponent<TapGesture>().Tapped -= tappedHandler;
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
		var gesture = (TransformGesture)sender;
		Move (gesture.DeltaPosition * 4);
	}




	public void Move(Vector2 direction){
		RigidBody.velocity = direction * Time.deltaTime;
		RigidBody.AddForce( direction * Time.deltaTime, ForceMode2D.Impulse);
	}


	public void SetLayer(int layerNumber){
		if (fullScreen)
			return;

		Layer = layerNumber;

		var layer = LayerMask.NameToLayer("Layer" + layerNumber);
		gameObject.layer = layer >= 0 ? layer : Wall.LayersCount;
		gameObject.name = Name + "(Layer: " + layerNumber +")";

		if (layerNumber == 0) {
			transform.SetAsLastSibling ();
			Wall.UpdateLayers ();
		}

		//transform.position = new Vector3(transform.position.x, transform.position.y, layerNumber * 10);
		//destinationScale = new Vector2(1/( 1 + (layerNumber * ScaleFactor)), 1/( 1 + (layerNumber * ScaleFactor)));
	}
}