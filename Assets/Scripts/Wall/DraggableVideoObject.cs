using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TouchScript;
using TouchScript.Gestures;
using TouchScript.Utils;


public class DraggableVideoObject : DraggableImageObject
{
	private MovieTexture movie;
    private Image playButton;

    public void SetVideo(MovieTexture texture){
        StartCoroutine(DoSetVideo(texture));
	}

    IEnumerator DoSetVideo(MovieTexture texture)
    {
        //We need the video to play before we can get his size
        while (!texture.isPlaying)
        {
            texture.Play();
            yield return null;
        }
        texture.Stop();

        movie = texture;
        movie.loop = true;
        var videoObject = GetComponentInChildren<VideoObject>();
        videoObject.SetVideo(texture);

        playButton = GetComponentInChildren<Image>();

        var collider = GetComponent<BoxCollider2D>();
        var ratioX = (float)texture.width / texture.height;
        var ratioY = (float)texture.height / texture.width;
        if (ratioX > 1)
        {
            var size = new Vector2(RectTransform.sizeDelta.x * ratioX, RectTransform.sizeDelta.y);
            RectTransform.sizeDelta = size;
            collider.size = size;
        }
        else
        {
            var size = new Vector2(RectTransform.sizeDelta.x, RectTransform.sizeDelta.y * ratioY);
            RectTransform.sizeDelta = size;
            collider.size = size;
        }
    }

    new void OnEnable()
	{
		base.OnEnable ();
		GetComponent<TapGesture>().Tapped += tappedHandler;
	}

    new void OnDisable()
	{
		base.OnDisable ();
		GetComponent<TapGesture>().Tapped -= tappedHandler;
	}

	private void tappedHandler(object sender, EventArgs e){
		if (movie.isPlaying) {
            playButton.gameObject.SetActive(true);
            movie.Pause();
		}
		else {
            playButton.gameObject.SetActive(false);
            movie.Play();
		}
	}
}