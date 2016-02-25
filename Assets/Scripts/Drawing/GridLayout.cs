using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GridLayout : MonoBehaviour {

	[Range(1, 1000)]
	public float Width = 10f;
	[Range(1, 1000)]
	public float Height = 10f;
	[Range(0.01f, 1000)]
	public float Speed = 10f;

	[HideInInspector]
	public int GridCellX = 0;
	[HideInInspector]
	public int GridCellY = 0;

	public bool DrawGrid;
	public bool DrawMovement;

	public float LineWidth = 1;

	public new List <Vector2[]> MovingLine;

	private WallScript Wall;

	void Start(){
		Wall = GetComponent<WallScript> ();
		MovingLine = new List <Vector2[]> ();

		GridCellX = (int)(Wall.RectTransform.sizeDelta.x / Width);
		GridCellY = (int)(Wall.RectTransform.sizeDelta.y / Height);
	}


	public bool MoveToCell(RectTransform transform, Vector2 gridCell){
		var posX = (gridCell.x * Width) - (Width / 2);
		var posY = (gridCell.y * Height) - (Height / 2);
		var source = transform.anchoredPosition;
		var destination = new Vector2 (posX, posY);

		transform.anchoredPosition = Vector2.Lerp (source, destination, Speed * Time.deltaTime);

		if (DrawMovement) {
			MovingLine.Add (new Vector2[] {
				new Vector2 (source.x, source.y),
				new Vector2 (destination.x, destination.y)
			});
		}

		if (transform.anchoredPosition.x >= destination.x - 0.01f &&
			transform.anchoredPosition.x <= destination.x + 0.01f &&
			transform.anchoredPosition.y >= destination.y - 0.01f &&
			transform.anchoredPosition.y <= destination.y + 0.01f) {
			return false;
		}
		return true;
	}

	void FixedUpdate(){
		if(MovingLine!=null)
			MovingLine.Clear ();
	}

	void OnGUI(){
		var wallWidth = Wall.RectTransform.sizeDelta.x;
		var wallHeight = Wall.RectTransform.sizeDelta.y;

		if (DrawGrid) {
			// Verticals rows
			for (var x = 0f; x < wallWidth; x += Width) {
				Drawing.DrawLine (CamToCanvas(new Vector2 (x, 0)), CamToCanvas(new Vector2 (x, wallWidth)), Color.red, LineWidth, true);
			}

			// Horizontal
			for (var y = 0f; y < wallHeight; y += Height) {
				Drawing.DrawLine (CamToCanvas(new Vector2 (0, y)), CamToCanvas(new Vector2 (wallWidth, y)), Color.red, LineWidth, true);
			}
		}

		if (DrawMovement) {
			// Moving lines
			foreach(var line in MovingLine){				
				Drawing.DrawLine (CamToCanvas(line[0]), CamToCanvas(line[1]), Color.green, LineWidth, true);
			}
		}
	}

	private Vector2 CamToCanvas(Vector2 pos){
		var camSize = Camera.main.pixelRect.size;
		var canvasSize = Wall.RectTransform.sizeDelta;
		var ratioX = camSize.x / canvasSize.x;
		var ratioY = camSize.y / canvasSize.y;
		return new Vector2 (pos.x * ratioX, Camera.main.pixelRect.size.y -pos.y * ratioY);
	}
}
