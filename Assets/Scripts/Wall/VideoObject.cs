using UnityEngine;
using UnityEngine.UI;


public class VideoObject : MonoBehaviour {

    public Texture Texture;
    public void SetVideo(Texture texture)
    {
        this.Texture = Texture;
        var image = GetComponent<RawImage>();
        image.texture = texture;
    }
}
