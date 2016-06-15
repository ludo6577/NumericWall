using System.Collections;
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
    public bool UnityCollisionSystem;

    [Range(0f, 1000f)]
	public float Inertia = 10f;

    [Range(0f, 1000f)]
    public float MaxVelocity;

    [Range(0f, 1f)]
	public float MinScale = 0.5f;

	[Range(1f, 5f)]
	public float MaxScale = 1.5f;

	[Range(0.1f, 5f)]
	public float MaxIdleTime;

    [Range(0.1f, 1f)]
    public float LayersScaleFactor = 0.2f;

    [Range(0.1f, 1f)]
    public float LayersColorFactor = 0.2f;

    private ObjectState currentState;
    protected ObjectState CurrentState
    {
        get { return currentState; }
        set
        {
            currentState = value;
            gameObject.name = GetObjectName();
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
        if (UnityCollisionSystem)
            collider.isTrigger = false;
        else
            collider.isTrigger = true;
    }

	public void Update(){
		if (wall == null)
			return;

        //Debug.Log(CurrentState);

        switch (CurrentState) {
			case ObjectState.Idle:
                RigidBody.isKinematic = false;
			break;

			case ObjectState.Moving:
                RigidBody.isKinematic = false;
				if (!wall.Grid.MoveToCell(RectTransform, destinationPosition))
					CurrentState = ObjectState.Idle;
			break;
                
			case ObjectState.Pressed: 
            case ObjectState.Transformed:
			break;

            case ObjectState.Launched:
            case ObjectState.Released:
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
                RigidBody.isKinematic = true;
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
                RigidBody.isKinematic = false;
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

        if (otherObj != null && RigidBody.velocity.magnitude < MaxVelocity &&
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
            
            RigidBody.AddForce(direction * power, ForceMode2D.Impulse);
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
		/*if (transform.localScale.x >= MaxScale)
			transform.localScale = new Vector2(MaxScale, transform.localScale.y);
		if (transform.localScale.x <= MinScale)
			transform.localScale = new Vector2(MinScale, transform.localScale.y);
        // Max scale (Y)
        if (transform.localScale.y >= MaxScale)
			transform.localScale = new Vector2(transform.localScale.x, MaxScale);
		if (transform.localScale.y <= MinScale)
			transform.localScale = new Vector2(transform.localScale.x, MinScale);*/

        // Get back to normal scale
        if (CurrentState != ObjectState.Transformed && CurrentState != ObjectState.Launched && Mathf.Abs(transform.localScale.x - destinationScale.x) > 0.01f)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, destinationScale, 0.02f);
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
        if ((RectTransform.anchoredPosition.x < 0 && RigidBody.velocity.x < 0) || (RectTransform.anchoredPosition.x > wall.RectTransform.sizeDelta.x && RigidBody.velocity.x > 0))
        {
            RigidBody.AddForce(new Vector2(-1.5f * RigidBody.velocity.x, RigidBody.velocity.y) * 2, ForceMode2D.Impulse);
            if (isTriggered)
            {
                var pos = new Vector2(Random.Range(0, wall.Grid.NbCellsX), Random.Range(0, wall.Grid.NbCellsY));
                SetGridPosition(pos);
            }
        }
        // Goes out (Y)
        if ((RectTransform.anchoredPosition.y < 0 && RigidBody.velocity.y < 0) || (RectTransform.anchoredPosition.y > wall.RectTransform.sizeDelta.y && RigidBody.velocity.y > 0))
        {
            RigidBody.AddForce(new Vector2(RigidBody.velocity.x, -1.5f * RigidBody.velocity.y) * 2, ForceMode2D.Impulse);
            if (isTriggered)
            {
                var pos = new Vector2(Random.Range(0, wall.Grid.NbCellsX), Random.Range(0, wall.Grid.NbCellsY));
                SetGridPosition(pos);
            }
        }

        if (RigidBody.velocity.magnitude > MaxVelocity)
        {
            var vel = RigidBody.velocity;
            RigidBody.velocity = new Vector2(vel.x * 0.5f, vel.y * 0.5f);
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

            destinationScale = new Vector2(1f - (layerNumber * LayersScaleFactor), 1f - (layerNumber * LayersScaleFactor));
            destinationColor = 1f - layer * LayersColorFactor;
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