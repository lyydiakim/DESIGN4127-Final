using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScriptableObject containing the key configuration data for a station.
/// Can be used as a data template to spawn or configure stations at runtime.
/// </summary>
[CreateAssetMenu(fileName = "New Station Data", menuName = "Game Assemblies/Stations/Station Data")]
public class StationDataSO : ScriptableObject
{
    [Header("Identity")]
    public string stationName = "New Station";
    public Sprite stationGraphic;
    [Tooltip("Tint color applied to the main station sprite. Use white for no tint.")]
    public Color stationSpriteTint = Color.white;
    [Tooltip("Overall scale of the station. Affects transform.localScale.")]
    [Range(0.1f, 3f)]
    public float stationScale = 0.5f;
    [Tooltip("Sprite shown when the station is inactive/dead. If null, stationGraphic is used.")]
    public Sprite deadSprite;
    [Tooltip("Reference to the prefab representing this station.")]
    public GameObject stationPrefab;
    [Tooltip("Vertical offset for the info panel pop-up. Higher values position the UI higher above the station.")]
    public float offsetY = 0.5f;
    [Tooltip("Scale factor for the station's Canvas Scaler. Controls the size of the info panel and progress UI.")]
    public float uiScale = 0.5f;
    [Tooltip("When true, the progress bar position is set manually relative to the station. When false, the slider uses its prefab layout.")]
    public bool manualSliderPosition = false;
    [Tooltip("Y offset for the progress bar when manual slider position is enabled. Negative values place it below the info panel.")]
    public float sliderOffsetY = -0.46f;

    [Header("Consume (IN) - Produce (OUT)")]
    public bool consumeResource;
    public bool produceResource;
    public List<Resource> consumes = new List<Resource>();
    public List<Resource> produces = new List<Resource>();
    [Tooltip("What this station outputs: Resource, Station, or LootTable.")]
    public Station.productionMode whatToProduce = Station.productionMode.Resource;
    [Tooltip("When whatToProduce is LootTable, this loot table defines the random output resources.")]
    public LootTable produceLootTable;
    [Tooltip("When true, spawns physical resource prefabs in the output area.")]
    public bool spawnResourcePrefab = true;
    [Tooltip("When true, the InputArea child object is enabled. Use for stations that accept physical resources.")]
    public bool useInputArea = true;
    [Tooltip("When true, the OutputArea child object is enabled. Use for stations that spawn resources.")]
    public bool useOutputArea = true;
    [Tooltip("Radius for random spawn offset when not using output area.")]
    public float spawnRadius = 1f;

    [Header("Lifespan")]
    public bool isSingleUse;
    public bool destroyAfterSingleUse = true;

    [Header("Capital")]
    public bool capitalInput;
    public bool capitalOutput;
    public int capitalInputAmount = 1;
    public int capitalOutputAmount = 1;

    [Header("Goals")]
    public bool completesGoals_consumption;
    public bool completesGoals_production;

    [Header("Timing & Interaction")]
    public bool canBeWorked = true;
    public float workDuration = 5f;
    public float productionInterval = 5f;
    public Station.interactionType typeOfProduction = Station.interactionType.whenWorked;
    public Station.interactionType typeOfConsumption = Station.interactionType.whenWorked;

    /// <summary>
    /// Applies this data to a Station component. Assigns this SO as the station's data source
    /// and performs one-time setup (input/output areas, sprite). The station reads all config
    /// from stationData after this call.
    /// </summary>
    public void ApplyToStation(Station station)
    {
        if (station == null) return;

        station.stationData = this;

        if (stationGraphic != null)
        {
            var sr = station.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.GetComponent<Canvas>() == null && sr.transform.GetComponentInParent<Canvas>() == null)
            {
                sr.sprite = stationGraphic;
                sr.color = stationSpriteTint;
            }
        }

        station.transform.localScale = new Vector3(stationScale, stationScale, 1f);
        station.offsetY = offsetY;

        var canvasScaler = station.GetComponentInChildren<CanvasScaler>();
        if (canvasScaler != null) canvasScaler.scaleFactor = uiScale;

        station.manualSliderPosition = manualSliderPosition;

        station.useInputArea = useInputArea;
        station.useOutputArea = useOutputArea;

        if (station.inputArea != null)
        {
            station.inputArea.requirements.Clear();
            foreach (var r in consumes)
                if (r != null) station.inputArea.requirements.Add(r);
            station.inputArea.gameObject.SetActive(useInputArea);
        }
        else
        {
            var inputAreaObj = FindChildRecursive(station.transform, "InputArea");
            if (inputAreaObj != null) inputAreaObj.gameObject.SetActive(useInputArea);
        }
        if (station.outputArea != null)
        {
            station.outputArea.gameObject.SetActive(useOutputArea);
        }
        else
        {
            var outputAreaObj = FindChildRecursive(station.transform, "OutputArea");
            if (outputAreaObj != null) outputAreaObj.gameObject.SetActive(useOutputArea);
        }

        station.produceLootTable = produceLootTable;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
