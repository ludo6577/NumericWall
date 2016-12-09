using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TouchScript;
using TouchScript.Gestures;
using TouchScript.Utils;

public class DraggableImageObject : DraggableObject
{
    public virtual void SetImage(Texture2D sprite, Rect rect)
    {
        var image = GetComponent<RawImage>();
        image.texture = sprite;
        image.uvRect = rect;

        var collider = GetComponent<BoxCollider2D>();

        var ratioX = (float)rect.width / rect.height;
        var ratioY = (float)rect.height / rect.width;
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
}