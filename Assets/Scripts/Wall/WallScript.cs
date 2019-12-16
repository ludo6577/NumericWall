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
    private static string ResourcesPath = "/../Resources/";
    private static string SchemaPath = "Schemas";
    private static string ImagesPath = "Pictures";
    private static string VideosPath = "Videos";

    [Header("Prefabs")]
    public DraggableImageObject ObjectImagePrefab;
    public DraggableVideoObject ObjectVideoPrefab;

    [Header("Draggable object parts")]
    public GameObject Parts;

    [Header("Grid params")]
    public GridLayout Grid;
    
    [Header("Information panel")]
    public GameObject InformationText;
    
    [Header("Images Atlas params")]
    public int MaximumAtlasSize = 8192;
    public float AtlasMargin = 0.001f;
    [Tooltip("Use this Image to show the atlas in editor")]
    public RawImage DebugAtlas;

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

    private RectTransform rectTransform;
	public RectTransform RectTransform{
		get{ 
			if(rectTransform == null)
				rectTransform = GetComponent<RectTransform> ();
			return rectTransform; 
		}
	}

	private List<DraggableObject> draggableObjects;

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
        Dictionary<int, Vector2> objectsPositions = ReadSchema();
		
        yield return LoadImages(objectsPositions);
        yield return LoadVideos(objectsPositions);

        UpdateLayers ();
        initCompleted = true;

        ShowInformation(string.Format("[Wall Init] completed: total of {0} object created.", draggableObjects.Count));
    }

    void Update()
    { 
        // Exit the application
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();

        if (!initCompleted)
            return;

        /*
         * Make the wall a little more interactive
         */
        // 1) Randomly Move images to initial position
        if (Random.Range(0, MovingProbabilty) == 0)
        {
            var obj = draggableObjects[Random.Range(0, draggableObjects.Count - 1)];
            if (!obj.IsAttCell())
            {
                obj.SetLastGridPosition();
            }
        }
        // 2) Randomly change the layer of each image
        // TODO: Layer system do not works ! The simblingIndex is not updated (collision between layers is setted in the Unity's Physics options)
        //if (Random.Range(0, ChangeLayerProbabilty) == 0)
        //{
        //    var obj = draggableObjects[Random.Range(0, draggableObjects.Count - 1)];
        //    obj.SetLayer(0);
        //    UpdateLayers();
        //}
    }
    
    /*
     * Update the layers index of each images
     * (Images on same Layers collides with each others)
     */
    public void UpdateLayers()
    {
        foreach (var obj in draggableObjects)
        {
            var length = draggableObjects.Count;
            if (length <= 0)
                return;

            var index = obj.transform.GetSiblingIndex();
            var layerIndex = LayersCount - (((float)(index + 1) / (float)length) * (float)LayersCount);
            obj.SetLayer((int)layerIndex);
        }
    }
    



    /*
     * Read the schema
     */
    private Dictionary<int, Vector2> ReadSchema()
    {
        try
        {
            string[] fileEntries = Directory.GetFiles(SchemaPath);
            if (fileEntries.Length == 0)
                return null;

            string text = File.ReadAllText(fileEntries[0]);
            var rows = text.Split('\n');
            if (rows.Length <= 0)
                return null;

            var objectsPositions = new Dictionary<int, Vector2>();
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
                        if (objectsPositions.ContainsKey(value))
                        {
                            var msg = "[Schema] Key already exists: " + value;
                            Debug.LogError(msg);
                            ShowError(msg);
                        }
                        else
                            objectsPositions.Add(value, new Vector2(j, i));
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

            return objectsPositions;
        }
        catch (Exception e)
        {
            ShowError(string.Format("[Load Schema] Error reading file: {0}. Does this file exist and not opened in another application?", SchemaPath));
            return null;
        }
    }

    /*
     *  Load the images from files and create an atlas from them to significally improve performance
     */
    private IEnumerator LoadImages(Dictionary<int, Vector2> objectsPositions)
    {
        var index = 0;
        string[] fileEntries = Directory.GetFiles(ImagesPath);

        //Atlas
        List<Texture2D> textures = new List<Texture2D>();

        foreach (string fileName in fileEntries)
        {
            if (!(fileName.EndsWith(".jpg", true, CultureInfo.InvariantCulture) || fileName.EndsWith(".png", true, CultureInfo.InvariantCulture)))
                continue;

            Vector2 pos;
            if (objectsPositions.TryGetValue(index, out pos))
            {
                // Create the draggable object
                var obj = (DraggableImageObject)CreateNewObject(ObjectImagePrefab, "Image" + index, Grid.GetCellPosition(pos));
                obj.SetGridPosition(pos);
                draggableObjects.Add(obj);
                
                // Get texture from file
                WWW www = new WWW("file://" + fileName);
                yield return www;
                textures.Add(www.texture);
            }
            else
            {
                ShowError(string.Format("[Load Image] Index {0} missing in Schema", index));
            }
            index++;
        }

        createImagesAtlas(textures);

        imageCount = index;
        ShowInformation(string.Format("[Load Image] {0} Images imported.", imageCount));
    }

    /* 
     * Create an Atlas (improve performance, only one drawcall needed to show all images!)
     */
    private void createImagesAtlas(List<Texture2D> textures)
    {
        // Create the Atlas
        Texture2D atlas = new Texture2D(MaximumAtlasSize, MaximumAtlasSize);
        var rects = atlas.PackTextures(textures.ToArray(), 10, MaximumAtlasSize);

        // Draw white margin (instead of transparent)
        for (var i = 0; i < atlas.width; i++)
        {
            for (var j = 0; j < atlas.height; j++)
            {
                var pixel = atlas.GetPixel(i, j);
                if (pixel == new Color(pixel.r, pixel.g, pixel.b, 0f)) //Transparent?
                    atlas.SetPixel(i, j, Color.white);
            }
        }
        atlas.Apply();

        // Used to see the Atlas
        if (DebugAtlas != null && DebugAtlas.IsActive())
        {
#if UNITY_EDITOR
            DebugAtlas.texture = atlas;
#else
            DebugAtlas.gameObject.SetActive(false);
#endif
        }

        // Then apply the atlas texture to 
        for (var i = 0; i < draggableObjects.Count; i++)
        {
            ((DraggableImageObject)draggableObjects[i]).SetImage(atlas, new Rect(rects[i].x - AtlasMargin, rects[i].y - AtlasMargin, rects[i].width + AtlasMargin * 2, rects[i].height + AtlasMargin * 2));
        }
    }

    /*
     * Load the video from files (no performances improvment, can't do atlas from them)
     */
    private IEnumerator LoadVideos(Dictionary<int, Vector2> objectsPositions)
    {
        var index = imageCount;
        string[] fileEntries2 = Directory.GetFiles(VideosPath);

        foreach (string fileName2 in fileEntries2)
        {
            if (!fileName2.EndsWith(".ogg", true, CultureInfo.InvariantCulture))
                continue;
            
            Vector2 pos;
            if (objectsPositions.TryGetValue(index, out pos))
            {
                // Create the draggable object
                var obj = CreateNewObject(ObjectVideoPrefab, "Video" + index, Grid.GetCellPosition(pos));
                obj.SetGridPosition(pos);
                draggableObjects.Add(obj);

                // Get the video from file
                WWW www = new WWW("file://" + fileName2);
                while (!www.isDone || !www.GetMovieTexture().isReadyToPlay)
                    yield return www;
                ((DraggableVideoObject) obj).SetVideo(www.GetMovieTexture());
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
    
    /*
     *  Instantiate a the Draggable base object
     */
    private DraggableObject CreateNewObject(DraggableObject prefab, string objectName, Vector2 initialPosition)
    {
        Vector2 center = new Vector2(RectTransform.sizeDelta.x / 2, RectTransform.sizeDelta.y / 2);
        var obj = (DraggableObject)Instantiate(prefab, initialPosition, prefab.transform.rotation);
        obj.Init(Parts.transform, this, objectName);
        return obj;
    }

    
    /*
     *  Show messages on the wall (Errors, Informations, ...)
     */
    public void ShowError(string message)
    {
//#if UNITY_EDITOR
        Debug.LogError(message);
        ShowMessage(message, "#ff0000ff");
//#endif
    }

    public void ShowInformation(string message)
    {
#if UNITY_EDITOR
        Debug.Log(message);
        ShowMessage(message, "#ffffffff");
#endif
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
    
    private IEnumerator CloseInformationPanel()
    {
        yield return new WaitForSeconds(10f);

        var text = InformationText.GetComponentInChildren<Text>();
        text.text = "";
        InformationText.gameObject.SetActive(false);
        closeInformationPanel = null;
    }
}
