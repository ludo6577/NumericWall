using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions.Must;
using Random = UnityEngine.Random;


public class WallScript : MonoBehaviour
{
    [Header("Prefabs")]
    public DraggableImageObject ObjectImagePrefab;
    public DraggableVideoObject ObjectVideoPrefab;

    [Header("Grid params")]
    public GridLayout Grid;

    [Header("Init params")]
    public int MaxImages;
    public int MaxVideos;

    [Header("Canvas params")]
    [Range(1, 7)]
    public int LayersCount;
    [Range(0, 10)]
    public float InitialVelocity;
    [Range(50, 1000)]
    public int InitialRange;
    [Range(1, 10000)]
    public int MovingProbabilty;

    [Header("Prefab params")]
    [Range(0f, 1000f)]
    public float Inertia = 50f;
    [Range(0f, 1000f)]
    public float MaxVelocity = 500f;
    [Range(0.1f, 5f)]
    public float MaxIdleTime = 1f;
    [Range(1f, 100f)]
    public float InitialScale = 1f;
    [Range(0f, 1f)]
    public float MinScale = 0.2f;
    [Range(1f, 5f)]
    public float MaxScale = 100f;
    [Range(0.1f, 1f)]
    public float LayersScaleFactor = 0.2f;
    [Range(0.1f, 1f)]
    public float LayersColorFactor = 0.2f;

    private static WallScript instance;
    public static WallScript Get()
    {
        return instance;
    }


    private RectTransform _rectTransform;
	public RectTransform RectTransform{
		get{ 
			if(_rectTransform == null)
				_rectTransform = GetComponent<RectTransform> ();
			return _rectTransform; 
		}
	}

    private static string ResourcesPath = "/../Resources/";
    private static string SchemaPath = "Schemas";
    private static string ImagesPath = "Pictures";
	private static string VideosPath = "Videos";
	private List<DraggableObject> draggableObjects;

    private Dictionary<int, Vector2> objectPosition;
    private int objCount;
    private int rowsCount;
    private int columnsCount;

    private bool initCompleted = false;

    void Awake()
    {
        instance = this;
        ResourcesPath = Application.dataPath + ResourcesPath; // Unity error if setted in class
        SchemaPath = ResourcesPath + SchemaPath;
        ImagesPath = ResourcesPath + ImagesPath;
        VideosPath = ResourcesPath + VideosPath;
    }
    
        
    IEnumerator Start ()
	{
	    ReadSchema();
	    if (objCount >= 0 && rowsCount > 0 && columnsCount > 0)
	    {
	        Grid.NbCellsX = columnsCount;
            Grid.NbCellsY = rowsCount;
	        if (MaxImages > objCount)
	            MaxImages = objCount;
            Debug.Log(string.Format("[Wall] Grid created: {0} rows, {1} columns", rowsCount, columnsCount));
        }

        var index = 0;
		draggableObjects = new List<DraggableObject> ();
        string[] fileEntries = Directory.GetFiles(ImagesPath);
        foreach (string fileName in fileEntries)
        {
            if (draggableObjects.Count >= MaxImages)
                break;
            if (!(fileName.EndsWith(".jpg", true, CultureInfo.InvariantCulture) || fileName.EndsWith(".png", true, CultureInfo.InvariantCulture)))
                continue;
            
            Vector2 pos;
            if (objectPosition.TryGetValue(index, out pos))
            {
                var obj = (DraggableImageObject) CreateNewObject(ObjectImagePrefab, "Image" + index, Grid.GetCellPosition(pos));
                obj.SetGridPosition(pos);
                WWW www = new WWW("file://" + fileName);
                yield return www;
                obj.SetImage(www.texture);
                draggableObjects.Add(obj);
            }
            else
            {
                Debug.LogError(string.Format("[Wall] Index {0} missing in Schema", index));
            }
            index++;
        }
        int imagesCount = draggableObjects.Count;


        #region VIDEO_TODO (https://issuetracker.unity3d.com/issues/movietexture-fmod-error-when-trying-to-play-video-using-www-class)
        //string[] fileEntries2 = Directory.GetFiles(VideosPath);
        //foreach (string fileName2 in fileEntries2)
        //{
        //    if (draggableObjects.Count - imagesCount >= MaxVideos)
        //        break;
        //    if (!fileName2.EndsWith(".ogg", true, CultureInfo.InvariantCulture))
        //        continue;

        //    Debug.Log(string.Format("Importing: {0}", fileName2));

        //    WWW www = new WWW("file://" + fileName2);
        //    yield return www;
        //    while (!www.isDone || !www.movie.isReadyToPlay)
        //        yield return www;

        //    //try
        //    //{
        //        var obj = CreateNewObject(ObjectVideoPrefab, "Video" + index++);
        //        ((DraggableVideoObject) obj).SetVideo(www.movie);
        //        draggableObjects.Add(obj);
        //        Debug.Log(string.Format("Object created: {0}", obj.name));
        //    //}
        //    //catch (Exception e)
        //    //{
        //    //    Debug.Log(string.Format("Import of: {0} failed", fileName2));
        //    //}
        //}
        #endregion


        this.UpdateLayers ();

        Debug.Log(string.Format("[Wall] Init completed: {0} object created.", draggableObjects.Count));
        initCompleted = true;
	}
    
	void Update(){
		this.UpdateMove ();
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
    }
    
    void ReadSchema()
    {
        string[] fileEntries = Directory.GetFiles(SchemaPath);
        if (fileEntries.Length==0)
            return;

        string text = File.ReadAllText(fileEntries[0]);
        var rows = text.Split('\n');
        if (rows.Length <= 0)
            return;

        objectPosition = new Dictionary<int, Vector2>();
        objCount = 0;
        var maxColumnsCount = 0;
        rowsCount = rows.Length-1; //last line empty
        for (var i = 0; i < rowsCount; i++)
        {
            var cells = rows[i].Split(';');
            columnsCount = cells.Length;
            if (columnsCount > maxColumnsCount)
                maxColumnsCount = columnsCount;

            for (var j = 0; j < columnsCount; j++)
            {
                int value;
                if (int.TryParse(cells[j], out value))
                {
                    //Debug.Log("val:" + value + ", x: " + i + ", y: " + (char) (j + 'a'));
                    if (objectPosition.ContainsKey(value))
                        Debug.LogError("This key already exists: " + value);
                    else
                        objectPosition.Add(value, new Vector2(j, i));
                    if (value+1 > objCount)
                    {
                        objCount = value+1;
                    }
                }
            }
        }
        columnsCount = maxColumnsCount;
        Debug.Log(string.Format("[Wall] Schema imported with {0} objects", objCount));
    }

    private DraggableObject CreateNewObject(DraggableObject prefab, string objectName, Vector2 initialPosition)
    {
        Vector2 center = new Vector2(RectTransform.sizeDelta.x / 2, RectTransform.sizeDelta.y / 2);
        var obj = (DraggableObject)Instantiate(prefab, initialPosition, prefab.transform.rotation);
        obj.Init(transform, this, objectName);
        return obj;
    }



    private DraggableObject CreateNewObject(DraggableObject prefab, string objectName)
    {
		// TODO: ugly, but... its a poc...
		float posX = 0f, posY = 0f;
        posX = Random.Range(0, RectTransform.sizeDelta.x);
        posY = Random.Range(0, RectTransform.sizeDelta.y);
        return CreateNewObject(prefab, objectName, new Vector2(posX, posY));
    }


	public void UpdateLayers(){
		foreach (var obj in draggableObjects) {
			var index = obj.transform.GetSiblingIndex ();
			var length = draggableObjects.Count;

			if(length <= 1)
				return;

			var layerIndex = (int)(LayersCount - (((float)index / (float)(length-1)) * LayersCount));
			layerIndex = (layerIndex <= 0 && index != length-1) ? 1 : layerIndex; //Only last element on layer 0
			obj.SetLayer (layerIndex);
		}
	}


	public void UpdateMove()
	{
	    if (!initCompleted)
	        return;

        if (Random.Range (0, MovingProbabilty) == 0) {
            var obj = draggableObjects [Random.Range (0, draggableObjects.Count - 1)];
            if (!obj.IsAttCell())
            {
                obj.SetLastGridPosition();
            }
        }
	}
}
