using UnityEngine;


public class VideoObject : MonoBehaviour {

	public float marge;

	[HideInInspector]
	public MovieTexture Texture;

	private Renderer renderer;

	void Start(){
	}

	public void SetVideo(MovieTexture texture){
		this.Texture = texture;
		renderer = GetComponent<Renderer> ();
		renderer.material.mainTexture = texture;
	}

	public void SetSize(Vector2 size){
		var plane = GetComponent<MeshFilter> ();
		var currentSize = plane.mesh.bounds.size;
		transform.localScale = new Vector3((size.x - marge) / currentSize.x, 1, (size.y - marge) / currentSize.z);
	} 
}
