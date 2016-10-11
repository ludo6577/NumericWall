using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageObject : MonoBehaviour {

    [HideInInspector]
	public Texture Texture;
    
    public void SetImage(Texture texture)
    {
		this.Texture = texture;
        var image = GetComponent<RawImage>();
        image.texture = texture;
    }
}
