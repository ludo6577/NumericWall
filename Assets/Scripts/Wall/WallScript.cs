using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class WallScript : MonoBehaviour
{
    [Header("Prefabs")]
    public DraggableImageObject ObjectImagePrefab;
    public DraggableVideoObject ObjectVideoPrefab;

    [Header("Draggable object parts")]
    public GameObject Parts;

    [Header("Grid params")]
    public GridLayout Grid;

    [Header("Information panel")]
    public GameObject InformationText;

    //[Header("Init params")]
    //public int MaxImages;
    //public int MaxVideos;

    [Header("Canvas params")]
    [Range(1, 7)]
    public int LayersCount;
    [Range(0, 10)]
    public float InitialVelocity;
    [Range(50, 1000)]
    public int InitialRange;
    [Range(1, 10000)]
    public int MovingProbabilty;
    [Range(1, 10000)]
    public int ChangeLayerProbabilty;

    [Header("Prefab params")]
    [Range(0f, 100f)]
    public float Force = 4f;
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

    private RectTransform _rectTransform;
	public RectTransform RectTransform{
		get{ 
			if(_rectTransform == null)
				_rectTransform = GetComponent<RectTransform> ();
			return _rectTransform; 
		}
	}

    public int ObjectCount
    {
        get { return draggableObjects.Count; }
    }

    private static string ResourcesPath = "/../Resources/";
    private static string SchemaPath = "Schemas";
    private static string ImagesPath = "Pictures";
	private static string VideosPath = "Videos";
	private List<DraggableObject> draggableObjects;

    private Dictionary<int, Vector2> objectPosition;

    private int imageCount;
    private int videoCount;

    private bool initCompleted = false;

    void Awake()
    {
        draggableObjects = new List<DraggableObject>();
        ResourcesPath = Application.dataPath + ResourcesPath; // Unity error if setted in class
        SchemaPath = ResourcesPath + SchemaPath;
        ImagesPath = ResourcesPath + ImagesPath;
        VideosPath = ResourcesPath + VideosPath;
    }


    IEnumerator Start ()
	{
        //Create the Dictionnary of positions and the Grid Width/Height 
	    ReadSchema();
		
        yield return LoadImages();
        yield return LoadVideos();

        this.UpdateLayers ();
        initCompleted = true;

        ShowInformation(string.Format("[Wall Init] completed: total of {0} object created.", draggableObjects.Count));
    }

    IEnumerator LoadImages()
    {
        var index = 0;
        string[] fileEntries = Directory.GetFiles(ImagesPath);

        //Atlas test
        //List<Texture2D> atlasTextures = new List<Texture2D>();

        foreach (string fileName in fileEntries)
        {
            if (!(fileName.EndsWith(".jpg", true, CultureInfo.InvariantCulture) || fileName.EndsWith(".png", true, CultureInfo.InvariantCulture)))
                continue;

            Vector2 pos;
            if (objectPosition.TryGetValue(index, out pos))
            {
                var obj = (DraggableImageObject)CreateNewObject(ObjectImagePrefab, "Image" + index, Grid.GetCellPosition(pos));
                obj.SetGridPosition(pos);
                WWW www = new WWW("file://" + fileName);
                yield return www;

                //atlasTextures.Add(www.texture);

                obj.SetImage(www.texture);
                draggableObjects.Add(obj);
            }
            else
            {
                ShowError(string.Format("[Load Image] Index {0} missing in Schema", index));
            }
            index++;
        }

        //Texture2D atlas = new Texture2D(8192, 8192);
        //var rects = atlas.PackTextures(atlasTextures.ToArray(), 2, 8192);

        imageCount = index;
        ShowInformation(string.Format("[Load Image] {0} Images imported.", imageCount));
    }

    IEnumerator LoadVideos()
    {
        var index = imageCount;
        string[] fileEntries2 = Directory.GetFiles(VideosPath);

        foreach (string fileName2 in fileEntries2)
        {
            if (!fileName2.EndsWith(".ogg", true, CultureInfo.InvariantCulture))
                continue;
            
            Vector2 pos;
            if (objectPosition.TryGetValue(index, out pos))
            {
                var obj = CreateNewObject(ObjectVideoPrefab, "Video" + index, Grid.GetCellPosition(pos));
                obj.SetGridPosition(pos);
                WWW www = new WWW("file://" + fileName2);
                while (!www.isDone || !www.movie.isReadyToPlay)
                    yield return www;
                ((DraggableVideoObject) obj).SetVideo(www.movie);
                draggableObjects.Add(obj);
            }
            else
            {
                ShowError(string.Format("[Load Video] Index {0} missing in Schema", index));
            }
            index++;
        }

        videoCount = index - imageCount;
        ShowInformation(string.Format("[Load Video] {0} Video imported.", videoCount));
    }

    void ReadSchema()
    {
        try
        {
            string[] fileEntries = Directory.GetFiles(SchemaPath);
            if (fileEntries.Length == 0)
                return;

            string text = File.ReadAllText(fileEntries[0]);
            var rows = text.Split('\n');
            if (rows.Length <= 0)
                return;

            objectPosition = new Dictionary<int, Vector2>();
            var objCount = 0;
            var columnsCount = 0;
            var rowsCount = rows.Length - 1; //last line empty
            for (var i = 0; i < rowsCount; i++)
            {
                var cells = rows[i].Split(';');
                if (cells.Length > columnsCount)
                    columnsCount = cells.Length;

                for (var j = 0; j < cells.Length; j++)
                {
                    int value;
                    if (int.TryParse(cells[j], out value))
                    {
                        if (objectPosition.ContainsKey(value))
                        {
                            var msg = "[Schema] Key already exists: " + value;
                            Debug.LogError(msg);
                            ShowError(msg);
                        }
                        else
                            objectPosition.Add(value, new Vector2(j, i));
                        if (value + 1 > objCount)
                        {
                            objCount = value + 1;
                        }
                    }
                }
            }

            Grid.NbCellsX = columnsCount;
            Grid.NbCellsY = rowsCount;
            ShowInformation(string.Format("[Load Schema] {0} rows, {1} columns", rowsCount, columnsCount));
            ShowInformation(string.Format("[Load Schema] {0} objects", objCount));
        }
        catch (Exception e)
        {
            ShowError(string.Format("[Load Schema] Error reading file: {0}. Does this file exist and not opened in another application?", SchemaPath));
        }
    }

    private DraggableObject CreateNewObject(DraggableObject prefab, string objectName, Vector2 initialPosition)
    {
        Vector2 center = new Vector2(RectTransform.sizeDelta.x / 2, RectTransform.sizeDelta.y / 2);
        var obj = (DraggableObject)Instantiate(prefab, initialPosition, prefab.transform.rotation);
        obj.Init(Parts.transform, this, objectName);
        return obj;
    }

    void Update()
    {
        this.UpdateMove();
        // Exit the application
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();
    }

    public void UpdateLayers(){
		foreach (var obj in draggableObjects) {
			var index = obj.transform.GetSiblingIndex ();
			var length = draggableObjects.Count;

			if(length <= 1)
				return;

			var layerIndex = LayersCount - (((float)index / (float)(length-1)) * (float)LayersCount);
			//layerIndex = (layerIndex <= 0 && index != length-1) ? 1 : layerIndex; //Only last element on layer 0
			obj.SetLayer ((int)layerIndex);
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

        if (Random.Range(0, ChangeLayerProbabilty) == 0)
        {
            var obj = draggableObjects[Random.Range(0, draggableObjects.Count - 1)];
            obj.SetLayer(0);
            UpdateLayers();
        }
    }
    

    public void ShowError(string message)
    {
        Debug.LogError(message);
        ShowMessage(message, "#ff0000ff");   
    }

    public void ShowInformation(string message)
    {
        Debug.Log(message);
        ShowMessage(message, "#ffffffff");
    }


    private IEnumerator closeInformationPanel = null;
    private void ShowMessage(string message, string color)
    {
        InformationText.gameObject.SetActive(true);
        var text = InformationText.GetComponentInChildren<Text>();
        text.text += string.Format("<color={0}>{1}</color>{2}", color, message, Environment.NewLine);

        if (closeInformationPanel != null)
            StopCoroutine(closeInformationPanel);
        closeInformationPanel = CloseInformationPanel();
        StartCoroutine(closeInformationPanel);
    }
    
    IEnumerator CloseInformationPanel()
    {
        yield return new WaitForSeconds(10f);

        var text = InformationText.GetComponentInChildren<Text>();
        text.text = "";
        InformationText.gameObject.SetActive(false);
        closeInformationPanel = null;
    }
}
