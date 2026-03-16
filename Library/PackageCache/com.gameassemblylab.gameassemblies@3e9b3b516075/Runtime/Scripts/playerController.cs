using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Random = UnityEngine.Random; // Add this line
using UnityEngine.UI;
using System.Collections;
using UnityEngine.InputSystem.XR;

public class playerController : MonoBehaviour
{
    [Header("Managers")]
    public creationManager cManager;
    public GameManager gameManager;
    public PlayerInput playerInputComponent;

    [Header("Possible Player Graphics")]
    public Sprite sprite1;
    public Sprite sprite2;
    public Sprite sprite3;
    public Sprite sprite4;
    public List<Sprite> characterSprites;
    public SpriteRenderer playerSprite;
    public int spriteID = 0;
    private bool facingRight = true;

    [Header("Speed / Motion")]
    public float playerSpeed = 1f;
    public Rigidbody2D rb;
    private Vector2 playerVelocity;
    private Vector2 movementInput = Vector2.zero;


    private bool fired = false;
    //public SpriteRenderer spriteRenderer;
    public playersInfo pInfo;
    public int playerID = 0;
    public int playerIDOffset = 0;
    
    public bool isCarryingObject = false;
    public Transform carryPosition; 
    public GameObject objectToGrab;
    
    public List <GameObject> listobjectsToGrab = new List<GameObject>();
    public int maxObjectsToCarry = 2;
    public bool isAbsorbingResources = false;

    private Vector3 grabOffset;

    public GameObject objectToLabor;
    public List<GameObject> listObjectsToLabor = new List<GameObject>();
    public GameObject objectToInspect;
    public List<GameObject> listObjectsToInspect = new List<GameObject>();
    public bool performingLabor = false;

    [Header("Snap Target")]
    [Tooltip("When enabled, the grab area snaps to the nearest grabbable/workable within range, making grabbing and working easier.")]
    public bool useSnapTarget = true;
    [Tooltip("Radius within which the grab area snaps to the nearest grabbable/workable. Larger values make grabbing easier.")]
    public float snapRadius = 0.1f;
    [Tooltip("Bias toward targets in front of the player. Higher values prefer targets in the aim direction.")]
    [Range(0f, 1f)]
    public float snapAimBias = 0f;

    public int capital = 0;
    public List<GameObject> properties = new List<GameObject>();

    //Grab Point
    public float distanceFromPlayer = 0.5f; // Desired distance from the player
    public Vector3 grabPoint = Vector3.zero;
    public Vector3 grabAreaOffset = Vector3.zero;
    public GameObject grabArea;
    [Tooltip("Vertical offset between stacked objects when carrying multiple.")]
    public float multigrabStackOffset = 0.25f;
    public GameObject grabGraphic;
    private Vector3 normFwd = Vector2.zero;

    //EVENTS / on fire 
    public UnityEvent onPlayerButton_A;
    public UnityEvent onPlayerButton_B;
    public UnityEvent onPlayerButton_X;
    public UnityEvent onPlayerButton_Y;
    public UnityEvent onPlayerButton_Start;
    public UnityEvent onPlayerButton_LB;
    public UnityEvent onPlayerButton_RB;

    private bool coroutineRunning = false;
    public bool doUpdate = false;
    public bool debug = false;

    public bool useManagerColors = false;

    [Header("Particle Effects")]
    public ParticleSystem dustParticles;

    public AudioSource inspectSound;

    [Header("UI Buttons")]
    public GameObject x_button;
    public GameObject y_button;
    public GameObject b_button;
    public GameObject a_button;


    private void OnEnable()
    {
        // Subscribe to the event
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

            // Initialize based on current state
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
        else
        {
            // If no GameManager, ensure PlayerInput is set to Player action map
            if (playerInputComponent == null) playerInputComponent = GetComponent<PlayerInput>();
            if (playerInputComponent != null)
            {
                playerInputComponent.SwitchCurrentActionMap("Player");
                if (debug) Debug.Log("No GameManager found. Setting PlayerInput to 'Player' action map.");
            }
        }
    }

    private void Start()
    {
        //playerSprite = GetComponent<SpriteRenderer>(); // Get the sprite renderer component
        cManager = GameObject.FindFirstObjectByType<creationManager>(); // Get the creation manager component
        gameManager = GameObject.FindFirstObjectByType<GameManager>();

        playerInputComponent = GetComponent<PlayerInput>();

        //add yourself to the player list
        pInfo = GameObject.FindFirstObjectByType<playersInfo>(); // Get the players info component
        playerID = pInfo.allPlayers.Count; // Set the player ID to the current number of players

        //CHANGE CHARACTERS IN HARD LEVELS
        int levelID = 0; // Default to 0 if no GameManager exists
        if (GameManager.Instance != null)
        {
            levelID = GameManager.Instance.playingLevelID;
        }
        if (levelID > 0)
        {
            playerIDOffset = 4;
        }

        spriteID = playerID + playerIDOffset;
        
        // Only set sprite if playerSprite is valid
        if (playerSprite != null)
        {
            // Normalize spriteID if using list
            if (characterSprites != null && characterSprites.Count > 0)
            {
                if (spriteID >= characterSprites.Count || spriteID < 0)
                {
                    spriteID = 0;
                }
            }
            
            Sprite spriteToUse = GetSpriteByID(spriteID);
            
            if (spriteToUse != null)
            {
                playerSprite.sprite = spriteToUse;
            }
            else if (debug)
            {
                Debug.LogWarning("No sprite available from characterSprites list or individual sprite fields.");
            }
        }
        //if (playerID == 0) playerSprite.sprite = characterSprites[0];
        //if (playerID == 1) playerSprite.sprite = characterSprites[1];
        //if (playerID == 2) playerSprite.sprite = characterSprites[2];
        //if (playerID == 3) playerSprite.sprite = characterSprites[3];

        /*
        if (useManagerColors)
        {
            if (pInfo.playerColors[playerID] != null)
            {
                spriteRenderer.color = pInfo.playerColors[playerID]; // Set the color of the sprite renderer to the color of the player
            } else
            {
                spriteRenderer.color = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f); // Set the color of the sprite renderer to a random color
            }
        }
        */

        pInfo.allPlayers.Add(transform.gameObject);
        pInfo.allControllers.Add(this);

        // Ensure PlayerInput is set to Player action map if no GameManager exists
        if (gameManager == null && playerInputComponent != null)
        {
            playerInputComponent.SwitchCurrentActionMap("Player");
            if (debug) Debug.Log("No GameManager found in Start(). Ensuring PlayerInput is set to 'Player' action map.");
        }

        calculateGrabPoint(facingRight ? Vector2.right : Vector2.left);
    }
    //OUTLINE OF PLAYER ACTIONS:
    //-MOVE -> JOYSTICK             WORKING 
    //-GRAB / DROP ->               WORKING
    //-WORK / PRODUCE RESOURCE ->   WORKING
    //-CREATE STRUCTURE (FREE) ->   WORKING
    //-CREATE BY PURCHASE ->        NOT WORKING
    //-DESTROY STRUCTURE ->         NOT WORKING
    //-CLAIM OWNERSHIP ->           NOT WORKING
    //-LOCK / UNLOCK STRUCTURE ->   NOT WORKING

    //IMPROVEMENTS:
    //-PRECISE ACTION POINT ->      WORKING
    //-MULTI OBJECT GRAB ->          NOT WORKING
    void Update()
    {
        if(performingLabor)  PerformLabor();
        if(isAbsorbingResources) AbsorbResources();

        if (!isCarryingObject && !performingLabor && !isAbsorbingResources)
        {
            if (useSnapTarget)
                UpdateSnapTarget();
            else
                UpdateTargetFromLists();
        }

        if (b_button != null) displayButtons();
    }

    /// <summary>
    /// Legacy behavior when snap is disabled: use last item in trigger lists.
    /// </summary>
    private void UpdateTargetFromLists()
    {
        objectToGrab = listobjectsToGrab != null && listobjectsToGrab.Count > 0 ? listobjectsToGrab[listobjectsToGrab.Count - 1] : null;

        GameObject newLabor = listObjectsToLabor != null && listObjectsToLabor.Count > 0 ? listObjectsToLabor[listObjectsToLabor.Count - 1] : null;
        if (objectToLabor != newLabor)
        {
            if (objectToLabor != null)
            {
                var prevStation = objectToLabor.GetComponent<Station>();
                if (prevStation != null && objectToInspect != objectToLabor)
                    prevStation.isBeingInspected = false;
            }
            objectToLabor = newLabor;
            if (objectToLabor != null)
            {
                var station = objectToLabor.GetComponent<Station>();
                if (station != null)
                {
                    station.isBeingInspected = true;
                    station.doUpdate = true;
                }
            }
        }

        GameObject newInspect = listObjectsToInspect != null && listObjectsToInspect.Count > 0 ? listObjectsToInspect[listObjectsToInspect.Count - 1] : null;
        if (objectToInspect != newInspect)
        {
            if (objectToInspect != null)
            {
                var prevStation = objectToInspect.GetComponent<Station>();
                if (prevStation != null && objectToLabor != objectToInspect)
                    prevStation.isBeingInspected = false;
            }
            objectToInspect = newInspect;
            if (objectToInspect != null)
            {
                var station = objectToInspect.GetComponent<Station>();
                if (station != null)
                {
                    station.isBeingInspected = true;
                    station.doUpdate = true;
                }
                doUpdate = true;
            }
        }

        Vector2 fwd = normFwd;
        if (fwd.sqrMagnitude < 0.01f) fwd = facingRight ? Vector2.right : Vector2.left;
        UpdateSnapGraphic(grabPoint, fwd);
    }

    /// <summary>
    /// Picks the closest grabbable/workable/inspectable within snap radius.
    /// Uses aim bias to prefer targets in front of the player.
    /// Searches via Physics2D.OverlapCircle for reliable detection beyond the grab trigger.
    /// </summary>
    private void UpdateSnapTarget()
    {
        Vector2 searchCenter = grabPoint;
        Vector2 fwd = normFwd;

        if (searchCenter == Vector2.zero && grabArea != null)
            searchCenter = grabArea.transform.position;
        if (searchCenter == Vector2.zero)
            searchCenter = transform.position;

        objectToGrab = GetClosestInRadius(listobjectsToGrab, TagType.Grabbable, searchCenter, fwd);

        GameObject newLabor = GetClosestInRadius(listObjectsToLabor, TagType.Workable, searchCenter, fwd);
        if (objectToLabor != newLabor)
        {
            if (objectToLabor != null)
            {
                var prevStation = objectToLabor.GetComponent<Station>();
                if (prevStation != null && objectToInspect != objectToLabor)
                    prevStation.isBeingInspected = false;
            }
            objectToLabor = newLabor;
            if (objectToLabor != null)
            {
                var station = objectToLabor.GetComponent<Station>();
                if (station != null)
                {
                    station.isBeingInspected = true;
                    station.doUpdate = true;
                }
            }
        }

        GameObject newInspect = GetClosestInRadius(listObjectsToInspect, TagType.Inspectable, searchCenter, fwd);
        if (objectToInspect != newInspect)
        {
            if (objectToInspect != null)
            {
                var prevStation = objectToInspect.GetComponent<Station>();
                if (prevStation != null && objectToLabor != objectToInspect)
                    prevStation.isBeingInspected = false;
            }
            objectToInspect = newInspect;
            if (objectToInspect != null)
            {
                var station = objectToInspect.GetComponent<Station>();
                if (station != null)
                {
                    station.isBeingInspected = true;
                    station.doUpdate = true;
                }
                doUpdate = true;
            }
        }

        UpdateSnapGraphic(searchCenter, fwd);
    }

    private void UpdateSnapGraphic(Vector2 searchCenter, Vector2 fwd)
    {
        GameObject snapTarget = useSnapTarget ? (objectToGrab ?? objectToLabor ?? objectToInspect) : null;
        Vector3 graphicPos;

        if (useSnapTarget && snapTarget != null && grabArea != null && grabGraphic != null)
        {
            graphicPos = snapTarget.transform.position;
        }
        else
        {
            graphicPos = transform.position + grabAreaOffset + (Vector3)(fwd.sqrMagnitude > 0.01f ? fwd * distanceFromPlayer : (facingRight ? Vector2.right : Vector2.left) * distanceFromPlayer);
        }

        grabPoint = graphicPos;
        if (grabArea != null) grabArea.transform.position = graphicPos;
        if (grabGraphic != null) grabGraphic.transform.position = graphicPos;
    }

    private GameObject GetClosestInRadius(List<GameObject> existingList, TagType requiredTag, Vector2 searchCenter, Vector2 aimDir)
    {
        GameObject closest = null;
        float bestScore = float.MaxValue;

        var hits = Physics2D.OverlapCircleAll(searchCenter, snapRadius);
        foreach (var col in hits)
        {
            if (col == null || col.gameObject == null) continue;
            if (!TagUtilities.HasTag(col.gameObject, requiredTag)) continue;

            if (requiredTag == TagType.Grabbable)
            {
                var resourceObj = col.GetComponent<ResourceObject>();
                if (resourceObj != null && resourceObj.resourceType != null
                    && resourceObj.resourceType.typeOfBehavior == Resource.ResourceBehavior.Consumable)
                    continue;
            }

            var obj = col.gameObject;
            Vector2 toTarget = (Vector2)obj.transform.position - searchCenter;
            float dist = toTarget.magnitude;
            if (dist < 0.001f) dist = 0.001f;

            float aimBias = aimDir.sqrMagnitude > 0.01f
                ? Vector2.Dot(aimDir, toTarget / dist) * snapAimBias * dist
                : 0f;
            float score = dist - aimBias;

            if (score < bestScore)
            {
                bestScore = score;
                closest = obj;
            }
        }

        if (closest == null && existingList != null)
        {
            foreach (var obj in existingList)
            {
                if (obj == null || !TagUtilities.HasTag(obj, requiredTag)) continue;
                float d = Vector2.Distance(searchCenter, obj.transform.position);
                if (d > snapRadius) continue;

                Vector2 toTarget = (Vector2)obj.transform.position - searchCenter;
                float dist = toTarget.magnitude;
                if (dist < 0.001f) dist = 0.001f;
                float aimBias = aimDir.sqrMagnitude > 0.01f
                    ? Vector2.Dot(aimDir, toTarget / dist) * snapAimBias * dist
                    : 0f;
                float score = dist - aimBias;
                if (score < bestScore)
                {
                    bestScore = score;
                    closest = obj;
                }
            }
        }

        return closest;
    }

    public void setUpPlayerIDOffset(int offset)
    {
        playerIDOffset = offset;
    }

    // Helper method to get a sprite with fallback logic
    private Sprite GetSpriteByID(int id)
    {
        // Try to use characterSprites list first
        if (characterSprites != null && characterSprites.Count > 0)
        {
            if (id >= 0 && id < characterSprites.Count)
            {
                return characterSprites[id];
            }
        }
        
        // Fallback to individual sprite fields
        Sprite[] fallbackSprites = { sprite1, sprite2, sprite3, sprite4 };
        int fallbackIndex = id % 4;
        
        // Find the first available sprite
        for (int i = 0; i < 4; i++)
        {
            int index = (fallbackIndex + i) % 4;
            if (fallbackSprites[index] != null)
            {
                return fallbackSprites[index];
            }
        }
        
        return null;
    }

    // This method will be called whenever the game state changes
    private void HandleGameStateChanged(GameState newState)
    {
        if (debug) Debug.Log("State Change Called");
        if (playerInputComponent ==  null) playerInputComponent = GetComponent<PlayerInput>();

        switch (newState)
        {
            case GameState.Menu:
                playerInputComponent.SwitchCurrentActionMap("UI");
                break;
            case GameState.Playing:
                playerInputComponent.SwitchCurrentActionMap("Player");
                break;
            case GameState.Paused:
                playerInputComponent.SwitchCurrentActionMap("UI");
                break;
            case GameState.Results:
                playerInputComponent.SwitchCurrentActionMap("UI");
                break;
                // ...other states
        }

    }
    /*
    private void OnGameStateChanged(GameState newState)
    {
        if (debug) Debug.Log("State Change Called");

        switch (newState)
        {
            case GameState.Menu:
                playerInputComponent.SwitchCurrentActionMap("UI");
                break;
            case GameState.Playing:
                playerInputComponent.SwitchCurrentActionMap("Player");
                break;
            case GameState.Paused:
                playerInputComponent.SwitchCurrentActionMap("UI");
                break;
            case GameState.Results:
                playerInputComponent.SwitchCurrentActionMap("UI");
                break;
                // ...other states
        }
    }
    */

        private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    public void FlipCharacter(float horizontalInput)
    {
        // Check if we need to flip
        if (horizontalInput > 0 && !facingRight)
        {
            Flip();
        } else if (horizontalInput < 0 && facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = playerSprite.transform.localScale;
        scale.x *= -1; // Flip the x axis
        playerSprite.transform.localScale = scale;
    }

    public void displayButtons()
    {
        if (objectToGrab != null && isCarryingObject == false)
        {
            Vector3 offSet2D = new Vector2(20, -20);
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(grabPoint) + offSet2D;
            b_button.transform.position = screenPosition;
            b_button.SetActive(true);
        } else
        {
            b_button.SetActive(false);
        }
    }

    public void createDust()
    {
        dustParticles.Play();
    }
    public void calculateGrabPoint(Vector2 movement)
    {
        // Calculate the position at the specified distance in the player's forward direction
        if (movement != Vector2.zero) normFwd = movement.normalized;
        grabPoint = transform.position + grabAreaOffset + (normFwd * distanceFromPlayer);
        grabArea.transform.position = grabPoint;
        grabGraphic.transform.position = grabPoint;
        // Grab point icon always stays near the player; no magnetic snap to inspected objects
    }
    public void grabGraphicAnimation()
    {
        if (!coroutineRunning && !isCarryingObject && !performingLabor && doUpdate)
        {
            doUpdate = false;
            float startScale = grabGraphic.transform.localScale.x + 2;
            float endScale = grabGraphic.transform.localScale.x;
            float duration = 0.5f;
            StartCoroutine(ScaleOverTime(grabGraphic, new Vector3(startScale, startScale, startScale), new Vector3(endScale, endScale, endScale), duration));

            //Play sound
            if(inspectSound != null) inspectSound.Play();
        }
    }
    public void onMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();

        calculateGrabPoint(movementInput);

        FlipCharacter(movementInput.x);
    }
    public void onFire(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            fired = context.action.triggered;
            if (debug) Debug.Log("Button A Pressed!");
            onPlayerButton_A.Invoke();
        }
    }
    public void onFire2(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            fired = context.action.triggered;
            if (debug) Debug.Log("Button B Pressed!");
            onPlayerButton_B.Invoke();
        }
    }
    public void onFire3(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (debug) Debug.Log("Button X Pressed Down");
            StartLabor();
        } else if (context.performed)
        {
            if (debug) Debug.Log("Button X Held");
        } else if (context.canceled)
        {
            if (debug) Debug.Log("Button X Released");
            cancelLabor();
        }
    }
    public void onFire4(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (debug) Debug.Log("Button Y Pressed Down");
            StartAbsorb();
        } else if (context.performed)
        {
            if (debug) Debug.Log("Button Y Held");
        } else if (context.canceled)
        {
            if (debug) Debug.Log("Button Y Released");
            StopAbsorb();
            releaseAbsorbedObjects();
        }
    }

    public void onFire5(InputAction.CallbackContext context)
    {

        if (context.started)
        {
            Debug.Log("Button RB Pressed Down");
            
            if (playerSprite == null) return;
            
            spriteID--;
            
            // Handle wrapping based on available sprites
            int maxSprites = (characterSprites != null && characterSprites.Count > 0) 
                ? characterSprites.Count 
                : 4; // Fallback to 4 for individual sprite fields
            
            if (spriteID < 0)
            {
                spriteID = maxSprites - 1;
            }
            
            Sprite spriteToUse = GetSpriteByID(spriteID);
            if (spriteToUse != null)
            {
                playerSprite.sprite = spriteToUse;
            }
        }

    }

    public void onFire6(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("Button LB Pressed Down");
            
            if (playerSprite == null) return;
            
            spriteID++;
            
            // Handle wrapping based on available sprites
            int maxSprites = (characterSprites != null && characterSprites.Count > 0) 
                ? characterSprites.Count 
                : 4; // Fallback to 4 for individual sprite fields
            
            if (spriteID >= maxSprites)
            {
                spriteID = 0;
            }
            
            Sprite spriteToUse = GetSpriteByID(spriteID);
            if (spriteToUse != null)
            {
                playerSprite.sprite = spriteToUse;
            }
        }
    }

    public void onPause(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (debug) Debug.Log("Button Start Pressed Down");
            //StartAbsorb();
            if (gameManager != null)
            {
                gameManager.SetState(GameState.Paused);
                playerInputComponent.SwitchCurrentActionMap("UI");
            }

        } else if (context.performed)
        {
            if (debug) Debug.Log("Button Start Held");
        } else if (context.canceled)
        {
            if (debug) Debug.Log("Button Start Released");
            //StopAbsorb();
            //releaseAbsorbedObjects();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        
    }
    private void FixedUpdate()
    {
        if (dustParticles != null)
        {
            if(movementInput.x != 0 || movementInput.y != 0) createDust();
        }
        rb.linearVelocity = new Vector2(movementInput.x * playerSpeed, movementInput.y * playerSpeed);
    }
    public void CreateObjects()
    {   
        if(cManager != null){
            if (debug) Debug.Log("Create Attempt by: " + transform.gameObject);
            cManager.CreateSpriteAtPlayerPosition(transform.gameObject);
        }
        
    }
    public void StartAbsorb()
    {
        isAbsorbingResources = true;
    }
    public void StopAbsorb()
    {
        isAbsorbingResources = false;
    }
    public void StartLabor()
    {
        if (objectToLabor != null)
        {
            Station stationComp = objectToLabor.GetComponent<Station>();
            if (stationComp != null)
            {
                if(stationComp.inputArea != null && stationComp.consumeResource)
                {
                    if( stationComp.inputArea.AreAllRequirementsMet() != true)
                    {
                        stationComp.inputArea.playErrorSequence();
                        return;
                    }
                }
            }
        }

        performingLabor = true;
    }
    public void PerformLabor()
    {   
        if (objectToLabor != null)
        {
            Station stationComp = objectToLabor.GetComponent<Station>();
            if (stationComp != null)
            {   
                stationComp.worker = this; //assign worker to station
                stationComp.executeLabor(this);
            }
        }
    }
    public void cancelLabor()
    {
        performingLabor = false;
        if (objectToLabor != null)
        {
            Station stationComp = objectToLabor.GetComponent<Station>();
            if (stationComp != null)
            {
                stationComp.cancelLabor(this);
            }
        }
    }
    public void GrabDrop()
    {
        if (!isCarryingObject)
        {
            GrabObject();
        }
        else
        {
            DropObject();
        }
    }
    public void AbsorbResources()
    {
        Debug.Log("Absorbing Resources");
    }
    private void GrabObject()
    {   
        if(objectToGrab != null)
        {
            isCarryingObject = true;

            // Disable object's physics if necessary
            Rigidbody2D objectRb = objectToGrab.GetComponent<Rigidbody2D>();
            if (objectRb != null)
            {
                objectRb.isKinematic = true;
                objectRb.simulated = false;
                objectRb.linearVelocity = Vector2.zero;
                objectRb.angularVelocity = 0f;
            }

            // Calculate the local offset between the object and the carry position
            grabOffset = grabArea.transform.InverseTransformPoint(objectToGrab.transform.position);

            // Parent the object to the carry position
            objectToGrab.transform.SetParent(grabArea.transform);
            

            if (objectToGrab != null)
            {
                // Parent the object to the carry position
                objectToGrab.transform.SetParent(grabArea.transform);
                //objectToGrab.transform.localPosition = grabOffset;
                //objectToGrab.transform.localRotation = Quaternion.identity;
            }
        }
        
    }
    private void DropObject()
    {
        if (objectToGrab == null) return;

        isCarryingObject = false;

        // Re-enable object's physics if necessary
        Rigidbody2D objectRb = objectToGrab.GetComponent<Rigidbody2D>();
        if (objectRb != null)
        {
            objectRb.isKinematic = false;
            objectRb.simulated = true;
        }

        if (objectToGrab != null)
        {
            // Unparent the object
            objectToGrab.transform.SetParent(null);
        }
    }
    public void releaseAbsorbedObjects()
    {
        foreach (GameObject obj in listobjectsToGrab)
        {
            if (obj == null) continue;
            obj.transform.SetParent(null);
            Rigidbody2D objectRb = obj.GetComponent<Rigidbody2D>();
            if (objectRb != null)
            {
                objectRb.isKinematic = false;
                objectRb.simulated = true;
            }
            var dynSort = obj.GetComponent<dynamicSortingOrder>();
            if (dynSort != null) dynSort.invertOrder = false;
        }
        listobjectsToGrab.Clear();
        objectToGrab = null;
    }
    public void sortObsorbedObject(GameObject obj)
    {
        if (obj == null) return;
        if (listobjectsToGrab.Contains(obj)) return;
        if (listobjectsToGrab.Count >= maxObjectsToCarry) return;

        // When carrying one (normal grab) but absorbing more: merge into multigrab
        if (isCarryingObject && objectToGrab != null && !listobjectsToGrab.Contains(objectToGrab))
        {
            listobjectsToGrab.Add(objectToGrab);
            isCarryingObject = false;
            RepositionMultigrabStack();
        }

        listobjectsToGrab.Add(obj);

        Rigidbody2D objectRb = obj.GetComponent<Rigidbody2D>();
        if (objectRb != null)
        {
            objectRb.isKinematic = true;
            objectRb.simulated = false;
            objectRb.linearVelocity = Vector2.zero;
            objectRb.angularVelocity = 0f;
        }

        obj.transform.SetParent(grabArea.transform);
        RepositionMultigrabStack();
    }

    private void RepositionMultigrabStack()
    {
        for (int i = 0; i < listobjectsToGrab.Count; i++)
        {
            var obj = listobjectsToGrab[i];
            if (obj == null) continue;

            // First object at grab point (0,0,0), consecutive with small offset above
            float yOffset = i * multigrabStackOffset;
            obj.transform.localPosition = new Vector3(0, yOffset, 0);

            // Lowest sprite behind, consecutive in front: use sortingOrderOffset so index 0 = lowest
            var dynSort = obj.GetComponent<dynamicSortingOrder>();
            if (dynSort != null)
            {
                dynSort.invertOrder = true;
                dynSort.sortingOrderOffset = i;
            }
        }
    }
    public GameObject GetObjectToGrab()
    {   
        if (isCarryingObject)
        {
            return objectToGrab;
        } else
        {
            return null;
        }
        
    }
    public void DropAndDestroy()
    {
        if (isCarryingObject)
        {
            isCarryingObject = false;
            objectToGrab.transform.SetParent(null);
            Destroy(objectToGrab);
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
    
    public void pauseGame()
    {
        if(gameManager != null) gameManager.SetState(GameState.Paused);
    }
}