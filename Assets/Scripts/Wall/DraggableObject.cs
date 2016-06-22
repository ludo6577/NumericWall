﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TouchScript.Gestures;

public enum ObjectState{
	Idle = 0,	    // Do nothing
	Moving,         // Moving to designed position
    Pressed,        // Pressed by user
    Released,       // Released without Transform
    Transformed,    // Transformed by user (moved, scaled, rotated)
    Launched        // After a Transform
}


public class DraggableObject : MonoBehaviour {

    /*
	 * 	Public variables
	 */
    private ObjectState currentState;
    protected ObjectState CurrentState
    {
        get { return currentState; }
        set
        {
            currentState = value;
            gameObject.name = GetObjectName();
            switch (value)
            {
                case ObjectState.Idle:
                    RigidBody2D.isKinematic = false;
                    break;

                case ObjectState.Moving:
                    RigidBody2D.isKinematic = false;
                    break;

                case ObjectState.Pressed:
                case ObjectState.Transformed:
                    break;

                case ObjectState.Launched:
                case ObjectState.Released:
                    break;
            }
        }
    }

    private bool isTriggered;

    /*
	 * Private variables
	 */
    private string draggableObjectName;
    private WallScript wall;
	private int layer;
	private Vector2 destinationPosition;
    private Vector2 destinationScale;
    //private bool scaleImmediatly;

    private float destinationColor;

    private float idleTime;

    /*
	 *	Object components 
	 */
    private Rigidbody2D _rigidBody2D;
	protected Rigidbody2D RigidBody2D{
		get{ 
			if(_rigidBody2D == null)
				_rigidBody2D = GetComponent<Rigidbody2D> ();
			return _rigidBody2D; 
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

    private RawImage _childImage;
    protected RawImage ChildImage
    {
        get
        {
            if (_childImage == null)
                _childImage = transform.GetChild(0).GetComponentInChildren<RawImage>();
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
        this.isTriggered = false;
        //this.scaleImmediatly = false;
        this.wall = wall;
		this.layer = -1;

        var collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
    }

	public void Update(){
		if (wall == null)
			return;

        //Debug.Log(CurrentState);

        switch (CurrentState) {
			case ObjectState.Idle:
			break;

			case ObjectState.Moving:
				if (wall.Grid.MoveToCell(RectTransform, destinationPosition))
					CurrentState = ObjectState.Idle;
			break;
                
			case ObjectState.Pressed: 
            case ObjectState.Transformed:
			break;

            case ObjectState.Launched:
            case ObjectState.Released:
                if (Time.time - idleTime > WallScript.Get().MaxIdleTime)
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
        GetComponent<PressGesture>().StateChanged += pressStateChangedHandler;
        GetComponent<ReleaseGesture>().StateChanged += releaseStateChangedHandler;
        GetComponent<TransformGesture>().StateChanged += transformStateChangedHandler;
	}

	protected void OnDisable()
	{
        GetComponent<TapGesture>().StateChanged -= pressStateChangedHandler;
        GetComponent<PressGesture>().StateChanged -= pressStateChangedHandler;
        GetComponent<ReleaseGesture>().StateChanged -= releaseStateChangedHandler;
        GetComponent<TransformGesture>().StateChanged -= transformStateChangedHandler;
	}
    
    // On press
    private void pressStateChangedHandler(object sender, GestureStateChangeEventArgs e)
    {
        //Debug.Log(e.State);
        switch (e.State)
        {
            case Gesture.GestureState.Recognized:
                CurrentState = ObjectState.Pressed;
                RigidBody2D.isKinematic = true;
                idleTime = Time.time;
                SetLayer(0);
                wall.UpdateLayers();
            break;
        }
    }
    private void releaseStateChangedHandler(object sender, GestureStateChangeEventArgs e)
    {
        //Debug.Log(e.State);
        switch (e.State)
        {
            case Gesture.GestureState.Recognized:
                CurrentState = ObjectState.Released;
                RigidBody2D.isKinematic = false;
                idleTime = Time.time;
                break;
        }
    }
    private void transformStateChangedHandler(object sender, GestureStateChangeEventArgs e)
    {
        //Debug.Log(e.State);
        switch (e.State)
        {
            case Gesture.GestureState.Began:
            case Gesture.GestureState.Changed:
                CurrentState = ObjectState.Transformed;
                RigidBody2D.isKinematic = true;
                idleTime = Time.time;
                SetLayer(0);
            break;
            case Gesture.GestureState.Ended:
                CurrentState = ObjectState.Launched;
                RigidBody2D.isKinematic = false;
                idleTime = Time.time;
                RigidBody2D.AddForce(((TransformGesture)sender).DeltaPosition * WallScript.Get().Inertia, ForceMode2D.Impulse);
            break;
        }

    }


    // Collision with other object
    public void OnTriggerStay2D(Collider2D other)
    {
        var otherObj = other.gameObject.GetComponent<DraggableObject>();

        if (otherObj != null && RigidBody2D.velocity.magnitude < WallScript.Get().MaxVelocity &&
            (CurrentState != ObjectState.Pressed && CurrentState != ObjectState.Transformed)  &&
            (otherObj.CurrentState == ObjectState.Pressed || otherObj.CurrentState == ObjectState.Transformed || otherObj.CurrentState == ObjectState.Launched))
        {
            if (CurrentState == ObjectState.Moving)
                CurrentState = ObjectState.Idle;

            isTriggered = true;

            var direction = (GetCenter() - otherObj.GetCenter());
            direction.Normalize();
            
            //var power = transform.GetSiblingIndex() > otherObj.transform.GetSiblingIndex() ? 1f : 4f;
            var power = 4;
            
            RigidBody2D.AddForce(direction * power, ForceMode2D.Impulse);
        }
    }

    public void OnTriggerExit2D(Collider2D other)
    {
        isTriggered = false;
    }

    /*
     *  Behaviours (control the scale/rotation/position)
     */
    private void ControlScale(){
        // Max scale (X)
		if (transform.localScale.x >= WallScript.Get().MaxScale)
			transform.localScale = new Vector2(WallScript.Get().MaxScale, transform.localScale.y);
		if (transform.localScale.x <= WallScript.Get().MinScale)
			transform.localScale = new Vector2(WallScript.Get().MinScale, transform.localScale.y);
        // Max scale (Y)
        if (transform.localScale.y >= WallScript.Get().MaxScale)
			transform.localScale = new Vector2(transform.localScale.x, WallScript.Get().MaxScale);
		if (transform.localScale.y <= WallScript.Get().MinScale)
			transform.localScale = new Vector2(transform.localScale.x, WallScript.Get().MinScale);

        // Get back to normal scale
        if (CurrentState != ObjectState.Transformed && CurrentState != ObjectState.Launched && Mathf.Abs(transform.localScale.x - destinationScale.x) > 0.01f)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, destinationScale, 0.01f);
        }
	}

	private void ControlRotation(){
        // Go back to normal rotation
		if (CurrentState != ObjectState.Transformed && CurrentState != ObjectState.Launched && transform.rotation != Quaternion.identity) {
			transform.rotation = Quaternion.Lerp (transform.rotation, Quaternion.identity, 0.05f);
		}		
	}

    private void ControlPosition()
    {
        // Goes out (X)
        if ((RectTransform.anchoredPosition.x < 0 && RigidBody2D.velocity.x < 0) || (RectTransform.anchoredPosition.x > wall.RectTransform.sizeDelta.x && RigidBody2D.velocity.x > 0))
        {
            RigidBody2D.AddForce(new Vector2(-1.5f * RigidBody2D.velocity.x, RigidBody2D.velocity.y) * 2, ForceMode2D.Impulse);
            if (isTriggered)
            {
                var pos = new Vector2(Random.Range(0, wall.Grid.NbCellsX), Random.Range(0, wall.Grid.NbCellsY));
                SetGridPosition(pos);
            }
        }
        // Goes out (Y)
        if ((RectTransform.anchoredPosition.y < 0 && RigidBody2D.velocity.y < 0) || (RectTransform.anchoredPosition.y > wall.RectTransform.sizeDelta.y && RigidBody2D.velocity.y > 0))
        {
            RigidBody2D.AddForce(new Vector2(RigidBody2D.velocity.x, -1.5f * RigidBody2D.velocity.y) * 2, ForceMode2D.Impulse);
            if (isTriggered)
            {
                var pos = new Vector2(Random.Range(0, wall.Grid.NbCellsX), Random.Range(0, wall.Grid.NbCellsY));
                SetGridPosition(pos);
            }
        }

        if (RigidBody2D.velocity.magnitude > WallScript.Get().MaxVelocity)
        {
            var vel = RigidBody2D.velocity;
            RigidBody2D.velocity = new Vector2(vel.x * 0.5f, vel.y * 0.5f);
        }
    }

    private void ControlColor()
    {
        /*var rgb = Image.color.r;
        if (Mathf.Abs(rgb - destinationColor) > 0.02f)
        {
            var diff = 0.01f;
            rgb = rgb > destinationColor ? rgb - diff : rgb + diff;
            Image.color = new Color(rgb, rgb, rgb, 1f);

            //TODO VIDEO
            if(ChildImage!=null)
                ChildImage.color = new Color(rgb, rgb, rgb, 1f);
        }*/
    }


    /*
     *  Utility methods
     */
    public void SetLayer(int layerNumber)
    {
		if (this.layer != layerNumber)
		{
            layer = layerNumber;
            if (layerNumber == 0)
                transform.SetAsLastSibling();

            var unityLayer = LayerMask.NameToLayer ("Layer" + layerNumber);
			gameObject.layer = unityLayer >= 0 ? unityLayer : wall.LayersCount + 7;
		    gameObject.name = GetObjectName();

            destinationScale = new Vector2(WallScript.Get().InitialScale - (layerNumber * WallScript.Get().LayersScaleFactor), WallScript.Get().InitialScale - (layerNumber * WallScript.Get().LayersScaleFactor));
            destinationColor = 1f - layer * WallScript.Get().LayersColorFactor;
		}

		var index = transform.GetSiblingIndex ();
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, -index*0.1f);
	}

    private string GetObjectName()
    {
        return draggableObjectName + "(" + CurrentState + " on layer " + layer + ")";
    }

    public void SetGridPosition(Vector2 pos)
	{
	    if (CurrentState != ObjectState.Idle)
	        return;

		destinationPosition = pos;
		CurrentState = ObjectState.Moving;
	}

    public void SetLastGridPosition()
    {
        SetGridPosition(destinationPosition);
    }

    public bool IsAttCell()
    {
        return wall.Grid.IsAtCell(RectTransform, destinationPosition);
    }

    public Vector2 GetCenter()
    {
        return new Vector2(RectTransform.position.x + RectTransform.rect.width, RectTransform.position.y + RectTransform.rect.height);
    }
}