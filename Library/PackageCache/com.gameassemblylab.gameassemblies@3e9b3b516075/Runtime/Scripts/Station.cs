using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.2D;
using System.Collections;
//using System;

public class Station : ResourceNode
{
    [Header("Station Data (SO)")]
    [Tooltip("ScriptableObject containing this station's configuration. All behavior data is read from here.")]
    public StationDataSO stationData;

    [Header("Managers")]
    public StationManager sManager;
    public ResourceManager rManager;
    public GoalManager gManager;

    public enum productionMode
    {
        Resource,
        Station,
        LootTable
    }
    [Header("Consume (IN) - Produce (OUT)")]
    // --- Data from StationDataSO (getters read from stationData; hidden from inspector) ---
    [HideInInspector] public productionMode WhatToProduce => stationData != null ? stationData.whatToProduce : productionMode.Resource;
    [HideInInspector] public bool produceResource => stationData != null && stationData.produceResource;
    [HideInInspector] public bool consumeResource => stationData != null && stationData.consumeResource;
    [HideInInspector] public List<Resource> produces => stationData != null ? stationData.produces : new List<Resource>();
    [HideInInspector] public List<Resource> consumes => stationData != null ? stationData.consumes : new List<Resource>();

    public List<Station> produces_stations = new List<Station>();

    public LootTable produceLootTable;

    [HideInInspector] public float productionInterval => stationData != null ? stationData.productionInterval : 5f;
    private float productionTimer = 0f;       // count for the automatic production
    [HideInInspector] public bool spawnResourcePrefab => stationData != null && stationData.spawnResourcePrefab;
    private Vector3 spawnOffset = Vector3.zero;
    [HideInInspector] public float spawnRadius => stationData != null ? stationData.spawnRadius : 1f;

    [HideInInspector] public bool capitalInput => stationData != null && stationData.capitalInput;
    [HideInInspector] public bool capitalOutput => stationData != null && stationData.capitalOutput;
    [HideInInspector] public int capitalInputAmount => stationData != null ? stationData.capitalInputAmount : 0;
    [HideInInspector] public int capitalOutputAmount => stationData != null ? stationData.capitalOutputAmount : 0;

    [Header("Input/Output Areas")]
    public bool useInputArea = true;
    public bool useOutputArea = true;
    public Area inputArea;
    public Area outputArea;

    [HideInInspector] public bool canBeWorked => stationData != null && stationData.canBeWorked;

    public enum interactionType
    {
        None,
        automatic,
        whenWorked,
        whenResourcesConsumed,
        cycle
    }
    [HideInInspector] public interactionType typeOfProduction => stationData != null ? stationData.typeOfProduction : interactionType.None;
    [HideInInspector] public interactionType typeOfConsumption => stationData != null ? stationData.typeOfConsumption : interactionType.None;
    [HideInInspector] public float workDuration => stationData != null ? stationData.workDuration : 5f;
    public float workProgress = 0f; // Progress towards completing the work cycle
    public bool isBeingWorkedOn = false; // Is work currently being performed?
    public List<playerController> workerCount;
    private Coroutine workCoroutine; // Reference to the ongoing work coroutine

    [Header("Lifespan (DECAY / TIME)")] //This is for making the input resources a requirement for the station not to die
    //public bool decayWithoutInput = false;
    public int decayValue = 0;
    public int maxDecay = 5;
    public float decayTimer = 0f;
    public float decayCycle = 10.0f;

    [HideInInspector] public bool isSingleUse => stationData != null && stationData.isSingleUse;
    [HideInInspector] public bool destroyAfterSingleUse => stationData != null && stationData.destroyAfterSingleUse;
    public bool isAlive = true;
    private SpriteRenderer spRender;
    [HideInInspector] public Sprite normalSprite => stationData != null ? stationData.stationGraphic : null;
    [HideInInspector] public Sprite deadSprite => stationData != null ? (stationData.deadSprite != null ? stationData.deadSprite : stationData.stationGraphic) : null;
    public int age = 0;
    private float ageTimer = 0f;
    public float growthRate = 1.0f; //every how many seconds grow older
    public int maxAge = 100;
    public List<Sprite> ageSprites = new List<Sprite>();
    public bool useAgeSprites = false;
    public bool canDie = false;
    public bool canGrow = false;
    public bool randStartAge = false;

    [Header("Upgrades (TRANSFORMATION)")]
    public bool canBeUpgraded = false;
    public GameObject upgradePrefab;
    private int flaggedToUpgrade = -1;

    [HideInInspector] public bool completesGoals_production => stationData != null && stationData.completesGoals_production;
    [HideInInspector] public bool completesGoals_consumption => stationData != null && stationData.completesGoals_consumption;

    [Header("Property System (OWNER/WORKER)")]
    public playerController owner;
    public playerController worker;

    [Header("Inspect / Work UI (UI POP UP)")]
    public bool isBeingInspected = false; // Is the station being inspected?
    public Transform inspectionPoint; // Point where the inspection window should be displayed
    public GameObject sliderBar;
    public GameObject infoWindow;
    Slider progressSlider;
    public bool manualSliderPosition = true;
    public float offsetY = 2f;

    //public float worker_owner_distribution = 0.5f; //not in use yet
    public int purchasePrice = 0;

    [Header("Audio")]
    bool prevWork = false;
    public AudioSource outputAudio;
    public AudioClip workingSound;
    public AudioClip completeSound;
    public AudioClip notEnoughMaterialsSound;

    [Header("Particles")]
    public GameObject productionParticles;


    [Header("Debug Tools")]
    public bool doUpdate = false;
    public bool debug = false;


    private bool resourcesConsumed = false;
    private bool workCompleted = false;
    private bool coroutineRunning = false;


    // Start is called before the first frame update
    protected void Start()   
    {
        workerCount = new List<playerController>();

        if (!canBeWorked) sliderBar.SetActive(false);

        progressSlider = sliderBar.GetComponent<Slider>();
        if (inputArea != null) inputArea.requirements = consumes;

        sManager = GameObject.FindAnyObjectByType<StationManager>();
        rManager = GameObject.FindAnyObjectByType<ResourceManager>();
        gManager = GameObject.FindAnyObjectByType<GoalManager>();

        if (sManager != null) sManager.allStations.Add(this);

        spRender = transform.GetComponent<SpriteRenderer>();

        if (randStartAge)
        {
            age = Random.Range(0, maxAge);//make random age based on max age
            ageTimer = Random.Range(0, growthRate);
        }

        InitializeInforPanel();
        updateInfoPanel();

        prevWork = isBeingWorkedOn;
    }

    void onDestroy()
    {
        if (sManager != null) sManager.allStations.Remove(this);
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("workCompleted: " + workCompleted);
        //CONTROL PANEL
        if (isAlive)
        {
            updateInfoPanel(); //show info panel if inspected
            if (typeOfProduction == interactionType.whenWorked || typeOfConsumption == interactionType.whenWorked) updateSlider(); //1 - update the slider for workable units
            if (typeOfConsumption == interactionType.cycle) updateSliderCycle();

            if (produceResource)
            {
                if (typeOfProduction == interactionType.automatic) AutomaticProduction(); //and capital for owner
                if (typeOfProduction == interactionType.whenWorked) ProduceOnWork();//and capital for worker
                if (typeOfProduction == interactionType.whenResourcesConsumed) ConsumedProduction(); //and capital for worker

            }
            if (consumeResource)
            {
                if (typeOfConsumption == interactionType.automatic) AutomaticConsumption(); //and capital for owner
                if (typeOfConsumption == interactionType.whenWorked) ConsumeOnWork();//and capital for worker
                if (typeOfConsumption == interactionType.cycle) ConsumeOnCycle();
            }

            growOlder();
            workCompleted = false;

            playWorkingSound();
            if (flaggedToUpgrade > -1) flaggedToUpgrade--;
            if (flaggedToUpgrade == 0) upgradeStation();
           
        }
    }

    public void playWorkingSound()
    {
        if (outputAudio == null) return;
        if (workingSound == null) return;

        //Debug.Log("AudioCalled");

        if (prevWork != isBeingWorkedOn && isBeingWorkedOn == true)
        {
            outputAudio.clip = workingSound;
            outputAudio.Play();
            prevWork = isBeingWorkedOn;
            //Debug.Log("PLAY");
        } else if (prevWork != isBeingWorkedOn && isBeingWorkedOn == false) 
        {
            outputAudio.Stop();
            prevWork = isBeingWorkedOn;
            //Debug.Log("STOP");
        } else {
            //Debug.Log("NOTHING");
        }
    }

    public void ConsumeOnCycle()
    {
        decayTimer += Time.deltaTime; //decay timer
        if (decayTimer >= decayCycle) //decay cycle
        {   
            //if resources are there, they are consumed, if they are not, the unit decays
            if (inputArea.AreAllRequirementsMet()) 
            {
                ConsumeResource();
                ConsumeCapital(worker);
                //Debug.Log("TRUE!");
            } else{
                if (decayValue < maxDecay)
                {
                    decayValue++;
                } else
                {
                    isAlive = false;
                    swapSprite();
                }
                
                //Debug.Log("FALSE!");
            }
            decayTimer = 0f;
        }
        
    }
    public void growOlder()
    {
        if (canGrow)
        {
            ageTimer += Time.deltaTime;
            if(ageTimer >= growthRate)
            {
                age++;
                ageTimer = 0f;
            }
        }

        if (useAgeSprites)
        {
            if (ageSprites.Count > 0)
            {
                if (age < ageSprites.Count)
                {
                    spRender.sprite = ageSprites[age];
                }
            }
        }

    }
    public void ConsumeCapital(playerController pC)
    {
        if (capitalInput)
        {
            Debug.Log("Capital consumption called");
            if (pC != null)
            {
                Debug.Log("Capital removed from worker");
                pC.capital -= capitalInputAmount;
            }
            if (rManager != null)
            {
                Debug.Log("Capital removed from global");
                rManager.globalCapital -= capitalInputAmount;
            }
        }
    }
    public void ProduceCapital(playerController pC)
    {
        if (capitalOutput)
        {
            if (pC != null) pC.capital += capitalOutputAmount;
            if (rManager != null) rManager.globalCapital += capitalOutputAmount;
        }
    }
    public void swapSprite()
    {
        if (isAlive)
        {
            spRender.sprite = normalSprite;
        } else
        {
            spRender.sprite = deadSprite;
            sliderBar.SetActive(false);
            isBeingInspected = false;
            if (infoWindow != null) infoWindow.SetActive(false);

            if (destroyAfterSingleUse) Destroy(this.gameObject);
        }
    }
    public void InitializeInforPanel()
    {
        if(infoWindow != null)
        {
            infoWindow.GetComponent<InfoWindow>().InitializeResources(produces, consumes);
        }
        
    }
    public void updateInfoPanel()
    {
        if (isBeingInspected)
        {   
            if(infoWindow != null)
            {
                infoWindow.SetActive(true);
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * offsetY);
                infoWindow.transform.position = screenPosition;
                infoPanelAnimation();
            }
            
        } else
        {
            if (infoWindow != null) infoWindow.SetActive(false); // could be optimized
        }
    }

    public void infoPanelAnimation()
    {
        if (!coroutineRunning && doUpdate)
        {
            doUpdate = false;
            float startScale = infoWindow.transform.localScale.x + 0.5f;
            float endScale = infoWindow.transform.localScale.x;
            float duration = 0.5f;
            StartCoroutine(ScaleOverTime(infoWindow, new Vector3(startScale, startScale, startScale), new Vector3(endScale, endScale, endScale), duration));
        }
    }
    public void updateSliderCycle()
    {
        sliderBar.SetActive(true);
        if (manualSliderPosition)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * offsetY + Vector3.up * (stationData != null ? stationData.sliderOffsetY : -0.46f));
            progressSlider.transform.position = screenPosition;
        }
        float sliderValue = decayTimer / decayCycle;
        progressSlider.value = sliderValue;
    }
    public void updateSlider()
    {
        if (isBeingWorkedOn)
        {
            sliderBar.SetActive(true);
            if (manualSliderPosition)
            {
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * offsetY + Vector3.up * (stationData != null ? stationData.sliderOffsetY : -0.46f));
                progressSlider.transform.position = screenPosition;
            }
            float sliderValue = workProgress / workDuration;
            progressSlider.value = sliderValue;
        } else
        {
            sliderBar.SetActive(false); // could be optimized
            progressSlider.value = 0;
        }
    }
    public void executeLabor(playerController newWorker)
    {
        if(!workerCount.Contains(newWorker) ) { workerCount.Add(newWorker); } //only add the worker to the list if new entity

        isBeingWorkedOn = true;
        //workProgress += Time.deltaTime; //ONE PLAYER CAN WORK
        workProgress += Time.deltaTime * workerCount.Count;
        //if (debug) Debug.Log("Work Pogress: " + workProgress + "/" + workDuration);
        if (workProgress >= workDuration)
            {
                CompleteWork();
            }
    }
    public void cancelLabor(playerController newWorker)
    {
        if (workerCount.Contains(newWorker)) { workerCount.Remove(newWorker); }

        if(workerCount.Count == 0)
        {
            isBeingWorkedOn = false;
            workProgress = 0;
        }
        
    }
    private void CompleteWork()
    {
        if (debug) Debug.Log("Completed Labor Cycle.");

        // ON COMPLETED ->
        workCompleted = true;
        //if(produceResource) ProduceResource();
        //if(consumeResource) ConsumeResource();

        // Reset work progress
        workProgress = 0f;

        //if(canBeUpgraded) upgradeStation(); //0-----------------------------------------
    }
    void upgradeStation()
    {
        if(upgradePrefab != null)
        {
            GameObject newStation = Instantiate(upgradePrefab, transform.position, Quaternion.identity);
            Destroy(this.gameObject);
        }
    }
    void ProduceOnWork()
    {
        if (workCompleted)
        {
            ProduceResource();
            ProduceCapital(worker);
            //workCompleted = false;
        }
    }
    void ConsumeOnWork()
    {
        if(debug) Debug.Log("Consume on Work Called, workCompleted: " + workCompleted);
        if (workCompleted)
        {
            if (debug) Debug.Log("Consume on Work Called Completed Sequence");
            ConsumeResource();
            ConsumeCapital(worker);
            //workCompleted = false;
        }
    }
    void ConsumeResource()
    {
        if (inputArea == null) return;

        if (debug) Debug.Log("station check: " + inputArea.allRequirementsMet);
        if (inputArea.allRequirementsMet)
        {
            inputArea.RemoveMatchingResources();
            resourcesConsumed = true;

            if (completesGoals_consumption && gManager != null && consumes.Count > 0 && consumes[0] != null)
                gManager.goalContribution(consumes[0]); //IF IT CONTRIBUTES TO GOALS - CHECK AND PASS INFO TO THE GLOBAL SCORE

            if (canBeUpgraded) flaggedToUpgrade = 2;//upgradeStation();
        }
    }
    void ConsumedProduction()
    {
        if (debug) Debug.Log("Consumed Production Started - resources Consumed: " + resourcesConsumed);
        if (resourcesConsumed)
        {
            ProduceResource();
            ProduceCapital(worker);
            resourcesConsumed = false;
        }
    }
    void AutomaticConsumption()
    {
        if (debug) Debug.Log("Automatic Consumption Running - ");
        //productionTimer += Time.deltaTime;

        //if (productionTimer >= productionInterval)
        //{
            ConsumeResource();
            ConsumeCapital(owner);
            //productionTimer = 0f;
        //}
    }
    void AutomaticProduction()
    {
        if (debug) Debug.Log("Automatic Production Running - ");
        productionTimer += Time.deltaTime;

        if (productionTimer >= productionInterval)
        {
            ProduceResource();
            ProduceCapital(owner);

            productionTimer = 0f;
        }
    }

    public void playProductionSound()
    {
        if(outputAudio == null || completeSound == null) return;
        outputAudio.clip = completeSound;
        outputAudio.Play();
    }

    void ProduceResource()
    {
        if (WhatToProduce == productionMode.Resource)
        {
            for (int i = 0; i < produces.Count; i++)
            {
                // Add the produced resources to the local storage
                AddResource(produces[i], 1);

                // Optional: Visual or audio feedback
                //if (debug) 
                if (debug) Debug.Log($"{gameObject.name}: Produced {1} of {produces[i].resourceName}");

                if (spawnResourcePrefab)
                {  
                    InstantiateResourcePrefabs(produces[i]);// Instantiate the resource prefab
                }

                if (completesGoals_production && gManager != null && produces.Count > 0 && produces[0] != null)
                    gManager.goalContribution(produces[0]); //CONTRIBUTES TO GOALS THROUGH PRODUCTION


                //AUDIO:
                playProductionSound();
                
            }

            if (isSingleUse)
            {
                isAlive = false;
                swapSprite();
                
            }
        } else if (WhatToProduce == productionMode.Station)
        {
            for (int i = 0; i < produces_stations.Count; i++)
            {
                InstantiateStationPrefabs(i);
            }
        }else if (WhatToProduce == productionMode.LootTable)
        {
            if(produceLootTable != null)
            {
                var (output, qty) = produceLootTable.GetRandomDrop();
                if (output != null)
                {
                    AddResource(output, qty);
                    if (debug) Debug.Log($"{gameObject.name}: Produced {qty} of {output.resourceName}");
                    if (spawnResourcePrefab)
                    {
                        for (int i = 0; i < qty; i++)
                            InstantiateResourcePrefabs(output);
                    }

                    playProductionSound();

                    if (isSingleUse)
                    {
                        isAlive = false;
                        swapSprite();
                    }
                }
            } else
            {
                Debug.LogError("No Loot Table available");
            }
        }
    }
    void InstantiateStationPrefabs(int index)
    {
        if (produces_stations[index] != null)
        {
            Vector3 spawnPosition = outputArea.GetPosition();
            GameObject stationInstance = Instantiate(produces_stations[index].gameObject, spawnPosition, Quaternion.identity);
        }
    }
    void InstantiateResourcePrefabs(Resource rs)
    {
        if (rs.resourcePrefab != null)
        {
            if (useOutputArea)
            {
                //Vector3 spawnPosition = outputArea.GetPosition();
                Vector3 spawnPosition = outputArea.GetPositionWithRandomness(0.1f);
                GameObject resourceInstance = Instantiate(rs.resourcePrefab, spawnPosition, Quaternion.identity);

                if (productionParticles != null) Instantiate(productionParticles, spawnPosition, Quaternion.identity);

            } else
            {
                // Generate a random point inside a circle
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;

                // Use the random circle coordinates for X and Z axes
                Vector3 randomOffset = new Vector3(randomCircle.x, randomCircle.y, 0);

                Vector3 spawnPosition = transform.position + randomOffset;
                GameObject resourceInstance = Instantiate(rs.resourcePrefab, spawnPosition, Quaternion.identity);


                if (productionParticles != null) Instantiate(productionParticles, spawnPosition, Quaternion.identity);

                //Set owner
                ResourceObject rsObj = resourceInstance.GetComponent<ResourceObject>();
                if (rsObj != null && owner != null)
                {
                    rsObj.setOwner(owner);
                }

            }
        } else
        {
            //Debug.LogWarning($"{gameObject.name}: Resource prefab is not assigned for {resourceToProduce.resourceName}");
        }
    }
    IEnumerator ScaleOverTime(GameObject obj, Vector3 startScale, Vector3 endScale, float duration)
    {
        float elapsed = 0f;
        coroutineRunning = true;
        while (elapsed < duration)
        {
            // Normalize the elapsed time
            float t = elapsed / duration;

            // Apply the easing function
            float scaleValue = Tween.EaseOutBack(t);

            // Interpolate the scale
            obj.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, scaleValue);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the final scale is set
        obj.transform.localScale = endScale;
        coroutineRunning = false;
    }
}
