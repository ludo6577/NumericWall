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
	Dragged,	// Dragged by user
    Launched    // After a drag
}


[RequireComponent(typeof(Image))]
public class DraggableObject : MonoBehaviour {

    /*
	 * 	Public variables
	 */
    public bool UnityCollisionSystem;

    [Range(0f, 1000f)]
	public float Inertia = 10f;

    [Range(0f, 100f)]
    public float MaxVelocity;

    [Range(0f, 0.999f)]
	public float MinScale = 0.5f;

	[Range(1.001f, 2f)]
	public float MaxScale = 1.5f;

	[Range(1f, 100f)]
	public float MaxIdleTime;


    /*
	 * Private variables
	 */
    private string draggableObjectName;
    private WallScript wall;
	private ObjectState currentState;
	private int layer;
	private Vector2 destinationPosition;
    private Vector2 destinationScale;
    private bool forceScale;
    private float idleTime;

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
		this.transform.SetParent(parent, false);
        this.currentState = ObjectState.Idle;
        this.draggableObjectName = name;
        this.forceScale = false;
        this.wall = wall;
		this.layer = -1;

        var collider = GetComponent<BoxCollider2D>();
        if (UnityCollisionSystem)
            collider.isTrigger = false;
        else
            collider.isTrigger = true;
    }

	public void Update(){
		if (wall == null)
			return;

		switch (currentState) {
			case ObjectState.Idle:
                RigidBody.isKinematic = false;
			break;

			case ObjectState.Moving:
                RigidBody.isKinematic = false;
				if (!wall.Grid.MoveToCell (RectTransform, destinationPosition))
					currentState = ObjectState.Idle;
			break;

			case ObjectState.Dragged: 
			break;

            case ObjectState.Launched:
		        if (idleTime > MaxIdleTime)
		            currentState = ObjectState.Idle;
            break;
		}

        ControlPosition();
        ControlRotation();
        ControlScale();
	}

    /*
	 * 	EVENTS
	 */
    protected void OnEnable()
	{
		GetComponent<PressGesture>().Pressed += pressedHandler;
		GetComponent<TransformGesture>().TransformStarted += transformStartedHandler;
		GetComponent<TransformGesture>().Transformed += transformedHandler;
		GetComponent<TransformGesture>().TransformCompleted += transformCompletedhandler;
	}

	protected void OnDisable()
	{
		GetComponent<PressGesture>().Pressed += pressedHandler;
		GetComponent<TransformGesture>().TransformStarted -= transformStartedHandler;
		GetComponent<TransformGesture> ().Transformed -= transformedHandler;
		GetComponent<TransformGesture>().TransformCompleted -= transformCompletedhandler;
	}
    
    // On press
    private void pressedHandler(object sender, EventArgs e){
		currentState = ObjectState.Dragged;
        RigidBody.isKinematic = true;
        idleTime = Time.time;
        SetLayer (0);
        wall.UpdateLayers();
    }

	// Begin transform
	private void transformStartedHandler(object sender, EventArgs e){
        currentState = ObjectState.Dragged;
        RigidBody.isKinematic = true;
        idleTime = Time.time;
        SetLayer (0);
	}

	// transform in progress...
	private void transformedHandler(object sender, EventArgs e)
	{
        currentState = ObjectState.Dragged;
        RigidBody.isKinematic = true;
        idleTime = Time.time;
        SetLayer (0);
	}

	// End transform
	private void transformCompletedhandler(object sender, EventArgs e)
	{
        currentState = ObjectState.Launched;
        RigidBody.isKinematic = false;
        idleTime = Time.time;

        var gesture = (TransformGesture)sender;
        RigidBody.AddForce(gesture.DeltaPosition * Inertia, ForceMode2D.Impulse);
	}
    
    // Collision with other object
    public void OnTriggerStay2D(Collider2D other)
    {
        var otherObj = other.gameObject.GetComponent<DraggableObject>();
        if (otherObj != null && RigidBody.velocity.magnitude < MaxVelocity && currentState == ObjectState.Idle)
        {
            RigidBody.AddForce(GetCenter() - otherObj.GetCenter(), ForceMode2D.Force);
        }
    }

    /*
     *  Behaviours (control the scale/rotation/position)
     */
    private void ControlScale(){
        // Max scale (X)
		if (transform.localScale.x >= MaxScale)
			transform.localScale = new Vector2(MaxScale, transform.localScale.y);
		if (transform.localScale.x <= MinScale)
			transform.localScale = new Vector2(MinScale, transform.localScale.y);
        // Max scale (Y)
        if (transform.localScale.y >= MaxScale)
			transform.localScale = new Vector2(transform.localScale.x, MaxScale);
		if (transform.localScale.y <= MinScale)
			transform.localScale = new Vector2(transform.localScale.x, MinScale);

        // Get back to normal scale
        if (forceScale || Time.time > idleTime + MaxIdleTime && Math.Abs(transform.localScale.x - destinationScale.x) > 0.01f)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, destinationScale, 0.02f);
        }
        else
        {
            forceScale = false;
        }		
	}

	private void ControlRotation(){
        // Go back to normal rotation
		if (Time.time > idleTime + MaxIdleTime && transform.rotation != Quaternion.identity) {
			transform.rotation = Quaternion.Lerp (transform.rotation, Quaternion.identity, 0.05f);
		}		
	}

    private void ControlPosition()
    {
        // Goes out (X)
        if (RigidBody.velocity.magnitude < MaxVelocity && (RectTransform.anchoredPosition.x < 0 && RigidBody.velocity.x < 0) || (RectTransform.anchoredPosition.x > wall.RectTransform.sizeDelta.x && RigidBody.velocity.x > 0))
        {
            RigidBody.AddForce(new Vector2(-1.5f * RigidBody.velocity.x, RigidBody.velocity.y), ForceMode2D.Impulse);
        }
        // Goes out (Y)
        if (RigidBody.velocity.magnitude < MaxVelocity && (RectTransform.anchoredPosition.y < 0 && RigidBody.velocity.y < 0) || (RectTransform.anchoredPosition.y > wall.RectTransform.sizeDelta.y && RigidBody.velocity.y > 0))
        {
            RigidBody.AddForce(new Vector2(RigidBody.velocity.x, -1.5f * RigidBody.velocity.y), ForceMode2D.Impulse);
        }
    }


    /*
     *  Utility methods
     */
    public void SetLayer(int layerNumber){
		if (this.layer != layerNumber)
		{
		    forceScale = true;
            layer = layerNumber;
            if (layerNumber == 0)
                transform.SetAsLastSibling();

            var unityLayer = LayerMask.NameToLayer ("Layer" + layerNumber);
			gameObject.layer = unityLayer >= 0 ? unityLayer : wall.LayersCount + 7;
			gameObject.name = draggableObjectName + "(Layer: " + layerNumber + ")";

            destinationScale = new Vector2(1f - (layerNumber * 0.2f), 1f - (layerNumber * 0.2f));
        }

		var index = transform.GetSiblingIndex ();
		transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, (layerNumber * 10f) - (index * 0.01f) - (wall.LayersCount * 10f) );
	}

	public void SetGridPosition(Vector2 pos)
	{
	    if (currentState == ObjectState.Dragged || currentState == ObjectState.Launched)
	        return;

		destinationPosition = pos;
		currentState = ObjectState.Moving;
	}

    public Vector2 GetCenter()
    {
        return new Vector2(RectTransform.position.x + RectTransform.rect.width, RectTransform.position.y + RectTransform.rect.height);
    }
}