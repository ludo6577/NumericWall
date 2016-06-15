using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageObject : MonoBehaviour {

	public Texture Texture;

    public float marge;

    public void SetImage(Texture texture)
    {
		this.Texture = Texture;
        var image = GetComponent<RawImage>();
        image.texture = texture;
    }
    
}
