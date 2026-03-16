using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// A didactic Station Builder window that guides users through configuring a station.
/// Centers the station graphic and offers options for key variables with explanatory tooltips.
/// </summary>
public class SA_StationBuilderWindow : EditorWindow
{
    private const string DefaultTemplatePrefabPath = "Samples/Prefabs/Stations/station_template.prefab";

    private GameObject stationPrefabTemplate;

    // Station identity
    private bool showAdvancedOptions;
    private string stationName = "New Station";
    private Sprite stationGraphic;
    private Sprite deadSprite;
    private Color stationSpriteTint = Color.white;
    private float stationScale = 0.5f;
    private float uiOffsetY = 0.5f;
    private float uiScale = 0.5f;
    private bool manualSliderPosition = false;
    private float sliderOffsetY = -0.46f;

    // Resource consumption (list)
    private bool consumeResource;
    private List<Resource> consumeResources = new List<Resource>();

    // Resource production (list)
    private bool produceResource;
    private bool produceFromLootTable;
    private LootTable produceLootTable;
    private List<Resource> produceResources = new List<Resource>();

    // Lifespan
    private bool isSingleUse;
    private bool destroyAfterSingleUse;

    // Capital
    private bool produceCapital;
    private int capitalOutputAmount = 1;
    private bool consumeCapital;
    private int capitalInputAmount = 1;

    // Goals
    private bool consumptionCompletesGoals;
    private bool productionCompletesGoals;

    // Input/Output areas & spawn
    private bool useInputArea = true;
    private bool useOutputArea = true;
    private float spawnRadius = 1f;

    // Timing & interaction
    private float productionInterval = 5f;
    private float workDuration = 5f;
    private Station.interactionType typeOfProduction = Station.interactionType.whenWorked;
    private Station.interactionType typeOfConsumption = Station.interactionType.whenWorked;
    private bool canBeWorked = true;

    // Preview
    private Rect previewRect;
    private Vector2 scrollPosition;

    // Tooltips
    private static readonly GUIContent ConsumeResourceTip = new GUIContent(
        "Consume Resources",
        "When enabled, this station requires physical resources (e.g., wood, ore) to be placed in its input area before it can operate. " +
        "Players must bring the required resource(s) into the station's input zone. This creates a conversion or crafting flow.");
    private static readonly GUIContent ConsumeResourceSlotTip = new GUIContent(
        "Resource",
        "A resource this station accepts as input. Add multiple resources for stations that require several inputs.");
    private static readonly GUIContent ProduceResourceTip = new GUIContent(
        "Produce Resources",
        "When enabled, this station creates and outputs physical resources (e.g., planks, ingots). " +
        "The produced resource spawns in the output area and can be picked up by players or used by other stations.");
    private static readonly GUIContent ProduceResourceSlotTip = new GUIContent(
        "Resource",
        "A resource this station creates. Add multiple resources for stations that produce several outputs.");
    private static readonly GUIContent ProduceFromLootTableTip = new GUIContent(
        "Produce from Loot Table",
        "When enabled, output is chosen randomly from a loot table instead of fixed resources. Use for varied or random drops.");
    private static readonly GUIContent SingleUseTip = new GUIContent(
        "Single-Use Station",
        "When enabled, the station operates only once and then becomes inactive (or is destroyed). " +
        "Useful for one-time conversions, extractors that deplete, or consumable stations.");
    private static readonly GUIContent DestroyAfterSingleUseTip = new GUIContent(
        "Destroy After Single Use",
        "When enabled, the station is removed from the scene after its first use. When disabled, it stays visible with the dead sprite.");
    private static readonly GUIContent UIOffsetYTip = new GUIContent(
        "UI Offset Y",
        "Determines how high the info panel pop-up will appear above the station when inspected.");
    private static readonly GUIContent UIScaleTip = new GUIContent(
        "UI Scale",
        "Scale factor for the station's Canvas Scaler. Controls the size of the info panel and progress UI.");
    private static readonly GUIContent ManualSliderPositionTip = new GUIContent(
        "Manual Slider Position",
        "When enabled, the progress bar position is set manually relative to the station. When disabled, the slider uses its prefab layout.");
    private static readonly GUIContent SliderOffsetYTip = new GUIContent(
        "Slider Offset Y",
        "Y offset for the progress bar when manual slider position is enabled. Negative values place it below the info panel.");
    private static readonly GUIContent ProduceCapitalTip = new GUIContent(
        "Produces Capital",
        "When enabled, this station adds points (capital) to the global score when it completes a cycle. " +
        "Capital represents economic value and can drive goals, purchases, or win conditions.");
    private static readonly GUIContent CapitalOutputAmountTip = new GUIContent(
        "Capital Amount",
        "How many points are added to the global score each time this station completes a production cycle.");
    private static readonly GUIContent ConsumeCapitalTip = new GUIContent(
        "Consumes Capital",
        "When enabled, this station requires capital (points) to be spent from the global pool or worker when it operates. " +
        "Use for paid services, upgrades, or gated stations.");
    private static readonly GUIContent CapitalInputAmountTip = new GUIContent(
        "Capital Cost",
        "How many points are deducted when this station completes a consumption/production cycle.");
    private static readonly GUIContent ConsumptionCompletesGoalsTip = new GUIContent(
        "Consumption Completes Goals",
        "When enabled, consuming the required resource in this station counts toward active goals. " +
        "Use when players must deliver specific resources to stations to meet level objectives.");
    private static readonly GUIContent ProductionCompletesGoalsTip = new GUIContent(
        "Production Completes Goals",
        "When enabled, producing the output resource counts toward active goals. " +
        "Use when players must create specific resources to meet level objectives.");
    private static readonly GUIContent ProductionIntervalTip = new GUIContent(
        "Production Interval (seconds)",
        "For automatic stations: time between production cycles. For worked stations: not used.");
    private static readonly GUIContent WorkDurationTip = new GUIContent(
        "Work Duration (seconds)",
        "How long a player must work at this station (hold/interact) before the cycle completes. " +
        "Longer durations create more strategic timing and coordination.");
    private static readonly GUIContent CanBeWorkedTip = new GUIContent(
        "Requires Player Work",
        "When enabled, a player must interact with the station to trigger consumption/production. " +
        "When disabled, the station operates automatically (if configured for automatic mode).");
    private static readonly GUIContent UseInputAreaTip = new GUIContent(
        "Use Input Area",
        "When enabled, the InputArea child object is active. Players place resources here for stations that consume them.");
    private static readonly GUIContent UseOutputAreaTip = new GUIContent(
        "Use Output Area",
        "When enabled, the OutputArea child object is active. Produced resources spawn here when the station outputs.");
    private static readonly GUIContent SpawnRadiusTip = new GUIContent(
        "Spawn Radius",
        "When not using the output area, resources spawn within this radius around the station. Used for random spawn offset.");

    private const float FlowColumnWidth = 300f;
    private const float StationPreviewSize = 100f;
    private const float ArrowWidth = 36f;

    private enum StationTemplate
    {
        None,
        AutomaticStation,
        ConvertOnWork,
        OutputBox,
        SingleExtract
    }
    private StationTemplate selectedTemplate = StationTemplate.None;

    [MenuItem("Game Assemblies/Stations/Station Builder")]
    public static void ShowWindow()
    {
        var window = GetWindow<SA_StationBuilderWindow>("Station Builder");
        window.minSize = new Vector2(700, 520);
    }

    private void OnEnable()
    {
        if (stationPrefabTemplate == null)
        {
            stationPrefabTemplate = SA_AssetPathHelper.FindPrefab(DefaultTemplatePrefabPath);
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        DrawTemplates();
        DrawFlowDiagram();
        DrawGeneralSettings();
        DrawCreateButton();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(4);
        GUILayout.Label("Station Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Configure your station as a flow: inputs on the left, station in the center, outputs on the right. " +
            "Hover over labels for detailed explanations.",
            MessageType.Info);
        EditorGUILayout.Space(2);
        showAdvancedOptions = EditorGUILayout.Toggle("Show advanced options", showAdvancedOptions);
        EditorGUILayout.Space(2);
        stationName = EditorGUILayout.TextField("Station Name", stationName);
        stationSpriteTint = EditorGUILayout.ColorField(new GUIContent("Sprite Tint", "Tint color for the main station sprite. Use white for no tint."), stationSpriteTint);
        stationScale = EditorGUILayout.Slider(new GUIContent("Scale", "Overall scale of the station."), stationScale, 0.1f, 3f);
        uiOffsetY = EditorGUILayout.Slider(UIOffsetYTip, uiOffsetY, 0f, 5f);
        uiScale = EditorGUILayout.Slider(UIScaleTip, uiScale, 0.1f, 2f);
        manualSliderPosition = EditorGUILayout.Toggle(ManualSliderPositionTip, manualSliderPosition);
        if (manualSliderPosition)
        {
            sliderOffsetY = EditorGUILayout.Slider(SliderOffsetYTip, sliderOffsetY, -2f, 2f);
        }
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Prefab Template", EditorStyles.boldLabel);
        stationPrefabTemplate = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Station Prefab Template", "Prefab used as the base for new stations. Must have a Station component."),
            stationPrefabTemplate,
            typeof(GameObject),
            false);
        if (stationPrefabTemplate == null)
        {
            EditorGUILayout.HelpBox("No template selected. The default station_template prefab will be used.", MessageType.Warning);
        }
        EditorGUILayout.Space(4);
    }

    private static readonly string[] TemplateDisplayNames = new[]
    {
        "None",
        "Automatic Station (produces every 2s)",
        "Convert on Work (consume + produce when worked)",
        "Output Box (consume + complete goal)",
        "Single Extract (one-time use, produces resources)"
    };

    private static readonly string[] TemplateDescriptions = new[]
    {
        "",
        "Source node in the chain: continuously generates resources without player input. Use for mines, trees, wells—anything that \"spawns\" raw materials. Players gather the output to feed into Convert on Work stations.",
        "Transformation node: players bring input resources, work at the station, and receive output resources. The core of crafting—e.g. wood → planks, ore → ingots. Connects gathering (Automatic/Single Extract) to delivery (Output Box) or further conversion.",
        "Sink node in the chain: players deliver resources here to complete goals. No output—the resource is \"consumed\" for progress. Use for drop-off points, quest turn-ins, or win-condition checkpoints.",
        "Limited source node: produces resources once when worked, then becomes inactive. Use for one-time harvests (e.g. a fruit bush that depletes) or extractors that create a burst of resources. Good for risk/reward or scarce resources."
    };

    private void DrawTemplates()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Templates", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Resource chains flow: Source (gather) → Transform (craft) → Sink (deliver). Select a template to pre-fill; you can customize all values after applying.",
            MessageType.None);
        EditorGUILayout.BeginHorizontal();
        int templateIndex = EditorGUILayout.Popup(
            new GUIContent("Template", "Predefined configurations for different station behaviors."),
            (int)selectedTemplate,
            TemplateDisplayNames);
        selectedTemplate = (StationTemplate)templateIndex;
        if (GUILayout.Button("Apply Template", GUILayout.Width(120)))
        {
            ApplyTemplate(selectedTemplate);
        }
        EditorGUILayout.EndHorizontal();
        if (selectedTemplate != StationTemplate.None && (int)selectedTemplate < TemplateDescriptions.Length)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(TemplateDescriptions[(int)selectedTemplate], MessageType.None);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void ApplyTemplate(StationTemplate template)
    {
        if (template == StationTemplate.None) return;

        switch (template)
        {
            case StationTemplate.AutomaticStation:
                consumeResource = false;
                produceResource = true;
                useInputArea = false;
                useOutputArea = false;
                spawnRadius = 0.2f;
                consumeResources.Clear();
                if (produceResources.Count == 0) produceResources.Add(null);
                isSingleUse = false;
                produceCapital = false;
                consumeCapital = false;
                consumptionCompletesGoals = false;
                productionCompletesGoals = false;
                canBeWorked = false;
                productionInterval = 2f;
                workDuration = 5f;
                typeOfProduction = Station.interactionType.automatic;
                typeOfConsumption = Station.interactionType.None;
                break;

            case StationTemplate.ConvertOnWork:
                consumeResource = true;
                produceResource = true;
                useInputArea = true;
                useOutputArea = true;
                if (consumeResources.Count == 0) consumeResources.Add(null);
                if (produceResources.Count == 0) produceResources.Add(null);
                isSingleUse = false;
                produceCapital = false;
                consumeCapital = false;
                consumptionCompletesGoals = false;
                productionCompletesGoals = false;
                canBeWorked = true;
                productionInterval = 5f;
                workDuration = 1f;
                typeOfProduction = Station.interactionType.whenResourcesConsumed;
                typeOfConsumption = Station.interactionType.whenWorked;
                break;

            case StationTemplate.OutputBox:
                consumeResource = true;
                produceResource = false;
                useInputArea = true;
                useOutputArea = false;
                if (consumeResources.Count == 0) consumeResources.Add(null);
                produceResources.Clear();
                isSingleUse = false;
                produceCapital = false;
                consumeCapital = false;
                consumptionCompletesGoals = true;
                productionCompletesGoals = false;
                canBeWorked = false;
                productionInterval = 5f;
                workDuration = 5f;
                typeOfProduction = Station.interactionType.None;
                typeOfConsumption = Station.interactionType.whenResourcesConsumed;
                break;

            case StationTemplate.SingleExtract:
                consumeResource = false;
                produceResource = true;
                useInputArea = false;
                useOutputArea = false;
                spawnRadius = 0.2f;
                consumeResources.Clear();
                if (produceResources.Count == 0) produceResources.Add(null);
                isSingleUse = true;
                destroyAfterSingleUse = false;
                produceCapital = false;
                consumeCapital = false;
                consumptionCompletesGoals = false;
                productionCompletesGoals = false;
                canBeWorked = true;
                productionInterval = 5f;
                workDuration = 2f;
                typeOfProduction = Station.interactionType.whenWorked;
                typeOfConsumption = Station.interactionType.None;
                break;
        }
    }

    private void DrawFlowDiagram()
    {
        float arrowIconSize = 24f;
        float centerBandHeight = StationPreviewSize + 24;
        if (showAdvancedOptions || isSingleUse)
            centerBandHeight += 4 + 12 + 2 + StationPreviewSize;
        var arrowStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };

        EditorGUILayout.BeginHorizontal();

        // === LEFT: INPUTS ===
        EditorGUILayout.BeginVertical(GUILayout.Width(FlowColumnWidth), GUILayout.MinHeight(centerBandHeight));
        DrawInputsPanel();
        EditorGUILayout.EndVertical();

        // === CENTER BAND: Arrow | Station | Arrow (fixed height for alignment) ===
        EditorGUILayout.BeginHorizontal(GUILayout.Height(centerBandHeight), GUILayout.ExpandHeight(false));

        // Left arrow
        EditorGUILayout.BeginVertical(GUILayout.Width(ArrowWidth));
        GUILayout.FlexibleSpace();
        var arrowRect = GUILayoutUtility.GetRect(arrowIconSize, arrowIconSize);
        GUI.Label(arrowRect, "→", arrowStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();

        // Station icon
        EditorGUILayout.BeginVertical(GUILayout.Width(StationPreviewSize + 24));
        GUILayout.FlexibleSpace();
        var centerReserved = GUILayoutUtility.GetRect(StationPreviewSize, StationPreviewSize);
        var centerBgRect = new Rect(centerReserved.x - 6, centerReserved.y - 6, centerReserved.width + 12, centerReserved.height + 12);
        EditorGUI.DrawRect(centerBgRect, new Color(0.25f, 0.25f, 0.28f, 0.8f));
        stationGraphic = (Sprite)EditorGUI.ObjectField(centerReserved, stationGraphic, typeof(Sprite), false);
        previewRect = centerReserved;

        if (stationGraphic != null)
        {
            Texture previewTex = AssetPreview.GetAssetPreview(stationGraphic);
            if (previewTex != null)
            {
                GUI.DrawTexture(previewRect, previewTex, ScaleMode.ScaleToFit, true, 0, Color.white, 0, 0);
            }
            else if (stationGraphic.texture != null)
            {
                Texture tex = stationGraphic.texture;
                Rect texCoords = new Rect(
                    stationGraphic.textureRect.x / tex.width,
                    (tex.height - stationGraphic.textureRect.y - stationGraphic.textureRect.height) / tex.height,
                    stationGraphic.textureRect.width / tex.width,
                    stationGraphic.textureRect.height / tex.height);
                GUI.DrawTextureWithTexCoords(previewRect, tex, texCoords, true);
                Repaint();
            }
            else
            {
                var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(previewRect, "Loading...", style);
                Repaint();
            }
        }
        else
        {
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };
            GUI.Label(previewRect, "Drop sprite\nhere", style);
        }

        if (showAdvancedOptions || isSingleUse)
        {
            EditorGUILayout.Space(4);
            var deadLabelRect = GUILayoutUtility.GetRect(StationPreviewSize, 12);
            GUI.Label(deadLabelRect, "Dead Sprite", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);
            var deadReserved = GUILayoutUtility.GetRect(StationPreviewSize, StationPreviewSize);
            var deadBgRect = new Rect(deadReserved.x - 6, deadReserved.y - 6, deadReserved.width + 12, deadReserved.height + 12);
            EditorGUI.DrawRect(deadBgRect, new Color(0.25f, 0.25f, 0.28f, 0.8f));
            deadSprite = (Sprite)EditorGUI.ObjectField(deadReserved, deadSprite, typeof(Sprite), false);
            if (deadSprite != null)
            {
                Texture deadPreviewTex = AssetPreview.GetAssetPreview(deadSprite);
                if (deadPreviewTex != null)
                {
                    GUI.DrawTexture(deadReserved, deadPreviewTex, ScaleMode.ScaleToFit, true, 0, Color.white, 0, 0);
                }
                else if (deadSprite.texture != null)
                {
                    Texture tex = deadSprite.texture;
                    Rect texCoords = new Rect(
                        deadSprite.textureRect.x / tex.width,
                        (tex.height - deadSprite.textureRect.y - deadSprite.textureRect.height) / tex.height,
                        deadSprite.textureRect.width / tex.width,
                        deadSprite.textureRect.height / tex.height);
                    GUI.DrawTextureWithTexCoords(deadReserved, tex, texCoords, true);
                    Repaint();
                }
            }
            else
            {
                var deadStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };
                GUI.Label(deadReserved, "Drop sprite\nhere", deadStyle);
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();

        // Right arrow
        EditorGUILayout.BeginVertical(GUILayout.Width(ArrowWidth));
        GUILayout.FlexibleSpace();
        var arrowOutRect = GUILayoutUtility.GetRect(arrowIconSize, arrowIconSize);
        GUI.Label(arrowOutRect, "→", arrowStyle);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // === RIGHT: OUTPUTS ===
        EditorGUILayout.BeginVertical(GUILayout.Width(FlowColumnWidth), GUILayout.MinHeight(centerBandHeight));
        DrawOutputsPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    private void DrawResourceIcon(Resource resource, float size)
    {
        if (resource == null || resource.icon == null) return;

        var iconRect = GUILayoutUtility.GetRect(size, size);
        Texture previewTex = AssetPreview.GetAssetPreview(resource.icon);
        if (previewTex != null)
        {
            GUI.DrawTexture(iconRect, previewTex, ScaleMode.ScaleToFit, true);
        }
        else if (resource.icon.texture != null)
        {
            Texture tex = resource.icon.texture;
            Rect texCoords = new Rect(
                resource.icon.textureRect.x / tex.width,
                (tex.height - resource.icon.textureRect.y - resource.icon.textureRect.height) / tex.height,
                resource.icon.textureRect.width / tex.width,
                resource.icon.textureRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(iconRect, tex, texCoords, true);
            Repaint();
        }
    }

    private void DrawInputsPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(FlowColumnWidth - 12));
        int savedIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        GUILayout.Label("INPUTS", EditorStyles.miniBoldLabel);
        EditorGUILayout.Space(2);

        var r1 = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
        consumeResource = EditorGUI.ToggleLeft(r1, ConsumeResourceTip, consumeResource);
        if (consumeResource)
        {
            if (consumeResources.Count == 0)
                consumeResources.Add(null);
            EditorGUI.indentLevel = 1;
            for (int i = 0; i < consumeResources.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                consumeResources[i] = (Resource)EditorGUILayout.ObjectField(ConsumeResourceSlotTip, consumeResources[i], typeof(Resource), false);
                DrawResourceIcon(consumeResources[i], 24f);
                if (GUILayout.Button("−", GUILayout.Width(20)))
                {
                    consumeResources.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Input Resource"))
            {
                consumeResources.Add(null);
            }
        }
        EditorGUILayout.Space(2);

        var r2 = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
        consumeCapital = EditorGUI.ToggleLeft(r2, ConsumeCapitalTip, consumeCapital);
        if (consumeCapital)
        {
            EditorGUI.indentLevel = 1;
            capitalInputAmount = EditorGUILayout.IntField(CapitalInputAmountTip, Mathf.Max(1, capitalInputAmount));
        }
        EditorGUILayout.Space(2);

        var r3 = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
        consumptionCompletesGoals = EditorGUI.ToggleLeft(r3, ConsumptionCompletesGoalsTip, consumptionCompletesGoals);

        EditorGUI.indentLevel = savedIndent;
        EditorGUILayout.EndVertical();
    }

    private void DrawOutputsPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(FlowColumnWidth - 12));
        int savedIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        GUILayout.Label("OUTPUTS", EditorStyles.miniBoldLabel);
        EditorGUILayout.Space(2);

        var r1 = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
        produceResource = EditorGUI.ToggleLeft(r1, ProduceResourceTip, produceResource);
        if (produceResource)
        {
            EditorGUI.indentLevel = 1;
            produceFromLootTable = EditorGUILayout.Toggle(ProduceFromLootTableTip, produceFromLootTable);
            if (produceFromLootTable)
            {
                produceLootTable = (LootTable)EditorGUILayout.ObjectField(
                    new GUIContent("Loot Table", "Loot table defining weighted random output resources."),
                    produceLootTable, typeof(LootTable), false);
            }
            else
            {
                if (produceResources.Count == 0)
                    produceResources.Add(null);
                for (int i = 0; i < produceResources.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    produceResources[i] = (Resource)EditorGUILayout.ObjectField(ProduceResourceSlotTip, produceResources[i], typeof(Resource), false);
                    DrawResourceIcon(produceResources[i], 24f);
                    if (GUILayout.Button("−", GUILayout.Width(20)))
                    {
                        produceResources.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("+ Add Output Resource"))
                {
                    produceResources.Add(null);
                }
            }
        }
        EditorGUILayout.Space(2);

        var r2 = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
        produceCapital = EditorGUI.ToggleLeft(r2, ProduceCapitalTip, produceCapital);
        if (produceCapital)
        {
            EditorGUI.indentLevel = 1;
            capitalOutputAmount = EditorGUILayout.IntField(CapitalOutputAmountTip, Mathf.Max(1, capitalOutputAmount));
        }
        EditorGUILayout.Space(2);

        var r3 = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
        productionCompletesGoals = EditorGUI.ToggleLeft(r3, ProductionCompletesGoalsTip, productionCompletesGoals);

        EditorGUI.indentLevel = savedIndent;
        EditorGUILayout.EndVertical();
    }

    private void DrawGeneralSettings()
    {
        GUILayout.Label("General Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(FlowColumnWidth));
        isSingleUse = EditorGUILayout.Toggle(SingleUseTip, isSingleUse);
        if (isSingleUse)
        {
            destroyAfterSingleUse = EditorGUILayout.Toggle(DestroyAfterSingleUseTip, destroyAfterSingleUse);
        }
        useInputArea = EditorGUILayout.Toggle(UseInputAreaTip, useInputArea);
        useOutputArea = EditorGUILayout.Toggle(UseOutputAreaTip, useOutputArea);
        spawnRadius = EditorGUILayout.FloatField(SpawnRadiusTip, Mathf.Max(0.01f, spawnRadius));
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical();
        canBeWorked = EditorGUILayout.Toggle(CanBeWorkedTip, canBeWorked);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(FlowColumnWidth));
        if (canBeWorked)
        {
            workDuration = EditorGUILayout.FloatField(WorkDurationTip, Mathf.Max(0.1f, workDuration));
        }
        else
        {
            productionInterval = EditorGUILayout.FloatField(ProductionIntervalTip, Mathf.Max(0.1f, productionInterval));
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical();
        typeOfProduction = (Station.interactionType)EditorGUILayout.EnumPopup(
            new GUIContent("Production Trigger", "whenWorked / whenResourcesConsumed / cycle / automatic"),
            typeOfProduction);
        typeOfConsumption = (Station.interactionType)EditorGUILayout.EnumPopup(
            new GUIContent("Consumption Trigger", "whenWorked / whenResourcesConsumed / cycle / automatic"),
            typeOfConsumption);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    private void DrawCreateButton()
    {
        EditorGUILayout.Space(4);
        var createRect = GUILayoutUtility.GetRect(0, 36);
        createRect.x += 20;
        createRect.width -= 40;

        bool hasConsumeResources = !consumeResource || consumeResources.Exists(r => r != null);
        bool hasProduceResources = !produceResource
            || (produceFromLootTable && produceLootTable != null)
            || produceResources.Exists(r => r != null);
        bool canCreate = hasConsumeResources && hasProduceResources;

        EditorGUI.BeginDisabledGroup(!canCreate);
        if (GUI.Button(createRect, "Create Station"))
        {
            CreateStation();
        }
        EditorGUI.EndDisabledGroup();

        if (!canCreate)
        {
            string msg = "When 'Consume Resources' or 'Produce Resources' is enabled, add at least one resource";
            if (produceResource && produceFromLootTable)
                msg += " (or assign a loot table)";
            msg += ".";
            EditorGUILayout.HelpBox(msg, MessageType.Warning);
        }

        EditorGUILayout.HelpBox(
            "Creates the StationDataSO asset, a new prefab (from station_template), and instantiates it in the scene with all data assigned.",
            MessageType.None);

        EditorGUILayout.Space(6);
    }

    private void CreateStation()
    {
        GameObject templatePrefab = stationPrefabTemplate;
        if (templatePrefab == null)
        {
            templatePrefab = SA_AssetPathHelper.FindPrefab(DefaultTemplatePrefabPath);
        }
        if (templatePrefab == null)
        {
            Debug.LogError($"Station Builder: Template prefab not found at {DefaultTemplatePrefabPath}");
            return;
        }

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            Debug.LogError("Station Builder: No active scene. Open a scene first.");
            return;
        }

        SA_AssetPathHelper.EnsureAssetPathDirectories("Game Assemblies/Databases/Stations");
        SA_AssetPathHelper.EnsureAssetPathDirectories("Game Assemblies/Prefabs/Stations");

        StationDataSO data = CreateStationDataFromBuilderState();
        string dataAssetPath = $"Assets/Game Assemblies/Databases/Stations/{stationName}.asset";
        AssetDatabase.CreateAsset(data, dataAssetPath);
        AssetDatabase.SaveAssets();

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(templatePrefab, scene);
        instance.name = stationName;

        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        GameObject stationsRoot = GameObject.Find("Stations");
        if (stationsRoot == null)
        {
            stationsRoot = new GameObject("Stations");
            stationsRoot.transform.position = Vector3.zero;
        }
        instance.transform.SetParent(stationsRoot.transform);
        instance.transform.position = Vector3.zero;

        Station station = instance.GetComponent<Station>();
        if (station == null)
        {
            Debug.LogError("Station Builder: Template prefab has no Station component.");
            Object.DestroyImmediate(instance);
            return;
        }

        data.ApplyToStation(station);

        if (stationGraphic != null)
        {
            var sr = instance.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.GetComponent<Canvas>() == null && sr.transform.GetComponentInParent<Canvas>() == null)
            {
                ResizeCollidersToSprite(sr.gameObject, stationGraphic, sr);
            }
        }

        string prefabPath = $"Assets/Game Assemblies/Prefabs/Stations/{stationName}.prefab";
        GameObject newPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.AutomatedAction);
        if (newPrefab != null)
        {
            data.stationPrefab = newPrefab;
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        Selection.activeGameObject = instance;
        EditorGUIUtility.PingObject(instance);
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log($"Station Builder: Created '{stationName}' - StationDataSO at {dataAssetPath}, prefab at {prefabPath}, instance in scene.");
    }

    private StationDataSO CreateStationDataFromBuilderState()
    {
        StationDataSO data = ScriptableObject.CreateInstance<StationDataSO>();
        data.stationName = stationName;
        data.stationGraphic = stationGraphic;
        data.stationSpriteTint = stationSpriteTint;
        data.stationScale = stationScale;
        data.offsetY = uiOffsetY;
        data.uiScale = uiScale;
        data.manualSliderPosition = manualSliderPosition;
        data.sliderOffsetY = sliderOffsetY;
        data.deadSprite = deadSprite;
        data.consumeResource = consumeResource;
        data.produceResource = produceResource;
        data.consumes = new List<Resource>();
        foreach (var r in consumeResources)
            if (r != null) data.consumes.Add(r);
        data.produces = new List<Resource>();
        foreach (var r in produceResources)
            if (r != null) data.produces.Add(r);
        data.whatToProduce = produceFromLootTable && produceLootTable != null
            ? Station.productionMode.LootTable
            : Station.productionMode.Resource;
        data.produceLootTable = produceFromLootTable ? produceLootTable : null;
        data.spawnResourcePrefab = true;
        data.useInputArea = useInputArea;
        data.useOutputArea = useOutputArea;
        data.spawnRadius = spawnRadius;
        data.isSingleUse = isSingleUse;
        data.destroyAfterSingleUse = destroyAfterSingleUse;
        data.capitalInput = consumeCapital;
        data.capitalOutput = produceCapital;
        data.capitalInputAmount = consumeCapital ? capitalInputAmount : 0;
        data.capitalOutputAmount = produceCapital ? capitalOutputAmount : 0;
        data.completesGoals_consumption = consumptionCompletesGoals;
        data.completesGoals_production = productionCompletesGoals;
        data.canBeWorked = canBeWorked;
        data.workDuration = workDuration;
        data.productionInterval = productionInterval;
        data.typeOfProduction = canBeWorked ? typeOfProduction : Station.interactionType.automatic;
        data.typeOfConsumption = canBeWorked ? typeOfConsumption : Station.interactionType.automatic;
        if (!canBeWorked)
        {
            data.typeOfProduction = Station.interactionType.automatic;
            data.typeOfConsumption = Station.interactionType.automatic;
        }
        return data;
    }

    private static void ResizeCollidersToSprite(GameObject gameObject, Sprite sprite, SpriteRenderer spriteRenderer)
    {
        Bounds bounds = sprite.bounds;
        Vector2 size = bounds.size;
        Vector2 center = bounds.center;

        foreach (var collider in gameObject.GetComponents<Collider2D>())
        {
            if (collider is BoxCollider2D box)
            {
                Undo.RecordObject(box, "Resize Station Collider");
                box.size = size;
                box.offset = center;
            }
            else if (collider is CircleCollider2D circle)
            {
                Undo.RecordObject(circle, "Resize Station Collider");
                float radius = Mathf.Min(size.x, size.y) * 0.5f;
                circle.radius = radius;
                circle.offset = center;
            }
            else if (collider is PolygonCollider2D poly)
            {
                Undo.RecordObject(poly, "Resize Station Collider");
                var physicsShape = new List<Vector2>();
                if (sprite.GetPhysicsShapeCount() > 0)
                {
                    sprite.GetPhysicsShape(0, physicsShape);
                    poly.points = physicsShape.ToArray();
                }
                else
                {
                    Vector2 extents = size * 0.5f;
                    poly.points = new Vector2[]
                    {
                        center + new Vector2(-extents.x, -extents.y),
                        center + new Vector2(extents.x, -extents.y),
                        center + new Vector2(extents.x, extents.y),
                        center + new Vector2(-extents.x, extents.y)
                    };
                }
            }
        }
    }
}
