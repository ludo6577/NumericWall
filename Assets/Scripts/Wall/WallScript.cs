using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;


public class WallScript : MonoBehaviour {
	
	public DraggableImageObject ObjectImagePrefab;
	public DraggableVideoObject ObjectVideoPrefab;

	public GridLayout Grid;

	public int MaxImages;
    public int MaxVideos;

    [Range(1, 7)]
	public int LayersCount;
	[Range(0, 10)]
	public float InitialVelocity;
	[Range(50, 1000)]
	public int InitialRange;
    [Range(1, 10000)]
    public int MovingProbabilty;


    private RectTransform _rectTransform;
	public RectTransform RectTransform{
		get{ 
			if(_rectTransform == null)
				_rectTransform = GetComponent<RectTransform> ();
			return _rectTransform; 
		}
	}

    private static string SchemaPath = "Schema/SimplePlacement";
    private static string ImagesPath = "Pictures";
	private static string VideosPath = "Videos";
	private List<DraggableObject> draggableObjects;

    private Dictionary<int, Vector2> objectPosition;
    private int objCount;
    private int rowsCount;
    private int columnsCount;

    private bool initCompleted = false;

    IEnumerator Start ()
	{
	    ReadSchema();
	    if (objCount > 0 && rowsCount > 0 && columnsCount > 0)
	    {
	        Grid.NbCellsX = columnsCount;
            Grid.NbCellsY = rowsCount;
	        MaxImages = objCount;
	    }

        var index = 1;
		draggableObjects = new List<DraggableObject> ();
        var path = Application.dataPath + "/Resources/" + ImagesPath;
        string[] fileEntries = Directory.GetFiles(path);
        foreach (string fileName in fileEntries)
        {
            if (draggableObjects.Count >= MaxImages)
                break;
            if (fileName.EndsWith(".meta"))
                continue;

            WWW www = new WWW("file://" + fileName);
            yield return www;

            var obj = CreateNewObject(ObjectImagePrefab, "Image" + index++);
            ((DraggableImageObject)obj).SetImage(www.texture);
            draggableObjects.Add(obj);
        }

        //   Texture[] sprites = GetTexturesFromFolder(ImagesPath);
        //      foreach (var sprite in sprites) {
        //	if (draggableObjects.Count >= MaxImages)
        //		break;

        //          var obj = CreateNewObject (ObjectImagePrefab, "Image" + index++);
        //          ((DraggableImageObject) obj).SetImage (sprite);
        //	draggableObjects.Add (obj);
        //}

        //var imageCount = draggableObjects.Count;
        //MovieTexture[] movies = Resources.LoadAll<MovieTexture>(VideosPath);
        //foreach (var movie in movies)
        //{
        //    if (draggableObjects.Count - imageCount >= MaxVideos)
        //        break;

        //    var obj = CreateNewObject(ObjectVideoPrefab, "Video");
        //    ((DraggableVideoObject)obj).SetVideo(movie);
        //    draggableObjects.Add(obj);
        //}
        
        this.UpdateLayers ();
		this.UpdateMove (true);

        initCompleted = true;
	}
    
	void Update(){
		this.UpdateMove (false);
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
    }

    //Texture[] GetTexturesFromFolder(string imagesPath)
    //{
    //    List<Texture> textures = new List<Texture>();

    //    var path = Application.dataPath + "/Resources/" + imagesPath;
    //    string[] fileEntries = Directory.GetFiles(path);
    //    foreach (string fileName in fileEntries)
    //    {
    //        if (fileName.EndsWith(".meta"))
    //            continue;

    //        byte[] fileData = File.ReadAllBytes(fileName);
    //        Texture2D tex = new Texture2D(2, 2);
    //        tex.LoadImage(fileData);
    //        textures.Add(tex);
    //    }

    //    return textures.ToArray();
    //}


    //private WWW wwwData;
    //MovieTexture[] GetMoviesFromFolder(string videosPath)
    //{
    //    List<MovieTexture> textures = new List<MovieTexture>();

    //    var path = Application.dataPath + "/Resources/" + videosPath;
    //    string[] fileEntries = Directory.GetFiles(path);
    //    foreach (string fileName in fileEntries)
    //    {
    //        if (fileName.EndsWith(".meta"))
    //            continue;
            
    //        WWW wwwData = new WWW("file:///" + fileName);
    //        while (!wwwData.isDone) ;
    //        textures.Add(wwwData.movie);
    //    }

    //    return textures.ToArray();
    //}

    //private IEnumerator WaitForDownload(string sURL)
    //{
    //    WWW wwwData = new WWW("file:///" + sURL);
    //    yield return wwwData;
    //}

    /*
    IEnumerator loadMovie(string fileName, List<MovieTexture> textures)
    {
        WWW www = new WWW("file:///" + fileName);
        yield return www;
        MovieTexture video = www.movie as MovieTexture;
        textures.Add(video);
    }*/

    void ReadSchema()
    {
        TextAsset textFile = (TextAsset)Resources.Load<TextAsset>(SchemaPath);
        if (textFile == null)
            return;

        string text = textFile.text;
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
                    if (value > objCount)
                    {
                        objCount = value;
                    }
                }
            }
        }
        columnsCount = maxColumnsCount;
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
		while(posX>=0f && posX<=RectTransform.sizeDelta.x && posY>=0f && posY<=RectTransform.sizeDelta.y){
			posX = Random.Range(-InitialRange, RectTransform.sizeDelta.x + InitialRange);
			posY = Random.Range(-InitialRange, RectTransform.sizeDelta.y + InitialRange);
		}
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


	public void UpdateMove(bool updateAll)
	{
	    if (!draggableObjects.Any())
	        return;

        if (updateAll)
        {
            var index = -1;
            foreach (var obj in draggableObjects)
            {
                Vector2 pos;
                if (!objectPosition.TryGetValue(++index, out pos))
                {
                    Debug.LogError("This key is missing: " + index);
                    pos = new Vector2(Random.Range(0, Grid.NbCellsX), Random.Range(0, Grid.NbCellsY));
                }
                obj.SetGridPosition(pos);
            }
        }
        else if (Random.Range (0, MovingProbabilty) == 0) {
            //var pos = new Vector2 (Random.Range (0, Grid.NbCellsX), Random.Range (0, Grid.NbCellsY));
            var obj = draggableObjects [Random.Range (0, draggableObjects.Count - 1)];
            //if(!obj.IsAttCell())
                obj.SetLastGridPosition();
		}
	}
}
