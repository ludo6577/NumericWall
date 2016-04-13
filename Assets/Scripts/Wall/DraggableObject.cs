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

	[Range(0.5f, 5f)]
	public float MaxIdleTime;


    private ObjectState _currentState;

    protected ObjectState CurrentState
    {
        get { return _currentState; }
        set
        {
            _currentState = value;
            gameObject.name = GetObjectName();
        }
    }

    /*
	 * Private variables
	 */
    private string draggableObjectName;
    private WallScript wall;
	private int layer;
	private Vector2 destinationPosition;
    private Vector2 destinationScale;
    private bool forceScale;

    private float destinationColor;

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

    private Image _image;
    protected Image Image
    {
        get
        {
            if (_image == null)
                _image = GetComponent<Image>();
            return _image;
        }
    }

    private Image _childImage;
    protected Image ChildImage
    {
        get
        {
            if (_childImage == null)
                _childImage = transform.GetChild(0).GetComponentInChildren<Image>();
            return _childImage;
        }
    }

    /*
	 * 	Start / Update
	 */
    public void Init(Transform parent, WallScript wall, string name){
		this.transform.SetParent(parent, false);
        this.CurrentState = ObjectState.Idle;
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

		switch (CurrentState) {
			case ObjectState.Idle:
                RigidBody.isKinematic = false;
			break;

			case ObjectState.Moving:
                RigidBody.isKinematic = false;
				if (!wall.Grid.MoveToCell (RectTransform, destinationPosition))
					CurrentState = ObjectState.Idle;
			break;

			case ObjectState.Dragged: 
			break;

            case ObjectState.Launched:
		        if (Time.time - idleTime > MaxIdleTime)
		        {
		            CurrentState = ObjectState.Idle;
                    SetLayer(1);
                    wall.UpdateLayers();
                }
		        break;
		}

        ControlPosition();
        ControlRotation();
        ControlScale();
	    ControlColor();
	}

    /*
	 * 	EVENTS
	 */
    protected void OnEnable()
	{
        GetComponent<TapGesture>().StateChanged += pressStateChangedHandler;
        GetComponent<TransformGesture>().StateChanged += transformStateChangedHandler;
	}

	protected void OnDisable()
	{
        GetComponent<TapGesture>().StateChanged -= pressStateChangedHandler;
        GetComponent<TransformGesture>().StateChanged -= transformStateChangedHandler;
	}


    // On press
    private void pressStateChangedHandler(object sender, GestureStateChangeEventArgs e)
    {
        //Debug.Log(e.State);
        switch (e.State)
        {
            case Gesture.GestureState.Recognized:
                CurrentState = ObjectState.Launched;
                RigidBody.isKinematic = true;
                idleTime = Time.time;
                SetLayer(0);
                wall.UpdateLayers();
            break;
        }
        
    }
    private void transformStateChangedHandler(object sender, GestureStateChangeEventArgs e)
    {
        switch (e.State)
        {
            case Gesture.GestureState.Began:
                CurrentState = ObjectState.Dragged;
                RigidBody.isKinematic = true;
                idleTime = Time.time;
                SetLayer(0);
            break;
            case Gesture.GestureState.Changed:
                CurrentState = ObjectState.Dragged;
                RigidBody.isKinematic = true;
                idleTime = Time.time;
                SetLayer(0);
            break;
            case Gesture.GestureState.Ended:
                CurrentState = ObjectState.Launched;
                RigidBody.isKinematic = false;
                idleTime = Time.time;
                RigidBody.AddForce(((TransformGesture)sender).DeltaPosition * Inertia, ForceMode2D.Impulse);
            break;
        }

    }


    // Collision with other object
    public void OnTriggerStay2D(Collider2D other)
    {
        var otherObj = other.gameObject.GetComponent<DraggableObject>();
        if (otherObj != null && RigidBody.velocity.magnitude < MaxVelocity && CurrentState == ObjectState.Idle && otherObj.CurrentState != ObjectState.Moving)
        {
            var direction = (GetCenter() - otherObj.GetCenter());
            direction.Normalize();

            var power = transform.GetSiblingIndex() > otherObj.transform.GetSiblingIndex() ? 0.5f : 2f;

            RigidBody.AddForce(direction * power, ForceMode2D.Impulse);
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
        if (RigidBody.velocity.magnitude < MaxVelocity/2 && (RectTransform.anchoredPosition.x < 0 && RigidBody.velocity.x < 0) || (RectTransform.anchoredPosition.x > wall.RectTransform.sizeDelta.x && RigidBody.velocity.x > 0))
        {
            RigidBody.AddForce(new Vector2(-1.5f * RigidBody.velocity.x, RigidBody.velocity.y), ForceMode2D.Impulse);
        }
        // Goes out (Y)
        if (RigidBody.velocity.magnitude < MaxVelocity/2 && (RectTransform.anchoredPosition.y < 0 && RigidBody.velocity.y < 0) || (RectTransform.anchoredPosition.y > wall.RectTransform.sizeDelta.y && RigidBody.velocity.y > 0))
        {
            RigidBody.AddForce(new Vector2(RigidBody.velocity.x, -1.5f * RigidBody.velocity.y), ForceMode2D.Impulse);
        }
    }

    private void ControlColor()
    {
        var rgb = Image.color.r;
        if (Math.Abs(rgb - destinationColor) > 0.02f)
        {
            var diff = 0.01f;
            rgb = rgb > destinationColor ? rgb - diff : rgb + diff;
            Image.color = new Color(rgb, rgb, rgb, 1f);

            //TODO VIDEO
            if(ChildImage!=null)
                ChildImage.color = new Color(rgb, rgb, rgb, 1f);
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
		    gameObject.name = GetObjectName();

            destinationScale = new Vector2(1f - (layerNumber * 0.2f), 1f - (layerNumber * 0.2f));
            destinationColor = 1f - layer * 0.2f;
		}

		var index = transform.GetSiblingIndex ();
		transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, (layerNumber * 10f) - (index * 0.01f) - (wall.LayersCount * 10f) );
	}

    private string GetObjectName()
    {
        return draggableObjectName + "(" + CurrentState + " on layer " + layer + ")";
    }

    public void SetGridPosition(Vector2 pos)
	{
	    if (CurrentState == ObjectState.Dragged || CurrentState == ObjectState.Launched)
	        return;

		destinationPosition = pos;
		CurrentState = ObjectState.Moving;
	}

    public Vector2 GetCenter()
    {
        return new Vector2(RectTransform.position.x + RectTransform.rect.width, RectTransform.position.y + RectTransform.rect.height);
    }
}