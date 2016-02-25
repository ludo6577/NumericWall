using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TouchScript;
using TouchScript.Gestures;
using TouchScript.Utils;

public enum ObjectState{
	Idle = 0,	// Do nothing
	Moving,		// Moving to designed position
	Dragged		// Dragged by user
}


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

	/*
	 * Private variables
	 */ 
	private WallScript wall;
	private ObjectState currentState;
	private int layer;
	private string name;
	private Vector2 destination;

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
	 * 	Start / Update
	 */
	public void Init(Transform parent, WallScript wall, string name){
		//this.Move (initialSpeed * (InitialVelocity));
		this.transform.SetParent(parent, false);
		this.currentState = ObjectState.Idle;
		this.wall = wall;
		this.layer = -1;
		this.name = name;
	}

	public void Update(){
		if (wall == null)
			return;

		switch (currentState) {
			case ObjectState.Idle:
				RigidBody.isKinematic = false;
			break;

			case ObjectState.Moving:
				RigidBody.isKinematic = true;
				if (!wall.Grid.MoveToCell (RectTransform, destination)) {
					currentState = ObjectState.Idle;
				}
			break;

			case ObjectState.Dragged: 
			break;
			
		}

		// Goes out
		if (RectTransform.anchoredPosition.x < 0 
			|| RectTransform.anchoredPosition.y < 0 
			|| RectTransform.anchoredPosition.x > wall.RectTransform.sizeDelta.x 
			|| RectTransform.anchoredPosition.y > wall.RectTransform.sizeDelta.y) {

			Vector2 initialPosition = new Vector2 (RectTransform.anchoredPosition.x , RectTransform.anchoredPosition.y);
			Vector2 center = new Vector2 (wall.RectTransform.sizeDelta.x / 2, wall.RectTransform.sizeDelta.y / 2);
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
	}

	private void OnDisable()
	{
		GetComponent<TransformGesture> ().Transformed -= transformedHandler;
		GetComponent<TransformGesture>().TransformCompleted -= transformCompletedhandler;
	}

	private void transformedHandler(object sender, EventArgs e)
	{
		RigidBody.isKinematic = true;
		currentState = ObjectState.Dragged;
		SetLayer (0);
	}

	private void transformCompletedhandler(object sender, EventArgs e)
	{
		RigidBody.isKinematic = false;
		currentState = ObjectState.Idle;
		var gesture = (TransformGesture)sender;
		Move (gesture.DeltaPosition * 4);
	}




	public void Move(Vector2 direction){
		RigidBody.velocity = direction * Time.deltaTime;
		RigidBody.AddForce( direction * Time.deltaTime, ForceMode2D.Impulse);
	}


	public void SetLayer(int layerNumber){
		if (this.layer == layerNumber)
			return;
		
		layer = layerNumber;

		var unityLayer = LayerMask.NameToLayer("Layer" + layerNumber);
		gameObject.layer = unityLayer >= 0 ? unityLayer : wall.LayersCount + 7;
		gameObject.name = name + "(Layer: " + layerNumber +")";

		if (layerNumber == 0) {
			transform.SetAsLastSibling ();
			wall.UpdateLayers ();
		}

		transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, layerNumber * 10 - (wall.LayersCount * 10));
	}

	public void SetGridPosition(Vector2 pos){
		destination = pos;
		currentState = ObjectState.Moving;
	}
}