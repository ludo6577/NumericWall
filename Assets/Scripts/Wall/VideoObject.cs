using UnityEngine;
using UnityEngine.UI;


public class VideoObject : MonoBehaviour {

    [HideInInspector]
    public Texture Texture;
    public void SetVideo(Texture texture)
    {
        this.Texture = texture;
        var image = GetComponent<RawImage>();
        image.texture = texture;
    }
}
