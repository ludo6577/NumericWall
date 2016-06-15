using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GridLayout : MonoBehaviour {

	[Range(1, 100)]
	public int NbCellsX = 10;
	[Range(1, 100)]
	public int NbCellsY = 10;

	[Range(1, 300)]
	public float MargeX = 50f;
	[Range(1, 300)]
	public float MargeY = 50f;

	[Range(0.01f, 1000)]
	public float Speed = 10f;

	//Design
	public bool DrawGrid;
	public bool DrawMovement;
	public float LineWidth = 1;

	public List <Vector2[]> MovingLine;

	private WallScript Wall;


	void Start(){
		Wall = GetComponent<WallScript> ();
		MovingLine = new List <Vector2[]> ();
	}

	public float GetCellsWidth(){
		var screenSize = Wall.RectTransform.sizeDelta;
		return (screenSize.x - (MargeX*2)) / NbCellsX;
	}

	public float GetCellsHeight(){
		var screenSize = Wall.RectTransform.sizeDelta;
		return (screenSize.y - (MargeY*2)) / NbCellsY;
	}

	public Vector2 GetCellPosition(Vector2 gridCell){
		var cellWidth = GetCellsWidth ();
		var cellHeight = GetCellsHeight ();
		var posX = MargeX + ((gridCell.x + 1) * cellWidth) - (cellWidth / 2);
		var posY = MargeY + ((gridCell.y + 1) * cellHeight) - (cellHeight / 2);
		return new Vector2 (posX, posY);
	}

    public bool IsAtCell(RectTransform transform, Vector2 gridCell)
    {
        var destination = GetCellPosition(gridCell);
        if (Math.Abs(transform.anchoredPosition.x - destination.x) <= 10f &&
            Math.Abs(transform.anchoredPosition.y - destination.y) <= 10f)
        {
            return false;
        }
        return true;
    }

	public bool MoveToCell(RectTransform transform, Vector2 gridCell){
		var source = transform.anchoredPosition;
        var destination = GetCellPosition(gridCell);

        transform.anchoredPosition = Vector2.Lerp (source, destination, Speed * Time.deltaTime);

		if (DrawMovement) {
			MovingLine.Add (new Vector2[] {
				new Vector2 (source.x, source.y),
				new Vector2 (destination.x, destination.y)
			});
		}

	    return IsAtCell(transform, gridCell);
	}

	void FixedUpdate(){
		if(MovingLine!=null)
			MovingLine.Clear ();
	}

	void OnGUI(){
		var wallWidth = Wall.RectTransform.sizeDelta.x;
		var wallHeight = Wall.RectTransform.sizeDelta.y;
		var cellWidth = GetCellsWidth ();
		var cellHeight = GetCellsHeight ();

		var margeX2 = MargeX + (NbCellsX * cellWidth);
		var margeY2 = MargeY + (NbCellsY * cellHeight);

		if (DrawGrid) {
			// Verticals rows
			for (var x = MargeX; x <= margeX2 + 0.01f; x += cellWidth) {
				Drawing.DrawLine (CamToCanvas(new Vector2 (x, MargeY)), CamToCanvas(new Vector2 (x, margeY2)), Color.red, LineWidth, true);
			}

			// Horizontal
			for (var y = MargeY; y <= margeY2 + 0.01f; y += cellHeight) {
				Drawing.DrawLine (CamToCanvas(new Vector2 (MargeX, y)), CamToCanvas(new Vector2 (margeX2, y)), Color.red, LineWidth, true);
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
